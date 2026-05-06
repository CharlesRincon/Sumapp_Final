using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.UI
{
    public class MinigameReadyUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _minigameReadyPanel;
        [SerializeField] private TextMeshProUGUI _minigameReadyText;
        [SerializeField] private Button _minigameReadyButton;

        /// <summary>Read-only panel reference for other controllers (e.g. VictoryUIController).</summary>
        public GameObject Panel => _minigameReadyPanel;

        private void Awake()
        {
            if (_minigameReadyButton != null)
                _minigameReadyButton.onClick.AddListener(OnMinigameReadyClicked);
        }

        private void OnDestroy()
        {
            if (_minigameReadyButton != null)
                _minigameReadyButton.onClick.RemoveListener(OnMinigameReadyClicked);
        }

        /// <summary>Hide panel immediately (used during session reset and initial setup).</summary>
        public void HidePanel()
        {
            if (_minigameReadyPanel != null)
                _minigameReadyPanel.SetActive(false);
        }

        /// <summary>Populate stat texts using defaults before a runner is available.</summary>
        public void InitializeStatus()
        {
            HidePanel();
            if (_minigameReadyButton != null)
                _minigameReadyButton.interactable = true;
        }

        /// <summary>Called every frame from LobbyCanvas.Update while game surface is visible.</summary>
        public void Refresh(NetworkRunner runner, bool isDiceRolling, GameObject turnNotificationPanel)
        {
            if (_minigameReadyPanel == null) return;

            var gameManager = Networking.Managers.GameManager.Instance;

            if (runner != null)
            {
                var localData = gameManager?.GetPlayerData(runner.LocalPlayer, runner);
                bool showPanel = localData != null
                    && localData.IsInMinigameReadyPhase
                    && !localData.IsAwaitingProjectScan
                    && !localData.IsAwaitingProjectDecision
                    && !localData.IsAwaitingCardScan
                    && !localData.IsAwaitingTrivia
                    && !isDiceRolling
                    && (turnNotificationPanel == null || !turnNotificationPanel.activeSelf);

                if (_minigameReadyPanel.activeSelf != showPanel)
                    _minigameReadyPanel.SetActive(showPanel);

                if (!showPanel) return;

                int readyCount = 0;
                int totalPlayers = 0;
                foreach (var player in runner.ActivePlayers)
                {
                    var data = gameManager.GetPlayerData(player, runner);
                    if (data == null || !data.IsInMinigameReadyPhase) continue;
                    totalPlayers++;
                    if (data.IsReadyForMinigame) readyCount++;
                }

                if (_minigameReadyText != null)
                    _minigameReadyText.text = $"Esperando a los demás jugadores... {readyCount}/{Mathf.Max(1, totalPlayers)} listos";

                if (_minigameReadyButton != null)
                    _minigameReadyButton.interactable = !localData.IsReadyForMinigame;
            }
        }

        private void OnMinigameReadyClicked()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner
                         ?? FindFirstObjectByType<NetworkRunner>();
            if (runner == null)
            {
                Debug.LogError("[MinigameReadyUIController] NetworkRunner not found.");
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null)
            {
                Debug.LogError("[MinigameReadyUIController] Local PlayerSessionData not found.");
                return;
            }

            localData.RPC_RequestMinigameReady();

            if (_minigameReadyButton != null)
                _minigameReadyButton.interactable = false;
        }
    }
}
