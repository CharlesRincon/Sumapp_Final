using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.UI
{
    public class ProjectFlowUIController : MonoBehaviour
    {
        [SerializeField] private Button _toggleActiveProjectsButton;
        [SerializeField] private GameObject _activeProjectsPanel;
        [SerializeField] private Transform _projectContainer;
        [SerializeField] private GameObject _projectEntryPrefab;
        [Space]
        [SerializeField] private GameObject _projectDecisionPanel;
        [SerializeField] private TextMeshProUGUI _projectDecisionTitleText;
        [SerializeField] private TextMeshProUGUI _projectDecisionPriceText;
        [SerializeField] private TextMeshProUGUI _projectDecisionBodyText;
        [SerializeField] private Button _projectBuyButton;
        [SerializeField] private Button _projectDeclineButton;
        [Space]
        [SerializeField] private GameObject _cardInfoPanel;
        [SerializeField] private TextMeshProUGUI _cardInfoTitleText;
        [SerializeField] private TextMeshProUGUI _cardInfoLoreText;
        [SerializeField] private TextMeshProUGUI _cardInfoEffectText;
        [SerializeField] private float _cardInfoAutoHideSeconds = 4.0f;

        public bool IsProjectFlowVisible { get; private set; }
        private string _lastShownCardTitle;
        private float _cardInfoVisibleUntil;

        private void Awake()
        {
            if (_toggleActiveProjectsButton != null)
            {
                _toggleActiveProjectsButton.onClick.AddListener(OnToggleActiveProjectsClicked);
            }

            if (_projectBuyButton != null)
            {
                _projectBuyButton.onClick.AddListener(OnProjectBuyClicked);
            }

            if (_projectDeclineButton != null)
            {
                _projectDeclineButton.onClick.AddListener(OnProjectDeclineClicked);
            }
        }

        private void OnDestroy()
        {
            if (_toggleActiveProjectsButton != null)
            {
                _toggleActiveProjectsButton.onClick.RemoveListener(OnToggleActiveProjectsClicked);
            }

            if (_projectBuyButton != null)
            {
                _projectBuyButton.onClick.RemoveListener(OnProjectBuyClicked);
            }

            if (_projectDeclineButton != null)
            {
                _projectDeclineButton.onClick.RemoveListener(OnProjectDeclineClicked);
            }
        }

        public void InitializePanels()
        {
            IsProjectFlowVisible = false;
            _lastShownCardTitle = string.Empty;
            _cardInfoVisibleUntil = 0f;

            if (_projectDecisionPanel != null)
            {
                _projectDecisionPanel.SetActive(false);
            }

            if (_cardInfoPanel != null)
            {
                _cardInfoPanel.SetActive(false);
            }

            if (_activeProjectsPanel != null)
            {
                _activeProjectsPanel.SetActive(false);
            }
        }

        public void RefreshProjectDecisionUI(NetworkRunner runner, bool isVuforiaOpen, System.Action closeVuforiaPanel)
        {
            var gameManager = Networking.Managers.GameManager.Instance;
            var localData = gameManager?.GetPlayerData(runner.LocalPlayer, runner);

            bool isAwaitingProjectScan = localData != null && localData.IsAwaitingProjectScan;
            bool isAwaitingProjectDecision = localData != null && localData.IsAwaitingProjectDecision;
            bool isAwaitingCardScan = localData != null && localData.IsAwaitingCardScan;
            bool projectFlowActive = isAwaitingProjectScan || isAwaitingProjectDecision;
            bool hasCardInfo = localData != null && !string.IsNullOrWhiteSpace(localData.PendingCardTitle.ToString());

            var lobbyCanvas = FindFirstObjectByType<LobbyCanvas>();
            bool isNotificationActive = (lobbyCanvas != null && lobbyCanvas.IsProcessingNotification);

            IsProjectFlowVisible = projectFlowActive || isAwaitingCardScan || hasCardInfo;

            if (isAwaitingProjectDecision && isVuforiaOpen && !isNotificationActive
                && _projectDecisionPanel != null && !_projectDecisionPanel.activeSelf)
            {
                _projectDecisionPanel.SetActive(true);
            }

            bool showCardInfo = false;
            if (isVuforiaOpen && hasCardInfo && !isAwaitingProjectDecision)
            {
                string title = localData.PendingCardTitle.ToString();
                if (!string.Equals(_lastShownCardTitle, title, System.StringComparison.Ordinal))
                {
                    _lastShownCardTitle = title;
                    _cardInfoVisibleUntil = Time.unscaledTime + Mathf.Max(0.1f, _cardInfoAutoHideSeconds);
                }

                showCardInfo = Time.unscaledTime < _cardInfoVisibleUntil;
            }
            if (_cardInfoPanel != null)
            {
                _cardInfoPanel.SetActive(showCardInfo);
            }

            if (showCardInfo)
            {
                if (_cardInfoTitleText != null)
                {
                    _cardInfoTitleText.text = localData.PendingCardTitle.ToString();
                }

                if (_cardInfoLoreText != null)
                {
                    _cardInfoLoreText.text = localData.PendingCardLore.ToString();
                }

                if (_cardInfoEffectText != null)
                {
                    _cardInfoEffectText.text = localData.PendingCardEffect.ToString();
                }
            }

            if (!isAwaitingProjectDecision)
            {
                if (_projectDecisionPanel != null)
                {
                    _projectDecisionPanel.SetActive(false);
                }
                return;
            }

            if (_projectDecisionPanel != null && !_projectDecisionPanel.activeSelf)
            {
                _projectDecisionPanel.SetActive(true);
            }

            string projectName = localData.PendingProjectName.ToString();
            if (string.IsNullOrWhiteSpace(projectName))
            {
                projectName = $"Project {localData.PendingProjectId}";
            }

            if (_projectDecisionTitleText != null)
            {
                _projectDecisionTitleText.text = projectName;
            }

            if (_projectDecisionPriceText != null)
            {
                _projectDecisionPriceText.text = $"Price: {localData.PendingProjectPrice}";
            }

            if (_projectDecisionBodyText != null)
            {
                string description = localData.PendingProjectDescription.ToString();
                _projectDecisionBodyText.text = description;
            }

            if (_projectBuyButton != null)
            {
                _projectBuyButton.interactable = localData.MoneyAmount >= localData.PendingProjectPrice;
            }

            if (_projectDeclineButton != null)
            {
                _projectDeclineButton.interactable = true;
            }
        }

        public void RefreshActiveProjectsUI(NetworkRunner runner)
        {
            if (_projectContainer == null || _projectEntryPrefab == null)
            {
                return;
            }

            var gameManager = Networking.Managers.GameManager.Instance;
            if (gameManager == null)
            {
                return;
            }

            var localData = gameManager.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null)
            {
                return;
            }

            var projectDatabase = Networking.Managers.GameManager.Instance != null
                ? gameManager.ProjectDatabase
                : null;

            var slots = new (int id, int zone)[]
            {
                (localData.OwnedProjectSlot0Id, localData.OwnedProjectSlot0Zone),
                (localData.OwnedProjectSlot1Id, localData.OwnedProjectSlot1Zone),
                (localData.OwnedProjectSlot2Id, localData.OwnedProjectSlot2Zone),
            };

            int slotCount = 0;
            foreach (var slot in slots)
            {
                if (slot.id > 0) slotCount++;
            }

            while (_projectContainer.childCount < slotCount)
            {
                Instantiate(_projectEntryPrefab, _projectContainer);
            }

            while (_projectContainer.childCount > slotCount)
            {
                DestroyImmediate(_projectContainer.GetChild(_projectContainer.childCount - 1).gameObject);
            }

            int entryIndex = 0;
            foreach (var slot in slots)
            {
                if (slot.id <= 0) continue;

                var entryTransform = _projectContainer.GetChild(entryIndex);
                var label = entryTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (label == null)
                {
                    entryIndex++;
                    continue;
                }

                string name = $"Project {slot.id}";
                int water = 0;
                int money = 0;

                if (projectDatabase != null && projectDatabase.TryGetProject(slot.id, out var projectDef) && projectDef != null)
                {
                    name = projectDef.DisplayName;
                    var (w, m) = projectDef.GetIncomeForZone((Networking.Models.ColombiaZone)slot.zone);
                    water = w;
                    money = m;
                }

                label.text = $"{name}";
                entryIndex++;
            }
        }

        private void OnToggleActiveProjectsClicked()
        {
            if (_activeProjectsPanel == null)
            {
                Debug.LogWarning("[ProjectFlowUIController] Active projects panel is not assigned.");
                return;
            }

            _activeProjectsPanel.SetActive(!_activeProjectsPanel.activeSelf);
        }

        private void OnProjectBuyClicked()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (runner == null)
            {
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || !localData.IsAwaitingProjectDecision)
            {
                return;
            }

            localData.RPC_RequestBuyPendingProject();
        }

        private void OnProjectDeclineClicked()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (runner == null)
            {
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || (!localData.IsAwaitingProjectDecision && !localData.IsAwaitingProjectScan))
            {
                return;
            }

            localData.RPC_RequestDeclinePendingProject();
        }
    }
}
