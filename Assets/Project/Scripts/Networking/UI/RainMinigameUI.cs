using UnityEngine;
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;
using Networking.Managers;

namespace Networking.UI
{
    public class RainMinigameUI : MonoBehaviour
    {
        [Header("Gameplay UI")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Transform _playerListContainer;
        
        [Header("Leaderboard UI")]
        [SerializeField] private GameObject _leaderboardPanel;
        [SerializeField] private Transform _leaderboardContainer;
        [SerializeField] private GameObject _playerCardPrefab;
        
        [Header("Countdown UI")]
        [SerializeField] private GameObject _countdownPanel;
        [SerializeField] private TextMeshProUGUI _countdownText;

        [Header("Events")]
        [SerializeField] private FusionEvent OnGameEndEvent;

        private RainMinigameManager _manager;
        private NetworkRunner _runner;
        private bool _gameEnded = false;
        private bool _playerCardsInitialized = false;
        private Dictionary<PlayerRef, TextMeshProUGUI> _activePlayerTexts = new Dictionary<PlayerRef, TextMeshProUGUI>();

        private void OnEnable()
        {
            if (OnGameEndEvent == null) OnGameEndEvent = Resources.Load<FusionEvent>("Events/OnGameEndEvent");
            if (OnGameEndEvent != null) OnGameEndEvent.RegisterResponse(OnGameEnd);
        }

        private void OnDisable()
        {
            if (OnGameEndEvent != null) OnGameEndEvent.RemoveResponse(OnGameEnd);
        }

        private void Start()
        {
            _manager = FindFirstObjectByType<RainMinigameManager>();
            _runner = FindFirstObjectByType<NetworkRunner>();
            
            if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
            if (_countdownPanel != null) _countdownPanel.SetActive(true);
        }

        private void Update()
        {
            if (_manager == null)
            {
                _manager = FindFirstObjectByType<RainMinigameManager>();
            }

            if (_manager == null || !_manager.Object.IsValid) return;

            // Initialize live player list as soon as players are available
            if (!_playerCardsInitialized && _runner != null && _runner.ActivePlayers.Count() > 0)
            {
                InitializePlayerCards();
                _playerCardsInitialized = true;
            }

            // Handle Countdown
            float preGameTime = _manager.GetPreGameTime();
            if (preGameTime > 0)
            {
                if (_countdownPanel != null && !_countdownPanel.activeSelf) _countdownPanel.SetActive(true);
                if (_countdownText != null) _countdownText.text = Mathf.CeilToInt(preGameTime).ToString();
                
                // Show live points (leaderboard inicial) even during countdown
                UpdateLivePoints();
                return;
            }
            else
            {
                if (_countdownPanel != null && _countdownPanel.activeSelf) _countdownPanel.SetActive(false);
            }

            if (_gameEnded) return;

            // Update timer
            if (_timerText != null)
            {
                _timerText.text = $"{Mathf.Max(0, _manager.GetRemainingTime()):F1}s";
            }

            // Update live points
            if (_manager.IsGameActive())
            {
                UpdateLivePoints();
            }
        }

        private void InitializePlayerCards()
        {
            if (_playerListContainer == null || _playerCardPrefab == null) return;

            foreach (var player in _runner.ActivePlayers)
            {
                var card = Instantiate(_playerCardPrefab, _playerListContainer);
                var textTMP = card.GetComponentInChildren<TextMeshProUGUI>();
                if (textTMP != null)
                {
                    _activePlayerTexts[player] = textTMP;
                    var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                    string pName = playerData != null ? (string)playerData.Nick : $"P{player.PlayerId}";
                    textTMP.text = $"{pName}: 0";
                }
            }
        }

        private void UpdateLivePoints()
        {
            var points = _manager.GetAllPoints();
            foreach (var kvp in points)
            {
                if (_activePlayerTexts.TryGetValue(kvp.Key, out var textTMP))
                {
                    var playerData = GameManager.Instance.GetPlayerData(kvp.Key, _runner);
                    string pName = playerData != null ? (string)playerData.Nick : $"P{kvp.Key.PlayerId}";
                    textTMP.text = $"{pName}: {kvp.Value}";
                }
            }
            
            // Personal score shortcut
            if (_scoreText != null && _runner != null)
            {
                if (points.TryGetValue(_runner.LocalPlayer, out int p))
                {
                    _scoreText.text = $"Puntos: {p}";
                }
            }
        }

        private void OnGameEnd(PlayerRef player, NetworkRunner runner)
        {
            if (_gameEnded) return;
            _gameEnded = true;
            
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(true);
                DisplayLeaderboard();
            }
        }

        private void DisplayLeaderboard()
        {
            if (_leaderboardContainer == null || _manager == null || _playerCardPrefab == null) return;

            foreach (Transform child in _leaderboardContainer) Destroy(child.gameObject);

            var leaderboard = _manager.GetLeaderboard();
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                var card = Instantiate(_playerCardPrefab, _leaderboardContainer);
                var text = card.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"#{i + 1} {entry.name}: {entry.score} puntos";
                    text.alignment = TextAlignmentOptions.Center;
                }
            }
        }
    }
}
