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
            int lastAnimRoll = 1;
            while (elapsed < 2f)
            {
                lastAnimRoll = Random.Range(1, 11);
                _lobbyCanvas.DisplayDiceResult(lastAnimRoll);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Pick the final roll locally so we can show it immediately
            int finalRoll = Random.Range(1, 11);
            _lobbyCanvas.DisplayDiceResult(finalRoll);
            Debug.Log($"[DiceUI] Local final roll determined: {finalRoll}");

            // Request validated roll from host using our local value
            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData != null)
            {
                localData.RPC_RequestValidatedTurnRoll(finalRoll);
                Debug.Log($"[DiceUI] RPC_RequestValidatedTurnRoll sent for player {runner.LocalPlayer.PlayerId} with value {finalRoll}");

                // No need to wait for sync to show the number anymore, but we'll wait briefly 
                // to let the network catch up and keep the UI stable.
                yield return new WaitForSeconds(0.35f);
            }
            else
            {
                Debug.LogError("[DiceUI] Local PlayerSessionData not found!");
            }

            _lobbyCanvas.NotifyDiceRollCompleted(finalRoll);
            _isRolling = false;
        }
    }
}


