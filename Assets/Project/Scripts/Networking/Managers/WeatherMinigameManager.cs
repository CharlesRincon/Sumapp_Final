using UnityEngine;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;
using Networking.Models;

namespace Networking.Managers
{
    public struct WeatherLeaderboardEntry
    {
        public PlayerRef player;
        public int score;
        public string name;
    }

    public class WeatherMinigameManager : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _totalDuration = 60f;
        [SerializeField] private float _cardDuration = 5f;
        [SerializeField] private int _winnerWaterReward = 5;
        [SerializeField] private float _leaderboardDisplaySeconds = 10f;
        [SerializeField] private WeatherCardDefinition[] _cards;

        [Header("Events")]
        [SerializeField] private FusionEvent OnGameEndEvent;

        [Networked] private float RemainingTime { get; set; }
        [Networked] private int CurrentCardIndex { get; set; }
        [Networked] private NetworkBool GameActive { get; set; }

        private NetworkRunner _networkRunner;

        public float GetRemainingTime() => RemainingTime;
        public int GetCurrentCardIndex() => CurrentCardIndex;
        public bool IsGameActive() => GameActive;
        public WeatherCardDefinition GetCurrentCard() => (CurrentCardIndex >= 0 && CurrentCardIndex < _cards.Length) ? _cards[CurrentCardIndex] : null;

        public override void Spawned()
        {
            _networkRunner = Runner;
            if (Object.HasStateAuthority)
            {
                ResetAllPlayerScores();
                RemainingTime = _totalDuration;
                CurrentCardIndex = 0;
                _lastProcessedIndex = -1;
                _playersAnsweredCurrentCard.Clear();
                GameActive = true;
            }
        }

        private void ResetAllPlayerScores()
        {
            foreach (var player in _networkRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(player, _networkRunner);
                if (data != null) data.MinigameClickCount = 0;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || !GameActive) return;

            RemainingTime -= _networkRunner.DeltaTime;

            // Cycle cards: every 5s a new card appears (10 cards total)
            float elapsed = _totalDuration - RemainingTime;
            int newIndex = Mathf.FloorToInt(elapsed / _cardDuration);
            
            if (newIndex >= _cards.Length)
            {
                // No more cards, wait for end
            }
            else if (newIndex != CurrentCardIndex)
            {
                CurrentCardIndex = newIndex;
            }

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                GameActive = false;
                EndGame();
            }
        }

        private HashSet<PlayerRef> _playersAnsweredCurrentCard = new HashSet<PlayerRef>();
        private int _lastProcessedIndex = -1;

        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        public void RPC_SubmitAnswer(PlayerRef player, bool choiceIsElNino)
        {
            if (!GameActive) 
            {
                Debug.LogWarning($"[WeatherMinigameManager] Player {player.PlayerId} submitted answer while GameActive=false");
                return;
            }

            // Reset tracked players if card changed
            if (CurrentCardIndex != _lastProcessedIndex)
            {
                _playersAnsweredCurrentCard.Clear();
                _lastProcessedIndex = CurrentCardIndex;
            }

            // Prevent double answering same card
            if (_playersAnsweredCurrentCard.Contains(player))
            {
                Debug.Log($"[WeatherMinigameManager] Player {player.PlayerId} already answered this card ({CurrentCardIndex}). Ignoring.");
                return;
            }

            var currentCard = GetCurrentCard();
            if (currentCard == null) 
            {
                Debug.LogError($"[WeatherMinigameManager] No current card found for index {CurrentCardIndex}");
                return;
            }

            _playersAnsweredCurrentCard.Add(player);

            bool isCorrect = (choiceIsElNino == currentCard.IsElNino);
            int pointsEarned = 0;

            if (isCorrect)
            {
                // Calculate time-based score
                float elapsedSinceStart = _totalDuration - RemainingTime;
                float timeInCurrentCard = elapsedSinceStart % _cardDuration;
                float timeLeftInCard = Mathf.Max(0, _cardDuration - timeInCurrentCard);
                
                // Base 10 points + up to 10 points speed bonus
                pointsEarned = 10 + Mathf.FloorToInt(timeLeftInCard * 2);

                var data = GameManager.Instance.GetPlayerData(player, Runner);
                if (data != null)
                {
                    data.MinigameClickCount += pointsEarned;
                    Debug.Log($"[WeatherMinigameManager] Player {player.PlayerId} ({data.Nick.ToString()}) answered CORRECTLY. Earned {pointsEarned} pts. New Total: {data.MinigameClickCount}");
                }
else
                {
                    Debug.LogError($"[WeatherMinigameManager] PlayerData not found for Player {player.PlayerId}");
                }
            }
            else
            {
                Debug.Log($"[WeatherMinigameManager] Player {player.PlayerId} answered INCORRECTLY.");
            }

            // Notify the client of the result
            RPC_NotifyAnswerResult(player, isCorrect, pointsEarned);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_NotifyAnswerResult(PlayerRef player, bool isCorrect, int points)
        {
            // This will be picked up by the WeatherUIController on the local client
            if (_networkRunner.LocalPlayer == player)
            {
                var ui = UnityEngine.Object.FindFirstObjectByType<Networking.UI.WeatherUIController>();
                if (ui != null)
                {
                    ui.OnAnswerResult(isCorrect, points);
                }
            }
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
                var winnerData = GameManager.Instance.GetPlayerData(leaderboard[0].player, _networkRunner);
                if (winnerData != null) winnerData.WaterAmount += _winnerWaterReward;
            }
        }

        private System.Collections.IEnumerator ReturnToLobbyAfterDelay()
        {
            yield return new WaitForSeconds(_leaderboardDisplaySeconds);
            if (_networkRunner != null && _networkRunner.IsServer)
            {
                foreach (var player in _networkRunner.ActivePlayers)
                {
                    var data = GameManager.Instance.GetPlayerData(player, _networkRunner);
                    if (data != null) data.RPC_LoadLobbyScene();
                }
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_NotifyGameEnd()
        {
            OnGameEndEvent?.Raise(PlayerRef.None, _networkRunner);
        }

        public List<WeatherLeaderboardEntry> GetLeaderboard()
        {
            var list = new List<WeatherLeaderboardEntry>();
            if (_networkRunner == null) return list;

            foreach (var p in _networkRunner.ActivePlayers)
            {
                var data = GameManager.Instance.GetPlayerData(p, _networkRunner);
                list.Add(new WeatherLeaderboardEntry
                {
                    player = p,
                    score = data != null ? data.MinigameClickCount : 0,
                    name = data != null ? (string)data.Nick : $"P{p.PlayerId}"
                });
            }
            return list.OrderByDescending(x => x.score).ToList();
        }
    }
}
