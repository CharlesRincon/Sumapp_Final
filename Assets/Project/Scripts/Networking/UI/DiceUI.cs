using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using System.Linq;

namespace Networking.UI
{
    /// <summary>
    /// Controls the dice rolling UI for the table game.
    /// Displays a roll button and shows each player's dice result temporarily.
    /// </summary>
    public class DiceUI : MonoBehaviour
    {
        [SerializeField]
        private Button _rollDiceButton;

        [SerializeField]
        private Transform _diceResultContainer;

        [SerializeField]
        private GameObject _diceResultPrefab;

        [SerializeField]
        private float _resultDisplayDuration = 3f;  // How long to display each result

        private Networking.Managers.DiceManager _diceManager;
        private NetworkRunner _runner;
        private Dictionary<PlayerRef, (Transform display, float hideTime)> _activeDiceResults = 
            new Dictionary<PlayerRef, (Transform, float)>();
        private bool _diceResultsInitialized = false;

        private void OnEnable()
        {
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.AddListener(OnRollDicePressed);
            }
        }

        private void OnDisable()
        {
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.RemoveListener(OnRollDicePressed);
            }
        }

        private void Start()
        {
            _diceManager = Networking.Managers.DiceManager.Instance;
            _runner = FindFirstObjectByType<NetworkRunner>();

            if (_diceManager == null)
            {
                Debug.LogError("[DiceUI] DiceManager not found!");
                return;
            }

            if (_runner == null)
            {
                Debug.LogError("[DiceUI] NetworkRunner not found!");
                return;
            }

            // Hide results container initially
            if (_diceResultContainer != null)
            {
                _diceResultContainer.gameObject.SetActive(false);
            }

            Debug.Log("[DiceUI] ✓ Initialized");
        }

        private void Update()
        {
            if (_diceManager == null || _runner == null)
                return;

            // Initialize dice result displays once network has synced players
            if (!_diceResultsInitialized && _runner.ActivePlayers.Count() > 0)
            {
                InitializeDiceResults();
                _diceResultsInitialized = true;
                Debug.Log("[DiceUI] Dice result displays initialized for " + _runner.ActivePlayers.Count() + " players.");
            }

            // Update dice displays
            UpdateDiceDisplay();

            // Hide old results
            CleanupExpiredResults();
        }

        /// <summary>
        /// Create a result display for each player.
        /// </summary>
        private void InitializeDiceResults()
        {
            if (_diceResultContainer == null || _diceResultPrefab == null)
            {
                Debug.LogWarning("[DiceUI] Dice result container or prefab not assigned!");
                return;
            }

            Debug.Log($"[DiceUI] ✓ Initializing dice results for {_runner.ActivePlayers.Count()} active players");

            foreach (var player in _runner.ActivePlayers)
            {
                // Skip if we already have a display for this player
                if (_activeDiceResults.ContainsKey(player))
                {
                    continue;
                }

                var displayInstance = Instantiate(_diceResultPrefab, _diceResultContainer);
                displayInstance.name = $"DiceResult_{player.PlayerId}";
                displayInstance.SetActive(false);  // Hidden until first roll

                _activeDiceResults[player] = (displayInstance.transform, -1f);
            }

            Debug.Log($"[DiceUI] ✓ Initialized {_activeDiceResults.Count} dice result displays.");
        }

        /// <summary>
        /// Update the dice display for each player based on their last roll.
        /// </summary>
        private void UpdateDiceDisplay()
        {
            foreach (var kvp in _activeDiceResults)
            {
                PlayerRef player = kvp.Key;
                Transform display = kvp.Value.display;
                int diceRoll = _diceManager.GetLastDiceRoll(player);

                // Only show if player has rolled
                if (diceRoll > 0)
                {
                    var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, _runner);
                    string playerName = playerData != null ? (string)playerData.Nick : $"Player {player.PlayerId}";

                    var textTMP = display.GetComponentInChildren<TextMeshProUGUI>();
                    if (textTMP != null)
                    {
                        textTMP.text = $"{playerName}: {diceRoll}";
                    }

                    // Show the container if not already visible
                    if (!_diceResultContainer.gameObject.activeSelf)
                    {
                        _diceResultContainer.gameObject.SetActive(true);
                    }

                    // Enable this dice result display
                    if (!display.gameObject.activeSelf)
                    {
                        display.gameObject.SetActive(true);
                    }

                    // Update hide time
                    float hideTime = _diceManager.GetLastDiceRollTime(player) + _resultDisplayDuration;
                    _activeDiceResults[player] = (display, hideTime);

                    Debug.Log($"[DiceUI] Showing dice result for {playerName}: {diceRoll}");
                }
            }
        }

        /// <summary>
        /// Hide dice results that have been displayed for too long.
        /// </summary>
        private void CleanupExpiredResults()
        {
            float currentTime = Time.time;

            foreach (var kvp in _activeDiceResults)
            {
                Transform display = kvp.Value.display;
                float hideTime = kvp.Value.hideTime;

                // Hide if time has expired
                if (hideTime > 0 && currentTime > hideTime)
                {
                    if (display.gameObject.activeSelf)
                    {
                        display.gameObject.SetActive(false);
                        Debug.Log($"[DiceUI] Hiding expired dice result");
                    }
                }
            }

            // Hide container if no results are visible
            bool anyVisible = false;
            foreach (var kvp in _activeDiceResults)
            {
                if (kvp.Value.display.gameObject.activeSelf)
                {
                    anyVisible = true;
                    break;
                }
            }

            if (!anyVisible && _diceResultContainer.gameObject.activeSelf)
            {
                _diceResultContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called when the Roll Dice button is pressed.
        /// </summary>
        private void OnRollDicePressed()
        {
            if (_diceManager == null)
            {
                Debug.LogError("[DiceUI] DiceManager not found!");
                return;
            }

            if (_runner == null)
            {
                Debug.LogError("[DiceUI] NetworkRunner not found!");
                return;
            }

            PlayerRef localPlayer = _runner.LocalPlayer;
            Debug.Log($"[DiceUI] Rolling dice for local player {localPlayer.PlayerId}...");

            _diceManager.RequestDiceRoll(localPlayer);
        }
    }
}
