using UnityEngine;
using System.Collections;
using Fusion;

namespace Networking.UI
{
    /// <summary>
    /// Handles dice rolling animation and networked roll request.
    /// Displays rolling animation (1-10) for 2 seconds, then calls RPC_RequestValidatedTurnRoll
    /// on the local player's PlayerSessionData. The host validates and generates the final value.
    /// After the roll syncs, displays the result and notifies LobbyCanvas to end the turn.
    /// </summary>
    public class DiceUI : MonoBehaviour
    {
        private LobbyCanvas _lobbyCanvas;
        private bool _isRolling;

        private void Start()
        {
            _lobbyCanvas = FindFirstObjectByType<LobbyCanvas>();
        }

        public void StartDiceRoll()
        {
            if (_isRolling) return;

            if (_lobbyCanvas == null)
                _lobbyCanvas = FindFirstObjectByType<LobbyCanvas>();

            if (_lobbyCanvas == null)
            {
                Debug.LogError("[DiceUI] LobbyCanvas not found!");
                return;
            }

            StartCoroutine(DiceRollCoroutine());
        }

        private IEnumerator DiceRollCoroutine()
        {
            _isRolling = true;

            var runner = Networking.Services.FusionNetworkService.LocalRunner
                         ?? FindFirstObjectByType<NetworkRunner>();

            if (runner == null)
            {
                Debug.LogError("[DiceUI] NetworkRunner not found!");
                _isRolling = false;
                yield break;
            }

            // 2-second animation
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                _lobbyCanvas.DisplayDiceResult(Random.Range(1, 11));
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Request validated roll from host
            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData != null)
            {
                localData.RPC_RequestValidatedTurnRoll();
                Debug.Log($"[DiceUI] RPC_RequestValidatedTurnRoll sent for player {runner.LocalPlayer.PlayerId}");

                // Wait for networked LastDiceRoll to sync back
                float waitTime = 0f;
                int previousRoll = localData.LastDiceRoll;
                while (localData.LastDiceRoll == previousRoll && waitTime < 3f)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }

                int finalRoll = localData.LastDiceRoll;
                _lobbyCanvas.DisplayDiceResult(finalRoll);
                Debug.Log($"[DiceUI] Synced dice result: {finalRoll}");
            }
            else
            {
                Debug.LogError("[DiceUI] Local PlayerSessionData not found!");
            }

            _isRolling = false;
        }
    }
}


