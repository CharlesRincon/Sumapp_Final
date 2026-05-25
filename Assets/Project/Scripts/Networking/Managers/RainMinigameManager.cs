using UnityEngine;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Managers
{
    public class RainMinigameManager : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _preGameDurationSeconds = 3f;
        [SerializeField] private float _gameDurationSeconds = 20f;
        [SerializeField] private float _leaderboardDisplaySeconds = 8f;
        [SerializeField] private int _winnerWaterReward = 3;
        [SerializeField] private bool _resetScoreOnSpawn = false;

        [Header("Spawning")]
        [SerializeField] private float _baseSpawnInterval = 0.3f;
        [SerializeField] private float _minSpawnInterval = 0.1f;
        [SerializeField] private Vector2 _spawnXRange = new Vector2(-250, 250);
        [SerializeField] private float _spawnY = 600f;

        [Header("Difficulty Scaling")]
        [SerializeField] private float _baseFallSpeed = 600f;
        [SerializeField] private float _maxFallSpeedMultiplier = 2.0f;

        [Header("Drop Types")]
        [SerializeField] private GameObject _regularDropPrefab;
        [SerializeField] private GameObject _highValueDropPrefab;
        [SerializeField] private GameObject _contaminatedDropPrefab;

        [Header("Events")]
        [SerializeField] private FusionEvent OnGameEndEvent;

        [Networked] private float PreGameTime { get; set; }
        [Networked] private float RemainingTime { get; set; }
        [Networked] private NetworkBool GameActive { get; set; }
        [Networked] private NetworkBool IsGameEnded { get; set; }
        [Networked] public float CurrentFallSpeed { get; set; }

        private NetworkRunner _minigameRunner;
        private float _nextSpawnTime;
        private Dictionary<PlayerRef, int> _startingClickCounts = new Dictionary<PlayerRef, int>();

        public float GetRemainingTime() => RemainingTime;
        public float GetPreGameTime() => PreGameTime;
        public bool IsGameActive() => GameActive;
        public bool HasEnded() => IsGameEnded;

        public override void Spawned()
        {
            _minigameRunner = Runner;

            // Cache starting counts on all clients to track relative progress during game
            foreach (var player in _minigameRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(player, _minigameRunner);
                if (data != null) _startingClickCounts[player] = data.MinigameClickCount;
            }

            if (Object.HasStateAuthority)
            {
                if (_resetScoreOnSpawn)
                {
                    ResetAllPlayerScores();
                    _startingClickCounts.Clear();
                }
                
                PreGameTime = _preGameDurationSeconds;
                RemainingTime = _gameDurationSeconds;
                GameActive = false;
                IsGameEnded = false;
                CurrentFallSpeed = _baseFallSpeed;
            }
        }

        private void ResetAllPlayerScores()
        {
            if (!Object.HasStateAuthority) return;

            foreach (var player in _minigameRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(player, _minigameRunner);
                if (data != null) data.MinigameClickCount = 0;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || IsGameEnded) return;

            if (PreGameTime > 0)
            {
                PreGameTime -= _minigameRunner.DeltaTime;
                if (PreGameTime <= 0)
                {
                    PreGameTime = 0;
                    GameActive = true;
                    _nextSpawnTime = (float)Runner.SimulationTime + _baseSpawnInterval;
                }
                return;
            }

            if (!GameActive) return;

            RemainingTime -= _minigameRunner.DeltaTime;

            // Calculate difficulty scaling
            float progress = 1f - (RemainingTime / _gameDurationSeconds);
            CurrentFallSpeed = _baseFallSpeed * (1f + (progress * (_maxFallSpeedMultiplier - 1f)));
            float currentInterval = Mathf.Lerp(_baseSpawnInterval, _minSpawnInterval, progress);

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                GameActive = false;
                IsGameEnded = true;
                EndGame();
                return;
            }

            if ((float)Runner.SimulationTime >= _nextSpawnTime)
            {
                SpawnRandomDrop();
                _nextSpawnTime = (float)Runner.SimulationTime + currentInterval;
            }
        }

        private void SpawnRandomDrop()
        {
            float randX = Random.Range(_spawnXRange.x, _spawnXRange.y);
            Vector2 spawnPos = new Vector2(randX, _spawnY);

            float roll = Random.value;
            GameObject prefabToSpawn = _regularDropPrefab;

            // Rarity Adjustments:
            // Golden (High Value): 5%
            // Contaminated: 20%
            // Regular: 75%
            if (roll < 0.05f) prefabToSpawn = _highValueDropPrefab;
            else if (roll < 0.25f) prefabToSpawn = _contaminatedDropPrefab;

            _minigameRunner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, Object.InputAuthority);
        }

        private void EndGame()
        {
            RewardWinner();
            RPC_NotifyGameEnd();
            StartCoroutine(ReturnToLobbyAfterDelay());
        }

        private void RewardWinner()
        {
            var leaderboard = GetLeaderboard();
            if (leaderboard.Count > 0 && leaderboard[0].score > 0)
            {
                var winnerData = GameManager.Instance.GetPlayerData(leaderboard[0].player, _minigameRunner);
                if (winnerData != null) winnerData.WaterAmount += _winnerWaterReward;
            }
        }

        private System.Collections.IEnumerator ReturnToLobbyAfterDelay()
        {
            yield return new WaitForSeconds(_leaderboardDisplaySeconds);
            if (_minigameRunner != null && _minigameRunner.IsServer)
            {
                foreach (var player in _minigameRunner.ActivePlayers)
                {
                    var data = GameManager.Instance.GetPlayerData(player, _minigameRunner);
                    if (data != null) data.RPC_LoadLobbyScene();
                }
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_NotifyGameEnd()
        {
            OnGameEndEvent?.Raise(PlayerRef.None, _minigameRunner);
        }

        public struct LeaderboardEntry
        {
            public PlayerRef player;
            public int score;
            public string name;
        }

        public List<LeaderboardEntry> GetLeaderboard()
        {
            var list = new List<LeaderboardEntry>();
            if (_minigameRunner == null) return list;

            foreach (var p in _minigameRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(p, _minigameRunner);
                list.Add(new LeaderboardEntry
                {
                    player = p,
                    // Show final cumulative score in leaderboard
                    score = data != null ? data.MinigameClickCount : 0,
                    name = data != null ? (string)data.Nick : $"P{p.PlayerId}"
                });
            }
            return list.OrderByDescending(x => x.score).ToList();
        }

        public Dictionary<PlayerRef, int> GetAllPoints()
        {
            var dict = new Dictionary<PlayerRef, int>();
            if (_minigameRunner == null) return dict;

            foreach (var p in _minigameRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(p, _minigameRunner);
                if (data == null) continue;

                int total = data.MinigameClickCount;
                if (!_resetScoreOnSpawn && _startingClickCounts.ContainsKey(p))
                {
                    // Show points earned ONLY in this minigame session during active gameplay
                    dict[p] = Mathf.Max(0, total - _startingClickCounts[p]);
                }
                else
                {
                    dict[p] = total;
                }
            }
            return dict;
        }
    }
}
