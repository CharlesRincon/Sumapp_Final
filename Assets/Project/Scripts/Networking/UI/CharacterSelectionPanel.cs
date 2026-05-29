using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using Networking.Managers;
using FusionUtilsEvents;

namespace Networking.UI
{
    /// <summary>
    /// Carousel character selection panel: one character shown at a time,
    /// navigated with prev/next arrows, confirmed with a single select button.
    /// </summary>
    public class CharacterSelectionPanel : MonoBehaviour
    {
        // --- Carousel UI ---
        [SerializeField] private Image _characterImage;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _selectButton;
        [SerializeField] private TextMeshProUGUI _selectButtonText;

        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _selectionStatusText;
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        // Legacy fields kept so existing Inspector assignments do not break
        [SerializeField] private Transform _slotsContainer;
        [SerializeField] private CharacterSelectionSlot _slotPrefab;

        [SerializeField] private FusionUtilsEvents.FusionEvent OnSelectionCompleteEvent;

        private Networking.Managers.CharacterSelectionManager _selectionManager;
        private NetworkRunner _runner;
        private bool _hasInitialized = false;
        private bool _selectionComplete = false;

        // Carousel state
        private List<Networking.Models.CharacterConfig> _characters = new List<Networking.Models.CharacterConfig>();
        private int _currentIndex = 0;

        private void OnEnable()
        {
            var onPlayerDataSpawnedEvent = Resources.Load<FusionEvent>("Events/OnPlayerDataSpawnedEvent");
            if (onPlayerDataSpawnedEvent != null)
                onPlayerDataSpawnedEvent.RegisterResponse(OnAnyPlayerCharacterSelected);

            var onCharacterSelectionCompleteEvent = Resources.Load<FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            if (onCharacterSelectionCompleteEvent != null)
                onCharacterSelectionCompleteEvent.RegisterResponse(OnCharacterSelectionCompleteGlobal);

            RegisterManagerEvents();

            if (!_hasInitialized)
                TryAutoInitialize();
        }

        private void Update()
        {
            if (!_hasInitialized && gameObject.activeSelf)
                TryAutoInitialize();

            if (_hasInitialized && _selectionManager != null && _timerText != null)
            {
                float remaining = _selectionManager.GetRemainingTime();
                _timerText.text = $"{Mathf.Max(0, remaining):F1}s";

                if (remaining <= 0 && gameObject.activeSelf)
                    OnSelectionTimeExpired();
            }

            if (_hasInitialized && _selectionManager != null && gameObject.activeSelf)
            {
                if (AreAllPlayersSelected())
                    OnSelectionTimeExpired();
            }
        }

        private bool AreAllPlayersSelected()
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner == null) return false;

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                if (playerData == null || playerData.SelectedCharacterId <= 0)
                    return false;
            }
            return true;
        }

        private void OnSelectionTimeExpired()
        {
            if (_selectionComplete || !gameObject.activeSelf) return;

            _selectionComplete = true;
            Debug.Log("[CharacterSelectionPanel] Selection complete - hiding panel.");
            Hide();

            var evt = Resources.Load<FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            if (evt != null)
            {
                var runner = FindFirstObjectByType<NetworkRunner>();
                evt.Raise(PlayerRef.None, runner);
            }
        }

        private void TryAutoInitialize()
        {
            if (_hasInitialized) return;

            var managerInScene = FindFirstObjectByType<Networking.Managers.CharacterSelectionManager>();
            var runner = FindFirstObjectByType<NetworkRunner>();

            if (managerInScene != null && runner != null)
            {
                Initialize(managerInScene, runner);
                Debug.Log("[CharacterSelectionPanel] Auto-initialized.");
            }
        }

        private void OnDisable()
        {
            UnregisterManagerEvents();

            var onPlayerDataSpawnedEvent = Resources.Load<FusionEvent>("Events/OnPlayerDataSpawnedEvent");
            if (onPlayerDataSpawnedEvent != null)
                onPlayerDataSpawnedEvent.RemoveResponse(OnAnyPlayerCharacterSelected);

            var onCharacterSelectionCompleteEvent = Resources.Load<FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            if (onCharacterSelectionCompleteEvent != null)
                onCharacterSelectionCompleteEvent.RemoveResponse(OnCharacterSelectionCompleteGlobal);
        }

        private void OnDestroy()
        {
            UnregisterManagerEvents();
        }

        public void Initialize(Networking.Managers.CharacterSelectionManager selectionManager, NetworkRunner runner)
        {
            if (_hasInitialized) return;

            _hasInitialized = true;
            _selectionManager = selectionManager;
            _runner = runner;

            LoadCharacters();
            WireCarouselButtons();
            UpdateCarouselDisplay();
            UpdateSelectButton();
            UpdateSelectionStatus();

            if (_selectionManager.SelectionCompleteEvent != null)
                _selectionManager.SelectionCompleteEvent.RegisterResponse(OnSelectionPhaseComplete);

            if (_selectionManager.TimeRemainingEvent != null)
                _selectionManager.TimeRemainingEvent.RegisterResponse(UpdateTimer);

            Show();
        }

        private void RegisterManagerEvents()
        {
            if (_selectionManager == null) return;

            if (_selectionManager.SelectionCompleteEvent != null)
                _selectionManager.SelectionCompleteEvent.RegisterResponse(OnSelectionPhaseComplete);

            if (_selectionManager.TimeRemainingEvent != null)
                _selectionManager.TimeRemainingEvent.RegisterResponse(UpdateTimer);
        }

        private void UnregisterManagerEvents()
        {
            if (_selectionManager == null) return;

            if (_selectionManager.SelectionCompleteEvent != null)
                _selectionManager.SelectionCompleteEvent.RemoveResponse(OnSelectionPhaseComplete);

            if (_selectionManager.TimeRemainingEvent != null)
                _selectionManager.TimeRemainingEvent.RemoveResponse(UpdateTimer);
        }

        // ── Carousel ──────────────────────────────────────────────

        private void LoadCharacters()
        {
            _characters.Clear();
            for (int id = 1; id <= 6; id++)
            {
                var config = Networking.Managers.CharacterDatabase.Instance?.GetCharacterById(id);
                if (config != null)
                    _characters.Add(config);
            }
            _currentIndex = 0;
        }

        private void WireCarouselButtons()
        {
            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveAllListeners();
                _prevButton.onClick.AddListener(OnPrevClicked);
            }
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(OnNextClicked);
            }
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        private void OnPrevClicked()
        {
            if (_characters.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _characters.Count) % _characters.Count;
            UpdateCarouselDisplay();
            UpdateSelectButton();
        }

        private void OnNextClicked()
        {
            if (_characters.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _characters.Count;
            UpdateCarouselDisplay();
            UpdateSelectButton();
        }

        private void OnSelectClicked()
        {
            if (_selectionManager == null || _characters.Count == 0) return;

            var character = _characters[_currentIndex];
            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(_runner.LocalPlayer, _runner);

            if (localData != null && localData.SelectedCharacterId == character.CharacterId)
                _selectionManager.DeselectCharacterForLocalPlayer();
            else
                _selectionManager.SelectCharacterForLocalPlayer(character.CharacterId);

            UpdateSelectButton();
        }

        private void UpdateCarouselDisplay()
        {
            if (_characters.Count == 0) return;

            var character = _characters[_currentIndex];

            if (_characterImage != null)
            {
                _characterImage.sprite = character.CharacterSprite;
                _characterImage.enabled = character.CharacterSprite != null;
            }

            if (_characterNameText != null)
                _characterNameText.text = character.CharacterName;
        }

        private void UpdateSelectButton()
        {
            if (_selectButton == null || _selectionManager == null || _characters.Count == 0) return;

            var character = _characters[_currentIndex];
            var selectedIds = Managers.GameManager.Instance.GetSelectedCharacterIds(_runner);
            var localData = Managers.GameManager.Instance?.GetPlayerData(_runner.LocalPlayer, _runner);

            bool isSelectedByMe = localData != null && localData.SelectedCharacterId == character.CharacterId;
            bool isTakenByOther = !isSelectedByMe && selectedIds.Contains(character.CharacterId);

            _selectButton.interactable = !isTakenByOther;

            if (_selectButtonText != null)
                _selectButtonText.text = isSelectedByMe ? "Deselect" : (isTakenByOther ? "Taken" : "Select");
        }

        // ── Existing support methods ───────────────────────────────

        public void UpdateCharacterAvailability()
        {
            if (!_hasInitialized) return;
            UpdateSelectButton();
            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            if (_runner == null) return;

            int selectedCount = 0;
            int totalPlayers = 0;

            foreach (var player in _runner.ActivePlayers)
            {
                totalPlayers++;
                var playerData = Managers.GameManager.Instance.GetPlayerData(player, _runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                    selectedCount++;
            }

            if (_selectionStatusText != null)
                _selectionStatusText.text = $"Selected: {selectedCount}/{totalPlayers}";
        }

        private void UpdateTimer(PlayerRef player, NetworkRunner runner)
        {
            float remaining = _selectionManager.GetRemainingTime();
            if (_timerText != null)
                _timerText.text = $"{Mathf.Max(0, remaining):F1}s";
        }

        private void OnAnyPlayerCharacterSelected(PlayerRef player, NetworkRunner runner)
        {
            if (_hasInitialized)
            {
                UpdateSelectButton();
                UpdateSelectionStatus();
            }
        }

        private void OnSelectionPhaseComplete(PlayerRef player, NetworkRunner runner)
        {
            if (this == null) return;
            Debug.Log("[CharacterSelectionPanel] Selection phase complete.");
            OnSelectionCompleteEvent?.Raise(PlayerRef.None, runner);
            Hide();
        }

        private void OnCharacterSelectionCompleteGlobal(PlayerRef player, NetworkRunner runner)
        {
            if (this == null) return;
            _selectionComplete = true;
            Hide();
        }

        public void Show()
        {
            if (this == null) return;
            _selectionComplete = false;
            gameObject.SetActive(true);
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 1f;
                _panelCanvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (this == null) return;
            gameObject.SetActive(false);
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
                _panelCanvasGroup.blocksRaycasts = false;
            }
        }

        public FusionUtilsEvents.FusionEvent CompleteEvent => OnSelectionCompleteEvent;
    }
}
