using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Networking.UI
{
    /// <summary>
    /// Manages the turn order initialization phase.
    /// Each player rolls a D10 by pressing the roll button. The roll value is stored on
    /// PlayerSessionData.LastDiceRoll via RPC_RollDice() (host-authoritative, [Networked]).
    /// A polling coroutine checks every 0.5s whether all players have a non-zero LastDiceRoll.
    /// When all rolls are in, the results panel is shown sorted by roll (highest first).
    /// </summary>
    public class TurnOrderPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panelGameObject;
        [SerializeField] private TextMeshProUGUI _instructionText;
        [SerializeField] private Button _rollButton;
        [SerializeField] private TextMeshProUGUI _playerRollText;
        [SerializeField] private Transform _turnOrderListContainer;
        [SerializeField] private GameObject _turnOrderEntryPrefab;
        [SerializeField] private float _resultDisplayDuration = 3f;

        private NetworkRunner _runner;
        private bool _isRolling;
        private bool _phaseActive;
        private bool _resultsShown;
        private bool _localPlayerRolled;
        private Coroutine _pollCoroutine;

        private void Start()
        {
            if (_rollButton != null)
                _rollButton.onClick.AddListener(OnRollButtonPressed);

            if (_panelGameObject == null)
                _panelGameObject = gameObject;
        }

        private void OnDestroy()
        {
            if (_rollButton != null)
                _rollButton.onClick.RemoveListener(OnRollButtonPressed);
        }

        /// <summary>
        /// Called by LobbyCanvas to start the turn order phase on ALL clients.
        /// </summary>
        public void StartTurnOrderPhase()
        {
            if (_phaseActive)
            {
                Debug.Log("[TurnOrderPanel] Phase already active, ignoring duplicate call.");
                return;
            }

            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[TurnOrderPanel] NetworkRunner not found!");
                return;
            }

            _phaseActive = true;
            _resultsShown = false;
            _localPlayerRolled = false;

            Debug.Log($"[TurnOrderPanel] StartTurnOrderPhase on {(_runner.IsServer ? "HOST" : "CLIENT")} | Players: {_runner.ActivePlayers.Count()}");

            if (_panelGameObject != null)
                _panelGameObject.SetActive(true);

            if (_instructionText != null)
                _instructionText.text = "¡Tira el dado!";
            if (_rollButton != null)
            {
                _rollButton.gameObject.SetActive(true);
                _rollButton.interactable = true;
            }
            if (_playerRollText != null)
                _playerRollText.text = "";

            // Start polling for all players' rolls
            if (_pollCoroutine != null)
                StopCoroutine(_pollCoroutine);
            _pollCoroutine = StartCoroutine(PollAllRolledRoutine());
        }

        public void ResetPhaseFlag()
        {
            _phaseActive = false;
        }

        private void OnRollButtonPressed()
        {
            if (_isRolling || _localPlayerRolled) return;
            StartCoroutine(RollCoroutine());
        }

        /// <summary>
        /// Animate random numbers, then request a validated turn roll.
        /// The host generates the final value and Fusion syncs it to all clients.
        /// </summary>
        private IEnumerator RollCoroutine()
        {
            _isRolling = true;

            if (_rollButton != null)
                _rollButton.interactable = false;

            float rollDuration = 2f;
            float elapsed = 0f;

            // Animate random numbers during roll
            while (elapsed < rollDuration)
            {
                if (_playerRollText != null)
                    _playerRollText.text = Random.Range(1, 11).ToString();
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Send a single RPC request to host validation flow.
            var localData = GetPlayerData(_runner.LocalPlayer);
            if (localData != null)
            {
                if (!localData.Object.HasInputAuthority)
                {
                    Debug.LogError($"[TurnOrderPanel] Local player data has no input authority for player {_runner.LocalPlayer.PlayerId}.");
                }
                else
                {
                    localData.RPC_RequestValidatedTurnRoll();
                    Debug.Log($"[TurnOrderPanel] RPC_RequestValidatedTurnRoll called for local player {_runner.LocalPlayer.PlayerId}");
                }
            }
            else
            {
                Debug.LogError("[TurnOrderPanel] Local PlayerSessionData not found!");
            }

            _localPlayerRolled = true;
            _isRolling = false;

            // Wait for the networked value to sync back
            if (localData != null)
            {
                float waitTime = 0f;
                while (localData.LastDiceRoll == 0 && waitTime < 3f)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }

                if (localData.LastDiceRoll > 0)
                {
                    if (_playerRollText != null)
                        _playerRollText.text = localData.LastDiceRoll.ToString();
                    Debug.Log($"[TurnOrderPanel] Local player rolled: {localData.LastDiceRoll}");
                }
            }

            if (_instructionText != null)
                _instructionText.text = "Esperando...";
        }

        /// <summary>
        /// Polls every 0.5s to check if all active players have a non-zero LastDiceRoll.
        /// When all rolls are in, shows the final turn order.
        /// </summary>
        private IEnumerator PollAllRolledRoutine()
        {
            while (!_resultsShown)
            {
                yield return new WaitForSeconds(0.5f);

                if (_runner == null) continue;

                bool allRolled = true;
                int rolledCount = 0;
                int totalPlayers = 0;

                foreach (var player in _runner.ActivePlayers)
                {
                    totalPlayers++;
                    var data = GetPlayerData(player);
                    if (data == null || data.LastDiceRoll == 0)
                    {
                        allRolled = false;
                    }
                    else
                    {
                        rolledCount++;
                    }
                }

                Debug.Log($"[TurnOrderPanel] Poll: {rolledCount}/{totalPlayers} players have rolled");

                if (allRolled && totalPlayers > 0)
                {
                    _resultsShown = true;
                    ShowFinalTurnOrder();
                }
            }
        }

        private Networking.Models.PlayerSessionData GetPlayerData(PlayerRef player)
        {
            return Networking.Managers.GameManager.Instance?.GetPlayerData(player, _runner);
        }

        /// <summary>
        /// Display final turn order sorted by roll (highest first).
        /// Reads LastDiceRoll from each player's PlayerSessionData.
        /// </summary>
        private void ShowFinalTurnOrder()
        {
            Debug.Log("[TurnOrderPanel] All players have rolled — showing results!");

            if (_instructionText != null)
                _instructionText.text = "¡Resultados!";
            if (_rollButton != null)
                _rollButton.gameObject.SetActive(false);
            if (_playerRollText != null)
                _playerRollText.text = "";

            // Build sorted list from networked data
            var playerRolls = new List<(PlayerRef player, int roll)>();
            foreach (var player in _runner.ActivePlayers)
            {
                var data = GetPlayerData(player);
                int roll = data != null ? data.LastDiceRoll : 0;
                playerRolls.Add((player, roll));
            }

            playerRolls.Sort((a, b) => b.roll.CompareTo(a.roll));

            // Clear old entries
            if (_turnOrderListContainer != null)
            {
                foreach (Transform child in _turnOrderListContainer)
                    Destroy(child.gameObject);
            }

            if (_turnOrderEntryPrefab == null || _turnOrderListContainer == null)
            {
                Debug.LogError("[TurnOrderPanel] Entry prefab or list container is null!");
                return;
            }

            // Create result entries
            for (int i = 0; i < playerRolls.Count; i++)
            {
                var (player, roll) = playerRolls[i];
                int position = i + 1;

                var entry = Instantiate(_turnOrderEntryPrefab, _turnOrderListContainer);
                entry.name = $"TurnOrderEntry_{position}";
                entry.SetActive(true);

                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    var data = GetPlayerData(player);
                    string playerName = data != null ? (string)data.Nick : $"Player {player.PlayerId}";
                    text.text = $"{position}. {playerName} (rolled {roll})";
                    Debug.Log($"[TurnOrderPanel] {text.text}");
                }
            }

            StartCoroutine(ClosePanelAfterDelay());
        }

        private IEnumerator ClosePanelAfterDelay()
        {
            yield return new WaitForSeconds(_resultDisplayDuration);

            if (_panelGameObject != null)
                _panelGameObject.SetActive(false);

            _phaseActive = false;
            Debug.Log("[TurnOrderPanel] Panel closed. Transitioning to PlayerTurn state.");

            Networking.Managers.GameManager.Instance?.SetGameState(Networking.Managers.GameManager.GameState.PlayerTurn);
        }
    }
}
