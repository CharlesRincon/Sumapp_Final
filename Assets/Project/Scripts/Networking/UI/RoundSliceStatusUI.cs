using Fusion;
using TMPro;
using UnityEngine;

namespace Networking.UI
{
    /// <summary>
    /// Minimal read-only sync UI for the one-round vertical slice.
    /// </summary>
    public class RoundSliceStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _activeTurnText;
        [SerializeField] private TextMeshProUGUI _localWaterText;
        [SerializeField] private TextMeshProUGUI _basinText;

        private NetworkRunner _runner;

        private void Start()
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }

        private void Update()
        {
            if (_runner == null)
            {
                _runner = FindFirstObjectByType<NetworkRunner>();
                if (_runner == null)
                {
                    return;
                }
            }

            var gameManager = Networking.Managers.GameManager.Instance;
            if (gameManager == null)
            {
                return;
            }

            var activePlayer = gameManager.GetActivePlayer(_runner);
            if (_activeTurnText != null)
            {
                if (activePlayer == default)
                {
                    _activeTurnText.text = "Turn: waiting";
                }
                else
                {
                    var activeData = gameManager.GetPlayerData(activePlayer, _runner);
                    string activeName = activeData != null ? (string)activeData.Nick : $"P{activePlayer.PlayerId}";
                    _activeTurnText.text = $"Turn: {activeName}";
                }
            }

            var localData = gameManager.GetPlayerData(_runner.LocalPlayer, _runner);
            if (localData != null)
            {
                if (_localWaterText != null)
                {
                    _localWaterText.text = $"Water: {localData.WaterAmount}";
                }

                if (_basinText != null)
                {
                    _basinText.text = $"Basin: {localData.BasinHealth}";
                }
            }
        }
    }
}
