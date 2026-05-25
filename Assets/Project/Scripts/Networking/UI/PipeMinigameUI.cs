using UnityEngine;
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;
using Networking.Managers;

namespace Networking.UI
{
    /// <summary>
    /// Controls the Pipe Minigame UI and hole spawning.
    /// </summary>
    public class PipeMinigameUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _holePrefab;
        [SerializeField] private Transform _holeContainer;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private TextMeshProUGUI _goalText;
        [SerializeField] private GameObject _leaderboardPanel;
        [SerializeField] private Transform _leaderboardContainer;
        [SerializeField] private GameObject _playerProgressPrefab;
        [SerializeField] private Transform _progressContainer;

        [Header("Events")]
        [SerializeField] private FusionEvent OnGameEndEvent;

        [Header("Local Testing")]
        [SerializeField] private int _fallbackRequiredRepairs = 10;
        [SerializeField] private float _minHoleSeparation = 1.0f;

        private PipeMinigameManager _manager;
        private NetworkRunner _runner;
        private bool _gameEnded = false;
        private List<PipeHole> _activeHoles = new List<PipeHole>();
        private Dictionary<PlayerRef, TextMeshProUGUI> _progressTexts = new Dictionary<PlayerRef, TextMeshProUGUI>();
        private int _localTestRepairCount = 0;

        private void OnEnable()
        {
            if (OnGameEndEvent == null)
                OnGameEndEvent = Resources.Load<FusionEvent>("Events/OnGameEndEvent");

            if (OnGameEndEvent != null)
                OnGameEndEvent.RegisterResponse(OnGameEnd);
        }

        private void OnDisable()
        {
            if (OnGameEndEvent != null)
                OnGameEndEvent.RemoveResponse(OnGameEnd);
        }

        private bool _playerCardsInitialized = false;

        private void Start()
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            _manager = FindFirstObjectByType<PipeMinigameManager>();

            if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);

            if (_runner == null)
            {
                InitializeProgressUI();
                _playerCardsInitialized = true;
            }

            // Spawn initial holes
            int initialHoles = _runner != null ? Mathf.Max(3, _runner.ActivePlayers.Count() * 2) : 5;
            for (int i = 0; i < initialHoles; i++)
            {
                SpawnHole();
            }
        }

        private void Update()
        {
            if (_gameEnded) return;

            if (_manager == null)
            {
                _manager = FindFirstObjectByType<PipeMinigameManager>();
            }

            // Initialize player cards once network has synced players
            if (!_playerCardsInitialized && _runner != null && _runner.ActivePlayers.Count() > 0)
            {
                InitializeProgressUI();
                _playerCardsInitialized = true;
                Debug.Log("[PipeMinigameUI] Player cards initialized with " + _runner.ActivePlayers.Count() + " players.");
            }

            UpdateProgressUI();

            if (_goalText != null)
            {
                int current = 0;
                int required = _manager != null ? _manager.RequiredRepairs : _fallbackRequiredRepairs;

                if (_runner != null && _manager != null)
                {
                    current = _manager.GetPlayerRepairCount(_runner.LocalPlayer);
                }
                else
                {
                    current = _localTestRepairCount;
                }

                int remaining = Mathf.Max(0, required - current);
                _goalText.text = $"Hoyos por reparar: {remaining}";

                // Local end game trigger for testing
                if (_runner == null && remaining <= 0)
                {
                    OnGameEnd(PlayerRef.None, null);
                }
            }
        }

        private void InitializeProgressUI()
        {
            if (_progressContainer == null || _playerProgressPrefab == null) return;

            if (_runner == null)
            {
                Debug.Log("[PipeMinigameUI] Local testing: creating mock progress for LocalPlayer.");
                var progressGO = Instantiate(_playerProgressPrefab, _progressContainer);
                var text = progressGO.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "Local Player (Testing)";
                    _progressTexts[PlayerRef.None] = text;
                }
                return;
            }

            foreach (var player in _runner.ActivePlayers)
            {
                var progressGO = Instantiate(_playerProgressPrefab, _progressContainer);
                var text = progressGO.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    _progressTexts[player] = text;
                }
            }
        }

        private void UpdateProgressUI()
        {
            if (_runner == null)
            {
                if (_progressTexts.ContainsKey(PlayerRef.None))
                {
                    int required = _manager != null ? _manager.RequiredRepairs : _fallbackRequiredRepairs;
                    _progressTexts[PlayerRef.None].text = $"Local Player: {_localTestRepairCount}/{required}";
                }
                return;
            }

            if (_manager == null) return;

            foreach (var player in _runner.ActivePlayers)
            {
                if (_progressTexts.ContainsKey(player))
                {
                    var data = GameManager.Instance.GetPlayerData(player, _runner);
                    string name = data != null ? (string)data.Nick : $"P{player.PlayerId}";
                    int count = _manager.GetPlayerRepairCount(player);
                    _progressTexts[player].text = $"{name}: {count}/{_manager.RequiredRepairs}";
                }
            }
        }

        public void SpawnHole()
        {
            if (_gameEnded || _spawnPoints.Length == 0) return;

            var availablePoints = new List<Transform>();
            foreach (var sp in _spawnPoints)
            {
                if (sp == null) continue;

                bool tooClose = false;
                foreach (var activeHole in _activeHoles)
                {
                    if (activeHole == null) continue;
                    if (Vector3.Distance(activeHole.transform.position, sp.position) < _minHoleSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    availablePoints.Add(sp);
                }
            }

            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("[PipeMinigameUI] No spawn points available that satisfy the minimum hole separation.");
                return;
            }

            Transform spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
            var holeGO = Instantiate(_holePrefab, spawnPoint.position, Quaternion.identity, _holeContainer);
            var hole = holeGO.GetComponent<PipeHole>();
            if (hole != null)
            {
                hole.Initialize(this, _runner);
                _activeHoles.Add(hole);
            }
        }

        public void OnHoleRepaired(PipeHole hole)
        {
            _activeHoles.Remove(hole);

            if (_runner == null)
            {
                _localTestRepairCount++;
            }

            if (!_gameEnded)
            {
                SpawnHole();
            }
        }

        private void OnGameEnd(PlayerRef player, NetworkRunner runner)
        {
            _gameEnded = true;
            Debug.Log("[PipeMinigameUI] Game ended. Displaying leaderboard.");

            // Clear active holes
            foreach (var hole in _activeHoles)
            {
                if (hole != null) Destroy(hole.gameObject);
            }
            _activeHoles.Clear();

            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(true);
                DisplayLeaderboard();
            }
        }

        private void DisplayLeaderboard()
        {
            if (_leaderboardContainer == null || _manager == null) return;

            var leaderboard = _manager.GetLeaderboard();
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                var entryGO = Instantiate(_playerProgressPrefab, _leaderboardContainer);
                var text = entryGO.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"#{i + 1} {entry.name}: {entry.count} puntos";
                    text.alignment = TextAlignmentOptions.Center;
                }
            }
        }
    }
}