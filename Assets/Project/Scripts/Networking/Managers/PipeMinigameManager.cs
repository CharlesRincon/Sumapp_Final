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

        // Network-synchronized state
        [Networked]
        private NetworkBool GameActive { get; set; }

        [Networked]
        private NetworkBool IsRaceEnded { get; set; }

        // Runtime state
        private NetworkRunner _runner;

        public bool IsGameActive() => GameActive && !IsRaceEnded;
        public int RequiredRepairs => _requiredRepairs;

        private NetworkRunner RunnerRef => _runner ?? Runner;

        public int GetPlayerRepairCount(PlayerRef player)
        {
            var runner = RunnerRef;
            var playerData = GameManager.Instance.GetPlayerData(player, runner);
            return playerData != null ? playerData.MinigameClickCount : 0;
        }

        public override void Spawned()
        {
            _runner = Runner;

            Debug.Log($"[PipeMinigameManager] Spawned! IsHost: {Object.HasStateAuthority}");

            if (Object.HasStateAuthority)
            {
                ResetAllPlayerClickCounts();
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
            foreach (var player in _runner.ActivePlayers)
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
            var winnerData = GameManager.Instance.GetPlayerData(winner, _runner);
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

            if (_runner == null || !_runner.IsServer) yield break;

            foreach (var player in _runner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(player, _runner);
                if (data != null)
                    data.RPC_LoadLobbyScene();
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_NotifyGameEnd()
        {
            OnGameEndEvent?.Raise(PlayerRef.None, _runner);
        }

        public List<(PlayerRef player, int count, string name)> GetLeaderboard()
        {
            var leaderboard = new List<(PlayerRef, int, string)>();

            if (_runner == null)
            {
                leaderboard.Add((PlayerRef.None, RequiredRepairs, "Local Player"));
                return leaderboard;
            }

            var sortedPlayers = _runner.ActivePlayers.OrderByDescending(p => GetPlayerRepairCount(p)).ToList();

            foreach (var player in sortedPlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                string playerName = playerData != null ? (string)playerData.Nick : $"Player {player.PlayerId}";
                leaderboard.Add((player, GetPlayerRepairCount(player), playerName));
            }

            return leaderboard;
        }
    }
}