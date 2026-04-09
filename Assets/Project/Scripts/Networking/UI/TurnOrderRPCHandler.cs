using UnityEngine;
using FusionUtilsEvents;

namespace Networking.UI
{
    /// <summary>
    /// Event broadcaster for turn order initialization.
    /// Uses FusionEvent to broadcast player rolls to all clients.
    /// </summary>
    public class TurnOrderRPCHandler : MonoBehaviour
    {
        [SerializeField]
        private FusionEvent _onPlayerRolledEvent;

        private void Start()
        {
            // Load OnPlayerRolledEvent from Resources if not assigned in inspector
            if (_onPlayerRolledEvent == null)
            {
                _onPlayerRolledEvent = Resources.Load<FusionEvent>("Events/OnPlayerRolledEvent");
                if (_onPlayerRolledEvent != null)
                {
                    Debug.Log("[TurnOrderRPCHandler] ✓ OnPlayerRolledEvent loaded from Resources in Start()");
                }
                else
                {
                    Debug.LogError("[TurnOrderRPCHandler] ✗✗✗ CRITICAL: OnPlayerRolledEvent not found at Resources/Events/OnPlayerRolledEvent! Event broadcasts will NOT work!");
                }
            }
            else
            {
                Debug.Log("[TurnOrderRPCHandler] ✓ OnPlayerRolledEvent assigned in inspector");
            }
        }

        /// <summary>
        /// Public method called by TurnOrderPanel to broadcast a player's roll result.
        /// </summary>
        public void BroadcastPlayerRoll(Fusion.PlayerRef playerWhoRolled)
        {
            Debug.Log($"[TurnOrderRPCHandler] BroadcastPlayerRoll called for player {playerWhoRolled.PlayerId}");
            
            if (_onPlayerRolledEvent != null)
            {
                var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
                if (runner != null)
                {
                    Debug.Log($"[TurnOrderRPCHandler] ✓ Raising OnPlayerRolledEvent for player {playerWhoRolled.PlayerId}");
                    _onPlayerRolledEvent.Raise(playerWhoRolled, runner);
                    Debug.Log($"[TurnOrderRPCHandler] ✓ Event raised successfully - all clients should receive this");
                }
                else
                {
                    Debug.LogError("[TurnOrderRPCHandler] NetworkRunner not found! Cannot raise event!");
                }
            }
            else
            {
                Debug.LogError("[TurnOrderRPCHandler] ✗✗✗ OnPlayerRolledEvent is NULL! Cannot broadcast! Check if it's loaded in Start().");
            }
        }
    }
}
