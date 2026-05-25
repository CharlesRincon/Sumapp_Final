using UnityEngine;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Managers
{
    /// <summary>
    /// Manages the pipe repair minigame (race mode).
    /// First player to reach the required repair count wins money.
    /// </summary>
    public class PipeMinigameManager : NetworkBehaviour
    {
        [SerializeField]
        private int _requiredRepairs = 10;

        [SerializeField]
        private int _winnerMoneyReward = 5;

        [SerializeField]
        private float _leaderboardDisplaySeconds = 5f;

        [SerializeField]
        private FusionEvent OnGameEndEvent;

        [SerializeField]
        private bool _resetScoreOnSpawn = false;

        // Network-synchronized state
        [Networked]
        private NetworkBool GameActive { get; set; }

        [Networked]
        private NetworkBool IsRaceEnded { get; set; }

        // Local tracking to handle cumulative scores if not resetting
        private Dictionary<PlayerRef, int> _startingClickCounts = new Dictionary<PlayerRef, int>();

        // Runtime state
        private NetworkRunner _minigameRunner;

        public bool IsGameActive() => GameActive && !IsRaceEnded;
        public int RequiredRepairs => _requiredRepairs;

        private NetworkRunner RunnerRef => _minigameRunner ?? Runner;

        public int GetPlayerRepairCount(PlayerRef player)
        {
            var runner = RunnerRef;
            var playerData = GameManager.Instance.GetPlayerData(player, runner);
            if (playerData == null) return 0;

            if (!_resetScoreOnSpawn && _startingClickCounts.ContainsKey(player))
            {
                return Mathf.Max(0, playerData.MinigameClickCount - _startingClickCounts[player]);
            }
            
            return playerData.MinigameClickCount;
        }

        public override void Spawned()
        {
            _minigameRunner = Runner;

            Debug.Log($"[PipeMinigameManager] Spawned! IsHost: {Object.HasStateAuthority}");

            // Cache starting counts on all clients to track repairs correctly in UI and Logic
            foreach (var player in _minigameRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(player, _minigameRunner);
                if (data != null) _startingClickCounts[player] = data.MinigameClickCount;
            }

            if (Object.HasStateAuthority)
            {
                if (_resetScoreOnSpawn)
                {
                    ResetAllPlayerClickCounts();
                    // Clear cache since we just reset everything to 0
                    _startingClickCounts.Clear();
                }
                
                GameActive = true;
                IsRaceEnded = false;
            }
        }

        private void ResetAllPlayerClickCounts()
        {
            if (!Object.HasStateAuthority) return;

            var runner = RunnerRef;
            foreach (var player in runner.ActivePlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, runner);
                if (playerData != null)
                {
                    playerData.MinigameClickCount = 0;
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || !GameActive || IsRaceEnded)
                return;

            // Check if any player has reached the required repairs
            foreach (var player in _minigameRunner.ActivePlayers)
            {
                if (GetPlayerRepairCount(player) >= _requiredRepairs)
                {
                    EndGame(player);
                    break;
                }
            }
        }

        private void EndGame(PlayerRef winner)
        {
            IsRaceEnded = true;
            Debug.Log($"[PipeMinigameManager] Player {winner.PlayerId} won the race!");

            // Reward winner with money
            var winnerData = GameManager.Instance.GetPlayerData(winner, _minigameRunner);
            if (winnerData != null)
            {
                winnerData.MoneyAmount += _winnerMoneyReward;
                Debug.Log($"[PipeMinigameManager] Winner awarded {_winnerMoneyReward} money.");
            }

            RPC_NotifyGameEnd();
            StartCoroutine(ReturnToLobbyAfterDelay());
        }

        private System.Collections.IEnumerator ReturnToLobbyAfterDelay()
        {
            yield return new WaitForSeconds(_leaderboardDisplaySeconds);

            if (_minigameRunner == null || !_minigameRunner.IsServer) yield break;

            foreach (var player in _minigameRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(player, _minigameRunner);
                if (data != null)
                    data.RPC_LoadLobbyScene();
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_NotifyGameEnd()
        {
            OnGameEndEvent?.Raise(PlayerRef.None, _minigameRunner);
        }

        public List<(PlayerRef player, int count, string name)> GetLeaderboard()
        {
            var leaderboard = new List<(PlayerRef, int, string)>();

            if (_minigameRunner == null)
            {
                leaderboard.Add((PlayerRef.None, RequiredRepairs, "Local Player"));
                return leaderboard;
            }

            // Sort by total score (MinigameClickCount) to show the final ranking including previous games (Weather)
            var sortedPlayers = _minigameRunner.ActivePlayers
                .OrderByDescending(p => {
                    var data = GameManager.Instance.GetPlayerData(p, _minigameRunner);
                    return data != null ? data.MinigameClickCount : 0;
                }).ToList();

            foreach (var player in sortedPlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _minigameRunner);
                string playerName = playerData != null ? (string)playerData.Nick : $"Player {player.PlayerId}";
                int totalScore = playerData != null ? playerData.MinigameClickCount : 0;
                leaderboard.Add((player, totalScore, playerName));
            }

            return leaderboard;
        }
    }
}