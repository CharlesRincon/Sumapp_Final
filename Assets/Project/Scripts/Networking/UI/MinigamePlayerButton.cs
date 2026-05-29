using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Linq;

namespace Networking.UI
{
    /// <summary>
    /// Shared click button for all players in the minigame.
    /// All players click the SAME button, but each click is registered individually per player.
    /// When clicked, the local player's click count is incremented and synced via RPC.
    /// </summary>
    public class MinigamePlayerButton : MonoBehaviour
    {
        private Button _button;
        private Networking.Managers.MinigameManager _minigameManager;
        private NetworkRunner _runner;

        private void Start()
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogError("[MinigamePlayerButton] No Button component found!");
                return;
            }

            _minigameManager = FindFirstObjectByType<Networking.Managers.MinigameManager>();
            if (_minigameManager == null)
            {
                Debug.LogWarning("[MinigamePlayerButton] MinigameManager not found yet. Will retry on click.");
            }

            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[MinigamePlayerButton] NetworkRunner not found!");
                return;
            }

            // Wire the button click
            _button.onClick.AddListener(OnClickButton);

            Debug.Log("[MinigamePlayerButton] Click button initialized.");
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClickButton);
            }
        }

        /// <summary>
        /// Called when the player clicks this button.
        /// Directly increments the local player's networked click count.
        /// The [Networked] property automatically syncs to all clients.
        /// </summary>
        private void OnClickButton()
        {
            // Retry finding MinigameManager if not found yet
            if (_minigameManager == null)
            {
                _minigameManager = FindFirstObjectByType<Networking.Managers.MinigameManager>();
                if (_minigameManager == null)
                {
                    Debug.LogError("[MinigamePlayerButton] MinigameManager not found!");
                    return;
                }
                Debug.Log("[MinigamePlayerButton] ✓ Found MinigameManager on click attempt");
            }

            if (_runner == null)
            {
                _runner = FindFirstObjectByType<NetworkRunner>();
                if (_runner == null)
                {
                    Debug.LogError("[MinigamePlayerButton] NetworkRunner not found!");
                    return;
                }
            }

            if (!_minigameManager.IsGameActive())
            {
                Debug.LogWarning("[MinigamePlayerButton] Game is not active - click ignored.");
                return;
            }

            PlayerRef localPlayer = _runner.LocalPlayer;
            Debug.Log($"[MinigamePlayerButton] ✓ Player {localPlayer.PlayerId} clicked button.");

            // Get the local player's PlayerSessionData and call RPC to increment click count
            var localPlayerData = Networking.Managers.GameManager.Instance.GetPlayerData(localPlayer, _runner);
            if (localPlayerData != null)
            {
                Debug.Log($"[MinigamePlayerButton] PlayerData found. HasInputAuth: {localPlayerData.HasInputAuthority}, HasStateAuth: {localPlayerData.HasStateAuthority}");
                Debug.Log($"[MinigamePlayerButton] Before RPC: MinigameClickCount = {localPlayerData.MinigameClickCount}");
                
                if (localPlayerData.HasInputAuthority)
                {
                    // Call RPC on this player's data to increment the click count
                    // The RPC will execute on the host (StateAuthority), which increments the networked property
                    localPlayerData.RPC_IncrementMinigameClickCount();
                    Debug.Log($"[MinigamePlayerButton] ✓ Sent RPC_IncrementMinigameClickCount");
                }
                else
                {
                    Debug.LogError($"[MinigamePlayerButton] ✗ No InputAuthority on this PlayerSessionData!");
                }
            }
            else
            {
                Debug.LogError($"[MinigamePlayerButton] ✗ PlayerSessionData not found for player {localPlayer.PlayerId}");
            }
        }
    }
}
