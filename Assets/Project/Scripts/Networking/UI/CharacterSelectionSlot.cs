using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Networking.Managers;

namespace Networking.UI
{
    /// <summary>
    /// Individual character selection slot (button) displayed in the character selection panel.
    /// Handles visual feedback (enabled/disabled/selected) and click interactions.
    /// 
    /// Design: Each slot is self-managing - it knows its character config and can update
    /// its own visual state based on availability and selection status.
    /// </summary>
    public class CharacterSelectionSlot : MonoBehaviour
    {
        // UI component references - auto-discovered (NOT serialized)
        [System.NonSerialized]
        private Image _characterImage;  // Button's built-in Image child

        [System.NonSerialized]
        private TextMeshProUGUI _characterNameText;  // Button's built-in Text child

        [System.NonSerialized]
        private Button _selectButton;

        [System.NonSerialized]
        private Image _selectionIndicator;  // Optional child for selection feedback

        // Color settings (serialized for inspector customization)
        [SerializeField]
        private Color _availableColor = Color.white;

        [SerializeField]
        private Color _selectedByOtherColor = Color.gray;

        [SerializeField]
        private Color _selectedByYouColor = Color.green;

        // Runtime references (NOT serialized)
        [System.NonSerialized]
        private Networking.Models.CharacterConfig _character;

        [System.NonSerialized]
        private Networking.Managers.CharacterSelectionManager _selectionManager;

        [System.NonSerialized]
        private NetworkRunner _runner;

        [System.NonSerialized]
        private bool _isAvailable = true;

        [System.NonSerialized]
        private bool _isSelectedByLocalPlayer = false;

        public int CharacterId => _character != null ? _character.CharacterId : -1;

        /// <summary>
        /// Auto-discover UI components from Button structure.
        /// Finds Button's built-in Image and Text children.
        /// Optionally searches for SelectionIndicator child for visual feedback.
        /// </summary>
        private void AutoDiscoverComponents()
        {
            // Get the button on this GameObject (required)
            _selectButton = GetComponent<Button>();
            if (_selectButton == null)
            {
                Debug.LogError("[CharacterSelectionSlot] Could not find Button component on this GameObject!");
                return;
            }

            // Debug: List all children and their components
            Debug.Log("[CharacterSelectionSlot] Searching for UI components in children:");
            foreach (var child in GetComponentsInChildren<Transform>())
            {
                if (child != transform)
                {
                    var hasImage = child.GetComponent<Image>() != null;
                    var hasText = child.GetComponent<TextMeshProUGUI>() != null;
                    Debug.Log($"  Child '{child.name}' - HasImage: {hasImage}, HasTextTMP: {hasText}");
                }
            }

            // Find Image component (Button's default Image child)
            _characterImage = GetComponentInChildren<Image>();

            // Find TextMeshProUGUI component (Button's default Text child)
            _characterNameText = GetComponentInChildren<TextMeshProUGUI>();

            // Find optional SelectionIndicator by name
            foreach (var child in GetComponentsInChildren<Transform>())
            {
                if (child.name == "SelectionIndicator" && _selectionIndicator == null)
                    _selectionIndicator = child.GetComponent<Image>();
            }

            // Log results
            Debug.Log($"[CharacterSelectionSlot] Auto-discovery results - Image: {(_characterImage != null ? "Found" : "NOT FOUND")}, TextTMP: {(_characterNameText != null ? "Found" : "NOT FOUND")}");
            
            if (_characterImage == null)
                Debug.LogWarning("[CharacterSelectionSlot] No Image child found - character sprite won't display");
            if (_characterNameText == null)
                Debug.LogWarning("[CharacterSelectionSlot] No TextMeshProUGUI child found - character name won't display");
        }

        /// <summary>
        /// Initialize this slot with a character ID and manager references.
        /// Automatically fetches all character data FROM CharacterDatabase (sprite, name, description).
        /// </summary>
        public void Initialize(int characterId, Networking.Managers.CharacterSelectionManager selectionManager, NetworkRunner runner)
        {
            // Fetch character from database by ID
            _character = Networking.Managers.CharacterDatabase.Instance?.GetCharacterById(characterId);
            
            if (_character == null)
            {
                Debug.LogError($"[CharacterSelectionSlot] Character with ID {characterId} not found in CharacterDatabase!");
                return;
            }

            _selectionManager = selectionManager;
            _runner = runner;

            // Auto-discover UI components on first initialize
            AutoDiscoverComponents();

            // Automatically populate UI from CharacterDatabase
            if (_characterImage != null && _character.CharacterSprite != null)
            {
                _characterImage.sprite = _character.CharacterSprite;
                Debug.Log($"[CharacterSelectionSlot] Set sprite for '{_character.CharacterName}'");
            }
            else
            {
                Debug.LogWarning($"[CharacterSelectionSlot] Could not set sprite - Image null: {_characterImage == null}, Sprite null: {(_character.CharacterSprite == null)}");
            }

            if (_characterNameText != null)
            {
                _characterNameText.text = _character.CharacterName;
                Debug.Log($"[CharacterSelectionSlot] Set TEXT to '{_character.CharacterName}'");
            }
            else
            {
                Debug.LogWarning($"[CharacterSelectionSlot] Cannot set text - _characterNameText is NULL!");
            }

            // Bind button click
            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(OnSelectButtonClicked);
            }

            // Initial visual state
            UpdateVisualState();

            Debug.Log($"[CharacterSelectionSlot] Initialized slot for character ID {characterId} '{_character.CharacterName}'");
        }

        /// <summary>
        /// Set whether this character is available for selection.
        /// </summary>
        public void SetAvailable(bool isAvailable)
        {
            _isAvailable = isAvailable;
            UpdateVisualState();
        }

        /// <summary>
        /// Update the visual state based on availability and selection status.
        /// </summary>
        private void UpdateVisualState()
        {
            if (_selectButton == null)
                return;

            var localPlayer = _runner.LocalPlayer;
            var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(localPlayer, _runner);
            _isSelectedByLocalPlayer = (playerData != null && playerData.SelectedCharacterId == _character.CharacterId);

            // Determine color based on status
            Color displayColor = _availableColor;
            bool isInteractable = _isAvailable && !_isSelectedByLocalPlayer;

            if (_isSelectedByLocalPlayer)
            {
                displayColor = _selectedByYouColor;
                isInteractable = true; // Allow deselection
            }
            else if (!_isAvailable)
            {
                displayColor = _selectedByOtherColor;
                isInteractable = false;
            }

            // Apply visual feedback
            _selectButton.interactable = isInteractable;

            if (_characterImage != null)
                _characterImage.color = displayColor;

            if (_selectionIndicator != null)
            {
                _selectionIndicator.gameObject.SetActive(_isSelectedByLocalPlayer);
            }

            Debug.Log($"[CharacterSelectionSlot] '{_character.CharacterName}' - Available: {_isAvailable}, SelectedByYou: {_isSelectedByLocalPlayer}");
        }

        /// <summary>
        /// Called when player clicks this character slot.
        /// </summary>
        private void OnSelectButtonClicked()
        {
            if (!_isAvailable && !_isSelectedByLocalPlayer)
            {
                Debug.LogWarning($"[CharacterSelectionSlot] Cannot select '{_character.CharacterName}' - not available.");
                return;
            }

            // If already selected by this player, deselect
            if (_isSelectedByLocalPlayer)
            {
                _selectionManager.DeselectCharacterForLocalPlayer();
                Debug.Log($"[CharacterSelectionSlot] Deselected '{_character.CharacterName}'");
            }
            else
            {
                // Select this character
                _selectionManager.SelectCharacterForLocalPlayer(_character.CharacterId);
                Debug.Log($"[CharacterSelectionSlot] Selected '{_character.CharacterName}'");
            }
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            }
        }
    }
}
