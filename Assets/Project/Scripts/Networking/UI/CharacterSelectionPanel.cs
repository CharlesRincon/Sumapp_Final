using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;
using Networking.Managers;
using FusionUtilsEvents;

namespace Networking.UI
{
    /// <summary>
    /// Controller for the character selection panel UI.
    /// Manages display of 6 character slots, timer countdown, and real-time availability updates.
    /// 
    /// Design Pattern: Model-View pattern where CharacterSelectionManager is the model
    /// and this class is the view that visualizes the model state.
    /// Event-driven updates ensure no polling/frame syncing issues.
    /// </summary>
    public class CharacterSelectionPanel : MonoBehaviour
    {
        [SerializeField]
        private Transform _slotsContainer;

        [SerializeField]
        private TextMeshProUGUI _timerText;

        [SerializeField]
        private TextMeshProUGUI _selectionStatusText;

        [SerializeField]
        private CharacterSelectionSlot _slotPrefab;

        [SerializeField]
        private CanvasGroup _panelCanvasGroup;

        /// <summary>
        /// Event fired when character selection is complete and panel is being hidden.
        /// Allows parent UI (LobbyCanvas) to respond to completion.
        /// </summary>
        [SerializeField]
        private FusionUtilsEvents.FusionEvent OnSelectionCompleteEvent;

        private List<CharacterSelectionSlot> _slots = new List<CharacterSelectionSlot>();
        private Networking.Managers.CharacterSelectionManager _selectionManager;
        private NetworkRunner _runner;
        private bool _hasInitialized = false;
        private bool _selectionComplete = false;

        private void OnEnable()
        {
            // Subscribe to player data events (fires on character selection changes)
            var onPlayerDataSpawnedEvent = Resources.Load<FusionUtilsEvents.FusionEvent>("Events/OnPlayerDataSpawnedEvent");
            if (onPlayerDataSpawnedEvent != null)
            {
                onPlayerDataSpawnedEvent.RegisterResponse(OnAnyPlayerCharacterSelected);
            }

            // Subscribe to character selection complete event (fires on ALL clients when selection is done)
            var onCharacterSelectionCompleteEvent = Resources.Load<FusionUtilsEvents.FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            if (onCharacterSelectionCompleteEvent != null)
            {
                onCharacterSelectionCompleteEvent.RegisterResponse(OnCharacterSelectionCompleteGlobal);
            }

            // Try to auto-initialize if manager is already in scene (for non-host clients)
            if (!_hasInitialized)
            {
                TryAutoInitialize();
            }
        }

        private void Update()
        {
            // Auto-detect manager if not yet initialized (helps non-host clients)
            if (!_hasInitialized && gameObject.activeSelf)
            {
                TryAutoInitialize();
            }

            // Update timer display every frame (works on all clients simultaneously)
            if (_hasInitialized && _selectionManager != null && _timerText != null)
            {
                float remaining = _selectionManager.GetRemainingTime();
                _timerText.text = $"{Mathf.Max(0, remaining):F1}s";
                
                // Check if time has run out (all clients check together)
                if (remaining <= 0 && gameObject.activeSelf)
                {
                    OnSelectionTimeExpired();
                }
            }

            // Check if all players have selected (on all clients)
            if (_hasInitialized && _selectionManager != null && gameObject.activeSelf)
            {
                if (AreAllPlayersSelected())
                {
                    OnSelectionTimeExpired();
                }
            }
        }

        /// <summary>
        /// Check if all active players have selected a character.
        /// </summary>
        private bool AreAllPlayersSelected()
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner == null)
                return false;

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                if (playerData == null || playerData.SelectedCharacterId <= 0)
                {
                    return false; // At least one player hasn't selected
                }
            }

            return true; // All players have selected
        }

        /// <summary>
        /// Called when selection is complete (all selected OR timer expired).
        /// Hides panel on all clients and triggers game lobby transition.
        /// </summary>
        private void OnSelectionTimeExpired()
        {
            if (_selectionComplete || !gameObject.activeSelf)
                return;

            _selectionComplete = true;
            Debug.Log("[CharacterSelectionPanel] Selection complete detected - hiding panel and firing completion event.");
            Hide();
            
            // Fire the global completion event so all clients transition
            var onCharacterSelectionCompleteEvent = Resources.Load<FusionUtilsEvents.FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            if (onCharacterSelectionCompleteEvent != null)
            {
                var runner = FindFirstObjectByType<NetworkRunner>();
                onCharacterSelectionCompleteEvent.Raise(PlayerRef.None, runner);
                Debug.Log("[CharacterSelectionPanel] Fired OnCharacterSelectionCompleteEvent to all clients.");
            }
            else
            {
                Debug.LogWarning("[CharacterSelectionPanel] OnCharacterSelectionCompleteEvent not found in Resources!");
            }
        }

        /// <summary>
        /// Try to automatically initialize the panel by finding an existing CharacterSelectionManager.
        /// Used by non-host clients when the panel becomes active.
        /// </summary>
        private void TryAutoInitialize()
        {
            if (_hasInitialized)
                return;

            var managerInScene = FindFirstObjectByType<Networking.Managers.CharacterSelectionManager>();
            var runner = FindFirstObjectByType<NetworkRunner>();

            if (managerInScene != null && runner != null)
            {
                Initialize(managerInScene, runner);
                Debug.Log("[CharacterSelectionPanel] Auto-initialized with found CharacterSelectionManager.");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from player data events
            var onPlayerDataSpawnedEvent = Resources.Load<FusionUtilsEvents.FusionEvent>("Events/OnPlayerDataSpawnedEvent");
            if (onPlayerDataSpawnedEvent != null)
            {
                onPlayerDataSpawnedEvent.RemoveResponse(OnAnyPlayerCharacterSelected);
            }

            // Unsubscribe from completion event
            var onCharacterSelectionCompleteEvent = Resources.Load<FusionUtilsEvents.FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            if (onCharacterSelectionCompleteEvent != null)
            {
                onCharacterSelectionCompleteEvent.RemoveResponse(OnCharacterSelectionCompleteGlobal);
            }
        }

        /// <summary>
        /// Initialize the character selection panel with references to manager and runner.
        /// Called by LobbyCanvas when starting character selection.
        /// </summary>
        public void Initialize(Networking.Managers.CharacterSelectionManager selectionManager, NetworkRunner runner)
        {
            if (_hasInitialized)
                return; // Already initialized, prevent duplicate initialization

            _hasInitialized = true;
            _selectionManager = selectionManager;
            _runner = runner;

            CreateCharacterSlots();
            UpdateCharacterAvailability();

            if (_selectionManager.SelectionCompleteEvent != null)
            {
                _selectionManager.SelectionCompleteEvent.RegisterResponse(OnSelectionPhaseComplete);
            }

            if (_selectionManager.TimeRemainingEvent != null)
            {
                _selectionManager.TimeRemainingEvent.RegisterResponse(UpdateTimer);
            }

            // Show the panel with proper visibility settings
            Show();
        }

        /// <summary>
        /// Create 6 character selection slot UI elements from the prefab.
        /// Each slot pulls its character data from CharacterDatabase by ID.
        /// </summary>
        private void CreateCharacterSlots()
        {
            // Clear existing slots
            foreach (Transform child in _slotsContainer)
            {
                Destroy(child.gameObject);
            }
            _slots.Clear();

            // Create slot for each character by ID (1-6)
            // Character data is fetched from CharacterDatabase during slot initialization
            for (int characterId = 1; characterId <= 6; characterId++)
            {
                var slot = Instantiate(_slotPrefab, _slotsContainer);
                slot.Initialize(characterId, _selectionManager, _runner);
                _slots.Add(slot);
            }

            Debug.Log("[CharacterSelectionPanel] Created 6 character selection slots.");
        }

        /// <summary>
        /// Update character availability based on current selections across all players.
        /// Called whenever any player selects a character.
        /// </summary>
        public void UpdateCharacterAvailability()
        {
            var selectedIds = Managers.GameManager.Instance.GetSelectedCharacterIds(_runner);

            foreach (var slot in _slots)
            {
                bool isAvailable = !selectedIds.Contains(slot.CharacterId);
                slot.SetAvailable(isAvailable);
            }

            UpdateSelectionStatus();
        }

        /// <summary>
        /// Update the selection status text showing how many players have selected.
        /// </summary>
        private void UpdateSelectionStatus()
        {
            int selectedCount = 0;
            int totalPlayers = 0;

            foreach (var player in _runner.ActivePlayers)
            {
                totalPlayers++;
                var playerData = Managers.GameManager.Instance.GetPlayerData(player, _runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                {
                    selectedCount++;
                }
            }

            if (_selectionStatusText != null)
            {
                _selectionStatusText.text = $"Selected: {selectedCount}/{totalPlayers}";
            }
        }

        /// <summary>
        /// Update timer countdown display.
        /// </summary>
        private void UpdateTimer(PlayerRef player, NetworkRunner runner)
        {
            float remaining = _selectionManager.GetRemainingTime();
            if (_timerText != null)
            {
                _timerText.text = $"{Mathf.Max(0, remaining):F1}s";
            }
        }

        /// <summary>
        /// Called when any player selects a character.
        /// Updates availability and status display.
        /// </summary>
        private void OnAnyPlayerCharacterSelected(PlayerRef player, NetworkRunner runner)
        {
            UpdateCharacterAvailability();
        }

        /// <summary>
        /// Called when character selection phase completes (all selected OR timer expired).
        /// Hides the panel and notifies parent.
        /// </summary>
        private void OnSelectionPhaseComplete(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("[CharacterSelectionPanel] Character selection phase complete.");
            
            // Notify listeners before hiding (e.g., LobbyCanvas)
            OnSelectionCompleteEvent?.Raise(PlayerRef.None, runner);
            
            Hide();
        }

        /// <summary>
        /// Called on ALL clients when character selection is complete (global event from OnCharacterSelectionCompleteEvent).
        /// Ensures all clients hide the panel and prepare for game lobby.
        /// </summary>
        private void OnCharacterSelectionCompleteGlobal(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log($"[CharacterSelectionPanel] Global completion event received - ALL clients transitioning. Runner: {(runner != null ? runner.name : "NULL")}");
            
            // Mark as complete so we don't fire again
            _selectionComplete = true;
            Hide();
        }

        /// <summary>
        /// Show the character selection panel.
        /// </summary>
        public void Show()
        {
            _selectionComplete = false; // Reset for new selection phase
            gameObject.SetActive(true);
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 1f;
                _panelCanvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Hide the character selection panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
                _panelCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Property to access the selection complete event for external listeners (e.g., LobbyCanvas).
        /// </summary>
        public FusionUtilsEvents.FusionEvent CompleteEvent => OnSelectionCompleteEvent;
    }
}
