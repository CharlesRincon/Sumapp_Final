using UnityEngine;
using System.Collections;
using Fusion;

namespace Networking.UI
{
    /// <summary>
    /// Handles local dice rolling animation.
    /// Displays rolling animation (1-10) for 2 seconds, then locks in final result.
    /// Works with LobbyCanvas to display results in existing UI.
    /// No networking required - each player rolls locally.
    /// </summary>
    public class DiceUI : MonoBehaviour
    {
        private Networking.UI.LobbyCanvas _lobbyCanvas;
        private Networking.Managers.DiceManager _diceManager;
        private NetworkRunner _runner;
        private bool _isRolling = false;

        private void Start()
        {
            _lobbyCanvas = FindFirstObjectByType<Networking.UI.LobbyCanvas>();
            _diceManager = Networking.Managers.DiceManager.Instance;
            _runner = FindFirstObjectByType<NetworkRunner>();

            if (_lobbyCanvas == null)
            {
                Debug.LogError("[DiceUI] LobbyCanvas not found!");
                return;
            }

            Debug.Log("[DiceUI] ✓ Initialized");
        }

        /// <summary>
        /// Start the dice rolling animation.
        /// Rolls random numbers 1-10 for 2 seconds, then locks in result.
        /// </summary>
        public void StartDiceRoll()
        {
            if (_isRolling)
            {
                Debug.LogWarning("[DiceUI] Already rolling! Wait for previous roll to finish.");
                return;
            }

            if (_lobbyCanvas == null)
            {
                _lobbyCanvas = FindFirstObjectByType<Networking.UI.LobbyCanvas>();
            }

            if (_diceManager == null)
            {
                _diceManager = Networking.Managers.DiceManager.Instance;
            }

            if (_runner == null)
            {
                _runner = FindFirstObjectByType<NetworkRunner>();
            }

            if (_lobbyCanvas == null)
            {
                Debug.LogError("[DiceUI] LobbyCanvas not found!");
                return;
            }

            StartCoroutine(DiceRollCoroutine());
        }

        /// <summary>
        /// Animation coroutine: rapidly display numbers 1-10 for 2 seconds, then lock in result.
        /// </summary>
        private IEnumerator DiceRollCoroutine()
        {
            _isRolling = true;
            float rollDuration = 2f;  // 2 seconds of rolling
            float elapsedTime = 0f;

            Debug.Log("[DiceUI] Starting dice roll animation (2 seconds)...");

            try
            {
                // Roll for 2 seconds - display random numbers
                while (elapsedTime < rollDuration)
                {
                    int randomNumber = Random.Range(1, 11);  // 1-10
                    _lobbyCanvas.DisplayDiceResult(randomNumber);

                    elapsedTime += Time.deltaTime;
                    yield return null;  // Wait one frame
                }

                // Re-fetch runner to avoid stale reference
                _runner = Networking.Services.FusionNetworkService.LocalRunner;
                if (_runner == null)
                {
                    _runner = FindFirstObjectByType<NetworkRunner>();
                }
                _diceManager = Networking.Managers.DiceManager.Instance;

                if (_runner != null && _runner.LocalPlayer.IsRealPlayer && _diceManager != null)
                {
                    _diceManager.RequestDiceRoll(_runner.LocalPlayer);

                    var gameManager = Networking.Managers.GameManager.Instance;
                    var localData = gameManager != null ? gameManager.GetPlayerData(_runner.LocalPlayer, _runner) : null;
                    float wait = 0f;
                    while (localData != null && localData.LastDiceRoll == 0 && wait < 3f)
                    {
                        wait += Time.deltaTime;
                        yield return null;
                    }

                    int finalRoll = localData != null ? localData.LastDiceRoll : 0;
                    _lobbyCanvas.DisplayDiceResult(finalRoll);
                    Debug.Log($"[DiceUI] Synced dice result: {finalRoll}");
                }
            }
            finally
            {
                _isRolling = false;
            }
        }
    }
}


