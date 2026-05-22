using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Networking.Models;
using Networking.Managers;

namespace Networking.UI
{
    /// <summary>
    /// Represents a single leak in the pipe that needs repair.
    /// Requires multiple clicks to be fully repaired.
    /// </summary>
    public class PipeHole : MonoBehaviour
    {
        [SerializeField]
        private int _clicksRequired = 3;
        
        [SerializeField]
        private Button _button;

        private int _currentClicks;
        private PipeMinigameUI _uiParent;
        private NetworkRunner _runner;

        public void Initialize(PipeMinigameUI uiParent, NetworkRunner runner)
        {
            _uiParent = uiParent;
            _runner = runner;
            _currentClicks = 0;
            
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnHoleClicked);
            }
        }

        private void OnHoleClicked()
        {
            _currentClicks++;
            
            if (_currentClicks >= _clicksRequired)
            {
                RepairHole();
            }
        }

        private void RepairHole()
        {
            if (_runner != null)
            {
                PlayerRef localPlayer = _runner.LocalPlayer;
                var localPlayerData = GameManager.Instance.GetPlayerData(localPlayer, _runner);
                
                if (localPlayerData != null && localPlayerData.HasInputAuthority)
                {
                    localPlayerData.RPC_IncrementMinigameClickCount();
                    Debug.Log("[PipeHole] Hole repaired! Incremented repair count via Network.");
                }
            }
            else
            {
                Debug.Log("[PipeHole] Hole repaired! (Local testing mode, no runner)");
            }

            // Tell the UI that this hole is gone so it can spawn another or just cleanup
            if (_uiParent != null)
                _uiParent.OnHoleRepaired(this);
            
            // For now, just deactivate or destroy
            Destroy(gameObject);
        }
    }
}