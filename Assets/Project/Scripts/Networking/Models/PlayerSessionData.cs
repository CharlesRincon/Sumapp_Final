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

