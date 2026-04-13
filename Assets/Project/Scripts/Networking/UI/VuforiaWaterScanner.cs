using UnityEngine;
using Vuforia;
using Fusion;

namespace Networking.UI
{
    /// <summary>
    /// Attach to a Vuforia Image Target GameObject.
    /// When the target is detected (tracked), the local player requests
    /// a water bonus from the host via RPC.
    /// </summary>
    [RequireComponent(typeof(ObserverBehaviour))]
    public class VuforiaWaterScanner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int _waterBonus = 2;
        [SerializeField] private float _scanCooldownSeconds = 5f;

        private ObserverBehaviour _observer;
        private float _lastScanTime = float.NegativeInfinity;

        private void Awake()
        {
            _observer = GetComponent<ObserverBehaviour>();
            HideTargetImage();
        }

        /// <summary>
        /// Disables the MeshRenderer on the Image Target itself so only
        /// child 3D models are visible, not the target image plane.
        /// </summary>
        private void HideTargetImage()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            _observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        private void OnDisable()
        {
            _observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }

        private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
        {
            if (targetStatus.Status != Status.TRACKED &&
                targetStatus.Status != Status.EXTENDED_TRACKED)
            {
                return;
            }

            if (Time.time - _lastScanTime < _scanCooldownSeconds)
            {
                Debug.Log("[VuforiaWaterScanner] Scan ignored — cooldown active.");
                return;
            }

            _lastScanTime = Time.time;

            var runner = Services.FusionNetworkService.LocalRunner;
            if (runner == null || !runner.IsRunning)
            {
                Debug.LogWarning("[VuforiaWaterScanner] No active runner — cannot request water.");
                return;
            }

            var localPlayer = runner.LocalPlayer;
            var gm = Managers.GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[VuforiaWaterScanner] GameManager not found.");
                return;
            }

            var playerData = gm.GetPlayerData(localPlayer, runner);
            if (playerData == null)
            {
                Debug.LogWarning("[VuforiaWaterScanner] Local PlayerSessionData not found.");
                return;
            }

            Debug.Log($"[VuforiaWaterScanner] Image target detected — requesting +{_waterBonus} water.");
            playerData.RPC_RequestARWaterBonus(_waterBonus);
        }
    }
}
