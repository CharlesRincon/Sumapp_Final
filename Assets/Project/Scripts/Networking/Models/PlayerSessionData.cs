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
        public int MoneyAmount { get; set; }

        [Networked]
        public int OwnedProjectSlot0Id { get; set; }

        [Networked]
        public int OwnedProjectSlot0Zone { get; set; }

        [Networked]
        public int OwnedProjectSlot1Id { get; set; }

        [Networked]
        public int OwnedProjectSlot1Zone { get; set; }

        [Networked]
        public int OwnedProjectSlot2Id { get; set; }

        [Networked]
        public int OwnedProjectSlot2Zone { get; set; }

        [Networked]
        public int PendingProjectId { get; set; }

        [Networked]
        public NetworkString<_128> PendingProjectName { get; set; }
        
        [Networked]
        public NetworkString<_512> PendingProjectDescription { get; set; }

        [Networked]
        public int PendingProjectPrice { get; set; }

        [Networked]
        public int PendingProjectWaterIncome { get; set; }

        [Networked]
        public int PendingProjectMoneyIncome { get; set; }

        [Networked]
        public int PendingProjectZone { get; set; }

        [Networked]
        public bool IsAwaitingProjectScan { get; set; }

        [Networked]
        public bool IsAwaitingProjectDecision { get; set; }

        [Networked]
        public bool IsAwaitingCardScan { get; set; }

        [Networked]
        public NetworkString<_128> PendingCardTitle { get; set; }

        [Networked]
        public NetworkString<_512> PendingCardLore { get; set; }

        [Networked]
        public NetworkString<_512> PendingCardEffect { get; set; }

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
        public WeatherTag ActiveWeatherTag { get; set; }

        [Networked]
        public int WeatherVersion { get; set; }

        [Networked]
        public bool IsInMinigameReadyPhase { get; set; }

        [Networked]
        public bool IsReadyForMinigame { get; set; }

        [Networked]
        public bool HasScannedARThisTurn { get; set; }

        [Networked]
        public bool IsGameOver { get; set; }

        [Networked]
        public bool IsDefeat { get; set; }

        /// <summary>Absorbs the next negative effect from a tile or card. Does not block project passives.</summary>
        [Networked]
        public bool HasNegativeShield { get; set; }

        /// <summary>Applied additively to the player's next dice roll, then cleared to 0.</summary>
        [Networked]
        public int PendingDiceModifier { get; set; }

        /// <summary>True while the player is expected to submit a decision-card vote.</summary>
        [Networked]
        public bool IsAwaitingDecisionVote { get; set; }

        /// <summary>True while the player is waiting to answer a trivia question.</summary>
        [Networked]
        public bool IsAwaitingTrivia { get; set; }

        /// <summary>0 = not yet voted, 1 = chose A, 2 = chose B.</summary>
        [Networked]
        public int PendingDecisionVote { get; set; }

        [Networked]
        public bool DoubleTriviaReward { get; set; }

        [Networked]
        public bool IsPendingTeleportTileResolution { get; set; }

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
        public void RPC_LoadMinigameScene(string sceneName)
        {
            Debug.Log($"[PlayerSessionData] RPC_LoadMinigameScene called for {sceneName}. Loading minigame...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
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

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_SyncWeatherRollResult(int roll, int waterDelta, int moneyDelta)
        {
            var turnOrderPanel = Networking.UI.TurnOrderPanel.Instance;
            if (turnOrderPanel == null)
            {
                turnOrderPanel = UnityEngine.Object.FindFirstObjectByType<Networking.UI.TurnOrderPanel>();
            }

            if (turnOrderPanel == null)
            {
                var allPanels = UnityEngine.Object.FindObjectsOfType<Networking.UI.TurnOrderPanel>(true);
                if (allPanels != null && allPanels.Length > 0)
                {
                    turnOrderPanel = allPanels[0];
                }
            }

            if (turnOrderPanel != null)
            {
                turnOrderPanel.ShowWeatherRollResult(roll, waterDelta, moneyDelta);
                return;
            }

            Debug.LogWarning("[PlayerSessionData] TurnOrderPanel not found for weather roll display.");
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
        /// RPC to add variable points to the minigame click count.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_AddMinigamePoints(int points)
        {
            MinigameClickCount += points;
            Debug.Log($"[PlayerSessionData.RPC_AddMinigamePoints] Player {Object.InputAuthority.PlayerId} added {points} points. New Total: {MinigameClickCount}");
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestTriviaAnswer(bool correct)
        {
            if (!Object.HasStateAuthority) return;

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null) return;

            gameManager.HandleTriviaAnswer(Object.InputAuthority, runner, correct);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestSkipCardScan()
        {
            if (!Object.HasStateAuthority) return;

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null) return;

            gameManager.HandleSkipCardScan(Object.InputAuthority, runner);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestCardScan(int cardId)
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for card scan request.");
                return;
            }

            gameManager.HandleCardScan(Object.InputAuthority, runner, cardId);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestProjectCardScan(int projectId)
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for project scan request.");
                return;
            }

            gameManager.HandleProjectCardScan(Object.InputAuthority, runner, projectId);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestBuyPendingProject()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for buy project request.");
                return;
            }

            gameManager.HandleBuyPendingProject(Object.InputAuthority, runner);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestDeclinePendingProject()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for decline project request.");
                return;
            }

            gameManager.HandleDeclinePendingProject(Object.InputAuthority, runner);
        }

        /// <summary>
        /// Submits this player's vote for an active decision card.
        /// choice: 1 = Option A, 2 = Option B.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestDecisionVote(int choice)
        {
            if (!Object.HasStateAuthority) return;

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null)
            {
                Debug.LogError("[PlayerSessionData] Missing dependencies for decision vote request.");
                return;
            }

            gameManager.HandleDecisionVote(Object.InputAuthority, runner, choice);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_ResolveTeleportLanding()
        {
            if (!Object.HasStateAuthority) return;

            var runner = Runner;
            var gameManager = Networking.Managers.GameManager.Instance;
            if (runner == null || gameManager == null) return;

            if (!IsPendingTeleportTileResolution) return;

            IsPendingTeleportTileResolution = false;
            gameManager.HandleTeleportLanding(this, runner);
        }

        /// <summary>
        /// Backward-compatible wrapper for older UI callers.
        /// Sends the validated roll request RPC from the local input-authority context.
        /// </summary>
        public void RPC_RollDice()
        {
            RPC_RequestValidatedTurnRoll(0);
        }

        /// <summary>
        /// Input-authority request; host validates active-turn and resolves full turn execution.
        /// clientRoll: optional value picked by client during animation to show results immediately.
        /// </summary>
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_RequestValidatedTurnRoll(int clientRoll)
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

                if (clientRoll > 0)
                    LastDiceRoll = clientRoll;
                else
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

            if (clientRoll > 0)
                LastDiceRoll = clientRoll;
            else
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

