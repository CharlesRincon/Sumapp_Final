using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using FusionUtilsEvents;

namespace Networking.Models
{
    /// <summary>
    /// Exact replication of OtherGame PlayerData.
    /// Network-synchronized player data (nickname, instance reference).
    /// </summary>
    public class PlayerSessionData : NetworkBehaviour
    {
        [Networked]
        public NetworkString<_16> Nick { get; set; }

        [Networked]
        public NetworkObject Instance { get; set; }

        /// <summary>
        /// Network-synchronized character selection ID (0 = not selected yet).
        /// Synced across all clients automatically by Fusion.
        /// </summary>
        [Networked]
        public int SelectedCharacterId { get; set; }

        /// <summary>
        /// Network-synchronized minigame click count.
        /// Each player modifies their own count, which syncs automatically to all clients.
        /// </summary>
        [Networked]
        public int MinigameClickCount { get; set; }

        /// <summary>
        /// Network-synchronized dice roll result (1-10).
        /// Synced across all clients automatically by Fusion.
        /// </summary>
        [Networked]
        public int LastDiceRoll { get; set; }

        /// <summary>
        /// Timestamp of the last dice roll for UI display purposes.
        /// Used to auto-hide the dice result after a certain duration.
        /// </summary>
        [Networked]
        public float LastDiceRollTime { get; set; }

        [Networked]
        public int WaterAmount { get; set; }

        [Networked]
        public int BoardPosition { get; set; }

        [Networked]
        public int TurnOrder { get; set; }

        [Networked]
        public bool IsActiveTurn { get; set; }

        [Networked]
        public bool HasRolledThisTurn { get; set; }

        [Networked]
        public int BasinHealth { get; set; }

        [Networked]
        public int CurrentRound { get; set; }

        [Networked]
        public bool IsInMinigameReadyPhase { get; set; }

        [Networked]
        public bool IsReadyForMinigame { get; set; }

        [Networked]
        public bool HasScannedARThisTurn { get; set; }

        [Networked]
        public bool IsGameOver { get; set; }

        public FusionEvent OnPlayerDataSpawnedEvent;

        private ChangeDetector _changeDetector;

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_SetNick(string nick)
        {
            Nick = nick;
        }

        /// <summary>
        /// RPC to set the selected character. Called by client with InputAuthority OR by host (StateAuthority) for auto-assignment.
        /// Only StateAuthority (host) modifies the networked property.
        /// </summary>
        [Rpc(sources: RpcSources.StateAuthority | RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_SetSelectedCharacter(int characterId)
        {
            SelectedCharacterId = characterId;
            Debug.Log($"[PlayerSessionData] Character ID {characterId} set via RPC.");
        }

        /// <summary>
        /// RPC to clear the selected character (e.g., during character selection UI reset).
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_ClearSelectedCharacter()
        {
            SelectedCharacterId = 0;
        }

        /// <summary>
        /// RPC to load the minigame scene on all clients.
        /// Called by host only to transition all players to the minigame.
        /// </summary>
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_LoadMinigameScene()
        {
            Debug.Log("[PlayerSessionData] RPC_LoadMinigameScene called. Loading minigame...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Minigame");
        }

        /// <summary>
        /// RPC to load the lobby scene on all clients.
        /// Called by host to return everyone from minigame to lobby.
        /// </summary>
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_LoadLobbyScene()
        {
            Debug.Log("[PlayerSessionData] RPC_LoadLobbyScene called. Returning to lobby...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        /// <summary>
        /// Input-authority request to mark the local player ready for the next minigame.
        /// Host validates and starts the minigame once every active player is ready.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestMinigameReady()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for minigame ready request.");
                return;
            }

            gameManager.HandlePlayerReadyForMinigame(Object.InputAuthority, runner);
        }

        /// <summary>
        /// RPC to increment the minigame click count.
        /// Called by the player (InputAuthority) and executed on the host (StateAuthority).
        /// The host increments the networked property, which then syncs to all clients.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_IncrementMinigameClickCount()
        {
            Debug.Log($"[PlayerSessionData.RPC_IncrementMinigameClickCount] Host executing for player {Object.InputAuthority.PlayerId}. Before: {MinigameClickCount}");
            MinigameClickCount++;
            Debug.Log($"[PlayerSessionData.RPC_IncrementMinigameClickCount] After: {MinigameClickCount}");
        }

        /// <summary>
        /// Client requests a water bonus after scanning a Vuforia image target.
        /// Host validates and applies the delta.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestARWaterBonus(int waterAmount)
        {
            if (!Object.HasStateAuthority) return;

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for AR water bonus request.");
                return;
            }

            gameManager.HandleARWaterBonus(Object.InputAuthority, runner, waterAmount);
        }

        /// <summary>
        /// Backward-compatible wrapper for older UI callers.
        /// Sends the validated roll request RPC from the local input-authority context.
        /// </summary>
        public void RPC_RollDice()
        {
            RPC_RequestValidatedTurnRoll();
        }

        /// <summary>
        /// Input-authority request; host validates active-turn and resolves full turn execution.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestValidatedTurnRoll()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            var networkService = UnityEngine.Object.FindFirstObjectByType<Networking.Services.FusionNetworkService>();

            if (runner == null || gameManager == null || networkService == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for validated turn roll request.");
                return;
            }

            // Turn-order initialization allows each player to roll once before active-turn flow starts.
            if (gameManager.State == Networking.Managers.GameManager.GameState.TurnOrderInitialization)
            {
                if (LastDiceRoll > 0)
                {
                    return;
                }

                LastDiceRoll = networkService.GenerateValidatedDiceRoll();
                LastDiceRollTime = (float)runner.SimulationTime;

                Networking.Events.NetworkEventDefinitions.Instance?.OnDiceRolledEvent?.Raise(Object.InputAuthority, runner);
                Debug.Log($"[PlayerSessionData] Turn-order roll {LastDiceRoll} for player {Object.InputAuthority.PlayerId}");
                return;
            }

            var activePlayer = gameManager.GetActivePlayer(runner);
            if (!networkService.ValidateDiceRollRequest(Object.InputAuthority, activePlayer, runner))
            {
                return;
            }

            if (HasRolledThisTurn)
            {
                return;
            }

            LastDiceRoll = networkService.GenerateValidatedDiceRoll();
            LastDiceRollTime = (float)runner.SimulationTime;
            HasRolledThisTurn = true;

            Debug.Log($"[PlayerSessionData] Host validated dice roll {LastDiceRoll} for player {Object.InputAuthority.PlayerId}");

            // Drive turn advancement directly instead of through OnDiceRolledEvent,
            // which is local-only and may not reach GameManager if the subscription
            // failed during OnEnable (NetworkEventDefinitions.Instance not ready).
            gameManager.HandleValidatedTurnRoll(Object.InputAuthority, LastDiceRoll, runner);
        }

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
            Debug.Log($"[PlayerSessionData.Spawned] Player {Object.InputAuthority.AsIndex}: MinigameClickCount={MinigameClickCount}, HasInputAuthority={Object.HasInputAuthority}, HasStateAuthority={Object.HasStateAuthority}");

            if (Object.HasInputAuthority)
            {
                string nickName = PlayerPrefs.GetString("Nick", string.Empty);
                RPC_SetNick(string.IsNullOrEmpty(nickName) ? $"Player {Object.InputAuthority.AsIndex}" : nickName);
            }

            DontDestroyOnLoad(this);
            Runner.SetPlayerObject(Object.InputAuthority, Object);
            OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);

            if (Object.HasStateAuthority)
            {
                Networking.Managers.GameManager.Instance.SetPlayerDataObject(Object.InputAuthority, this);
            }

            Debug.Log($"[PlayerSessionData.Spawned] Player {Object.InputAuthority.AsIndex}: Spawn complete. MinigameClickCount={MinigameClickCount}");
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Nick):
                    case nameof(SelectedCharacterId):
                        OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
                        break;
                }
            }
        }
    }
}

