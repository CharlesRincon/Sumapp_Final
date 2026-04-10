using UnityEngine;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Managers
{
    /// <summary>
    /// Manages the minigame phase (click competition).
    /// Tracks click counts for each player and determines winner.
    /// Network-synchronized so all clients see consistent data.
    /// </summary>
    public class MinigameManager : NetworkBehaviour
    {
        [SerializeField]
        private float _gameDurationSeconds = 15f;

        [SerializeField]
        private FusionEvent OnGameEndEvent;

        // Network-synchronized state
        [Networked]
        private float RemainingTime { get; set; }

        [Networked]
        private NetworkBool GameActive { get; set; }

        // Runtime state
        private NetworkRunner _runner;

        public float GetRemainingTime() => RemainingTime;
        public bool IsGameActive() => GameActive;
        public int GetPlayerClickCount(PlayerRef player)
        {
            var playerData = GameManager.Instance.GetPlayerData(player, _runner);
            int clickCount = playerData != null ? playerData.MinigameClickCount : 0;
            Debug.Log($"[MinigameManager.GetPlayerClickCount] Player{player.PlayerId}: clickCount={clickCount}, playerData is {(playerData != null ? "VALID" : "NULL")}");
            return clickCount;
        }
        public Dictionary<PlayerRef, int> GetAllClickCounts()
        {
            var counts = new Dictionary<PlayerRef, int>();
            foreach (var player in _runner.ActivePlayers)
            {
                counts[player] = GetPlayerClickCount(player);
            }
            return counts;
        }

        public override void Spawned()
        {
            _runner = Runner;

            Debug.Log($"[MinigameManager] ✓ Spawned! IsHost: {Object.HasStateAuthority}, ActivePlayers: {_runner.ActivePlayers.Count()}");

            // Only host initializes the game
            if (Object.HasStateAuthority)
            {
                // Reset all players' click counts to 0
                ResetAllPlayerClickCounts();
                
                RemainingTime = _gameDurationSeconds;
                GameActive = true;
                Debug.Log($"[MinigameManager] ✓ Host initialized timer. {_gameDurationSeconds}s, {_runner.ActivePlayers.Count()} players.");
            }
            else
            {
                Debug.Log("[MinigameManager] Non-host instance syncing state from host.");
            }
        }

        /// <summary>
        /// Reset all players' click counts to 0 at the start of the minigame.
        /// Only the host can do this since MinigameClickCount is StateAuthority.
        /// </summary>
        private void ResetAllPlayerClickCounts()
        {
            if (!Object.HasStateAuthority)
            {
                Debug.LogError("[MinigameManager.ResetAllPlayerClickCounts] Only host can reset click counts!");
                return;
            }

            foreach (var player in _runner.ActivePlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                if (playerData != null)
                {
                    playerData.MinigameClickCount = 0;
                    Debug.Log($"[MinigameManager] ✓ Reset click count for Player {player.PlayerId} to 0");
                }
                else
                {
                    Debug.LogWarning($"[MinigameManager] Could not find PlayerSessionData for Player {player.PlayerId}");
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (!GameActive)
                return;

            // Countdown timer
            RemainingTime -= _runner.DeltaTime;

            // Game ended
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                GameActive = false;
                Debug.Log("[MinigameManager] Minigame ended. Showing leaderboard.");
                EndGame();
            }
        }

        /// <summary>
        /// Get sorted leaderboard (highest clicks first).
        /// </summary>
        public List<(PlayerRef player, int clicks, string name)> GetLeaderboard()
        {
            var leaderboard = new List<(PlayerRef, int, string)>();

            // Build list from active players sorted by their MinigameClickCount
            var sortedPlayers = _runner.ActivePlayers.ToList();
            sortedPlayers.Sort((a, b) =>
            {
                int clicksA = GetPlayerClickCount(a);
                int clicksB = GetPlayerClickCount(b);
                return clicksB.CompareTo(clicksA); // Descending
            });

            foreach (var player in sortedPlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                string playerName = playerData != null ? (string)playerData.Nick : $"Player {player.PlayerId}";
                int clicks = GetPlayerClickCount(player);
                leaderboard.Add((player, clicks, playerName));
            }

            return leaderboard;
        }

        [SerializeField] private float _leaderboardDisplaySeconds = 5f;

        /// <summary>
        /// End the game and notify all clients.
        /// After showing leaderboard, auto-return everyone to lobby.
        /// </summary>
        private void EndGame()
        {
            Debug.Log("[MinigameManager] Game ended on host. Broadcasting to all clients...");
            RPC_NotifyGameEnd();
            StartCoroutine(ReturnToLobbyAfterDelay());
        }

        private System.Collections.IEnumerator ReturnToLobbyAfterDelay()
        {
            yield return new UnityEngine.WaitForSeconds(_leaderboardDisplaySeconds);

            if (_runner == null || !_runner.IsServer) yield break;

            Debug.Log("[MinigameManager] Leaderboard shown. Sending all players back to lobby.");
            foreach (var player in _runner.ActivePlayers)
            {
                var data = Networking.Managers.GameManager.Instance?.GetPlayerData(player, _runner);
                if (data != null)
                    data.RPC_LoadLobbyScene();
            }
        }

        /// <summary>
        /// RPC to notify all clients that the game has ended.
        /// Fired by the host when timer reaches 0.
        /// </summary>
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_NotifyGameEnd()
        {
            Debug.Log("[MinigameManager] RPC_NotifyGameEnd received. Firing OnGameEndEvent on all clients.");
            OnGameEndEvent?.Raise(PlayerRef.None, _runner);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Debug.LogWarning("[MinigameManager] ✗ Despawned! This should rarely happen during active gameplay.");
        }
    }
}
