using Fusion;
using UnityEngine;
using Vuforia;

namespace Networking.UI
{
    [RequireComponent(typeof(ObserverBehaviour))]
    public class VuforiaProjectScanner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private Networking.Models.ProjectDefinition _projectDefinition;
        [SerializeField] private float _scanCooldownSeconds = 2f;

        private ObserverBehaviour _observer;
        private float _lastScanTime = float.NegativeInfinity;

        private void Awake()
        {
            _observer = GetComponent<ObserverBehaviour>();
            HideTargetImage();
        }

        private void OnEnable()
        {
            _observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        private void OnDisable()
        {
            _observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }

        private void HideTargetImage()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
        {
            if (targetStatus.Status != Status.TRACKED &&
                targetStatus.Status != Status.EXTENDED_TRACKED)
            {
                return;
            }

            if (_projectDefinition == null)
            {
                Debug.LogWarning($"[VuforiaProjectScanner] No ProjectDefinition assigned on '{name}'.");
                return;
            }

            if (Time.time - _lastScanTime < _scanCooldownSeconds)
            {
                return;
            }

            var runner = Services.FusionNetworkService.LocalRunner;
            var gameManager = Managers.GameManager.Instance;
            if (runner == null || !runner.IsRunning || gameManager == null)
            {
                return;
            }

            var playerData = gameManager.GetPlayerData(runner.LocalPlayer, runner);
            if (playerData == null || !playerData.IsAwaitingProjectScan)
            {
                return;
            }

            _lastScanTime = Time.time;
            playerData.RPC_RequestProjectCardScan(_projectDefinition.ProjectId);
        }
    }
}