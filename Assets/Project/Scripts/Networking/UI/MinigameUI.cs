using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;

namespace Networking.UI
{
    /// <summary>
    /// Controls the minigame UI (timer, player list, leaderboard).
    /// Displays live click counts during gameplay.
    /// Shows leaderboard when game ends.
    /// </summary>
    public class MinigameUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _timerText;

        [SerializeField]
        private Transform _playerListContainer;

        [SerializeField]
        private GameObject _playerCardPrefab;

        [SerializeField]
        private GameObject _leaderboardPanel;

        [SerializeField]
        private Transform _leaderboardContainer;

        [SerializeField]
        private Button _returnLobbyButton;

        [SerializeField]
        private FusionEvent OnGameEndEvent;

        private Networking.Managers.MinigameManager _minigameManager;
        private NetworkRunner _runner;
        private Dictionary<PlayerRef, Transform> _activePlayerCards = new Dictionary<PlayerRef, Transform>();
        private bool _gameEnded = false;
        private bool _playerCardsInitialized = false;
        private int _updatePlayerCountsCallCount = 0;
        private int _lastFrameUpdateCalled = -1;

        private void OnEnable()
        {
            // Load game end event
            if (OnGameEndEvent == null)
            {
                OnGameEndEvent = Resources.Load<FusionEvent>("Events/OnGameEndEvent");
            }
            if (OnGameEndEvent != null)
            {
                OnGameEndEvent.RegisterResponse(OnGameEnd);
            }

            // Wire return lobby button
            if (_returnLobbyButton != null)
            {
                _returnLobbyButton.onClick.AddListener(ReturnToLobby);
            }
        }

        private void OnDisable()
        {
            if (OnGameEndEvent != null)
            {
                OnGameEndEvent.RemoveResponse(OnGameEnd);
            }

            if (_returnLobbyButton != null)
            {
                _returnLobbyButton.onClick.RemoveListener(ReturnToLobby);
            }
        }

        private void Start()
        {
            _minigameManager = FindFirstObjectByType<Networking.Managers.MinigameManager>();
            _runner = FindFirstObjectByType<NetworkRunner>();

            if (_minigameManager == null)
            {
                Debug.LogWarning("[MinigameUI] MinigameManager not found in Start(). Will retry in Update()");
            }
            else
            {
                Debug.Log($"[MinigameUI] ✓ MinigameManager found in Start()");
            }

            if (_runner == null)
            {
                Debug.LogError("[MinigameUI] NetworkRunner not found!");
                return;
            }

            Debug.Log($"[MinigameUI] Found runner. IsServer: {_runner.IsServer}, LocalPlayer: {_runner.LocalPlayer.PlayerId}");
            Debug.Log($"[MinigameUI] Active players: {string.Join(", ", _runner.ActivePlayers.Select(p => p.PlayerId))}");

            // Hide leaderboard initially
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(false);
            }

            Debug.Log("[MinigameUI] Minigame UI started. Waiting for MinigameManager...");
        }

        private void Update()
        {
            // Try to find MinigameManager if not already found
            if (_minigameManager == null)
            {
                _minigameManager = FindFirstObjectByType<Networking.Managers.MinigameManager>();
                if (_minigameManager != null)
                {
                    Debug.Log("[MinigameUI] Found MinigameManager.");
                }
            }

            // Initialize player cards once network has synced players
            if (!_playerCardsInitialized && _runner != null && _runner.ActivePlayers.Count() > 0)
            {
                InitializePlayerCards();
                _playerCardsInitialized = true;
                Debug.Log("[MinigameUI] Player cards initialized with " + _runner.ActivePlayers.Count() + " players.");
            }

            if (_minigameManager == null || _gameEnded)
                return;

            // Update timer display
            if (_timerText != null)
            {
                float remaining = _minigameManager.GetRemainingTime();
                _timerText.text = $"{Mathf.Max(0, remaining):F1}s";
            }
            else
            {
                Debug.LogWarning("[MinigameUI] Timer text not assigned!");
            }

            // Update player click counts
            if (_minigameManager.IsGameActive())
            {
                UpdatePlayerCounts();
            }
        }

        /// <summary>
        /// Create card for each player in the player list.
        /// </summary>
        private void InitializePlayerCards()
        {
            if (_playerListContainer == null || _playerCardPrefab == null)
            {
                Debug.LogWarning("[MinigameUI] Player list container or card prefab not assigned!");
                return;
            }

            Debug.Log($"[MinigameUI] ✓ Initializing player cards for {_runner.ActivePlayers.Count()} active players");

            foreach (var player in _runner.ActivePlayers)
            {
                // Skip if we already have a card for this player
                if (_activePlayerCards.ContainsKey(player))
                {
                    Debug.Log($"[MinigameUI] Card already exists for player {player.PlayerId}, skipping");
                    continue;
                }

                var cardInstance = Instantiate(_playerCardPrefab, _playerListContainer);
                cardInstance.name = $"PlayerCard_{player.PlayerId}";
                _activePlayerCards[player] = cardInstance.transform;

                // Set initial player name and click count from the networked property
                var textTMP = cardInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (textTMP != null)
                {
                    var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, _runner);
                    string playerName = playerData != null ? (string)playerData.Nick : $"Player {player.PlayerId}";
                    int clickCount = playerData != null ? playerData.MinigameClickCount : 0;
                    textTMP.text = $"{playerName}: {clickCount}";
                    Debug.Log($"[MinigameUI] ✓ Created card for {playerName} (Player {player.PlayerId}) with {clickCount} clicks");
                }
            }

            Debug.Log($"[MinigameUI] ✓ Initialized {_activePlayerCards.Count} player cards.");
        }

        /// <summary>
        /// Update displayed click counts for all players.
        /// </summary>
        private void UpdatePlayerCounts()
        {
            int frameNum = Time.frameCount;
            bool isNewFrame = frameNum != _lastFrameUpdateCalled;
            if (isNewFrame)
            {
                _updatePlayerCountsCallCount = 0;
                _lastFrameUpdateCalled = frameNum;
            }
            _updatePlayerCountsCallCount++;

            Debug.Log($"[MinigameUI.UpdatePlayerCounts] FRAME {frameNum} CALL #{_updatePlayerCountsCallCount}");

            var clickCounts = _minigameManager.GetAllClickCounts();
            Debug.Log($"[MinigameUI.UpdatePlayerCounts] GetAllClickCounts returned: {string.Join(", ", clickCounts.Select(kvp => $"P{kvp.Key.PlayerId}={kvp.Value}"))}");
            
            if (clickCounts.Count == 0)
            {
                Debug.LogWarning("[MinigameUI] No click counts available!");
                return;
            }

            foreach (var kvp in clickCounts)
            {
                PlayerRef player = kvp.Key;
                int clicks = kvp.Value;
                Debug.Log($"[MinigameUI.UpdatePlayerCounts] Processing player {player.PlayerId}: GetAllClickCounts returned {clicks}");

                if (_activePlayerCards.ContainsKey(player))
                {
                    var textTMP = _activePlayerCards[player].GetComponentInChildren<TextMeshProUGUI>();
                    if (textTMP != null)
                    {
                        var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, _runner);
                        int playerSessionDataClickCount = playerData != null ? playerData.MinigameClickCount : -1;
                        string playerName = playerData != null ? (string)playerData.Nick : $"Player {player.PlayerId}";
                        string newText = $"{playerName}: {clicks}";
                        
                        Debug.Log($"[MinigameUI.UpdatePlayerCounts] Player{player.PlayerId} {playerName}: GetAllClickCounts={clicks}, PlayerSessionData.MinigameClickCount={playerSessionDataClickCount}");
                        
                        // Only update if the text changed
                        if (textTMP.text != newText)
                        {
                            textTMP.text = newText;
                            Debug.Log($"[MinigameUI] ✓ Updated {playerName} display to '{newText}'");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[MinigameUI] Player {player.PlayerId} not in active cards, but has {clicks} clicks");
                }
            }
        }

        /// <summary>
        /// Called when the game ends - show leaderboard.
        /// </summary>
        private void OnGameEnd(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("[MinigameUI] Game ended.");
            _gameEnded = true;

            // Hide player list (live counts)
            if (_playerListContainer != null)
            {
                _playerListContainer.gameObject.SetActive(false);
                Debug.Log("[MinigameUI] Player list hidden.");
            }

            // Show leaderboard panel
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(true);
                Debug.Log("[MinigameUI] Leaderboard panel shown.");
                DisplayLeaderboard();
            }

            // Keep return button visible
            if (_returnLobbyButton != null)
            {
                _returnLobbyButton.gameObject.SetActive(true);
                Debug.Log("[MinigameUI] Return button visible.");
            }

            Debug.Log("[MinigameUI] Game end sequence complete.");
        }

        /// <summary>
        /// Display the final leaderboard sorted by clicks.
        /// </summary>
        private void DisplayLeaderboard()
        {
            if (_leaderboardContainer == null)
            {
                Debug.LogError("[MinigameUI] Leaderboard container not assigned!");
                return;
            }

            if (_minigameManager == null)
            {
                Debug.LogError("[MinigameUI] MinigameManager is null!");
                return;
            }

            if (_playerCardPrefab == null)
            {
                Debug.LogError("[MinigameUI] Player card prefab not assigned!");
                return;
            }

            // Get leaderboard
            var leaderboard = _minigameManager.GetLeaderboard();
            Debug.Log($"[MinigameUI] Displaying {leaderboard.Count} leaderboard entries.");

            // Create entry for each player
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                int place = i + 1;

                // Instantiate card prefab
                var cardGO = Instantiate(_playerCardPrefab, _leaderboardContainer);
                cardGO.name = $"LeaderboardEntry_{place}";

                // Set text with rank, name, and clicks
                var textComponent = cardGO.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"#{place} {entry.name}: {entry.clicks} clicks";
                    Debug.Log($"[MinigameUI] Entry {place}: {entry.name} - {entry.clicks} clicks");
                }
            }

            Debug.Log("[MinigameUI] Leaderboard populated successfully.");
        }

        /// <summary>
        /// Return to lobby when button is clicked.
        /// </summary>
        public void ReturnToLobby()
        {
            Debug.Log("[MinigameUI] Returning to lobby...");
            
            // Load lobby scene - NetworkRunner will handle persistence
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
    }
}
