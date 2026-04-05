using UnityEngine;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Managers
{
    /// <summary>
    /// Manages character selection phase during multiplayer lobby.
    /// Only the host runs this logic (StateAuthority).
    /// 
    /// Features:
    /// - 30-second timer for character selection
    /// - Auto-assigns random character to players who didn't select
    /// - Fires event when all players selected or timer expires
    /// - Syncs timer state across all clients via [Networked] properties
    /// 
    /// Architecture: NetworkBehaviour ensures only StateAuthority executes selection logic,
    /// preventing double-assignment or race conditions in multiplayer.
    /// </summary>
    public class CharacterSelectionManager : NetworkBehaviour
    {
        [SerializeField]
        private float _selectionTimeoutSeconds = 30f;

        /// <summary>
        /// Event fired when character selection phase completes (all selected OR timer expired).
        /// Parameter: PlayerRef (unused), NetworkRunner.
        /// </summary>
        [SerializeField]
        private FusionEvent OnCharacterSelectionCompleteEvent;

        /// <summary>
        /// Event fired every tick to update UI countdown display (e.g., "15 seconds remaining").
        /// Parameter: PlayerRef (unused), NetworkRunner.
        /// </summary>
        [SerializeField]
        private FusionEvent OnSelectionTimeRemainingEvent;

        /// <summary>
        /// Public property to access the selection complete event.
        /// </summary>
        public FusionEvent SelectionCompleteEvent => OnCharacterSelectionCompleteEvent;

        /// <summary>
        /// Public property to access the time remaining event.
        /// </summary>
        public FusionEvent TimeRemainingEvent => OnSelectionTimeRemainingEvent;

        // Network-synchronized state (synced to all clients)
        [Networked]
        private float RemainingTime { get; set; }

        [Networked]
        private NetworkBool SelectionPhaseActive { get; set; }

        // Runtime references (NOT serialized)
        [System.NonSerialized]
        private NetworkRunner _runner;

        [System.NonSerialized]
        private bool _hasAutoAssignedRemaining = false;

        public float GetRemainingTime() => RemainingTime;
        public bool IsSelectionActive() => SelectionPhaseActive;

        public override void Spawned()
        {
            _runner = Runner;

            // Only host (StateAuthority) initializes the phase
            if (Object.HasStateAuthority)
            {
                RemainingTime = _selectionTimeoutSeconds;
                SelectionPhaseActive = true;
                _hasAutoAssignedRemaining = false;
                Debug.Log($"[CharacterSelectionManager] Selection phase started. {_selectionTimeoutSeconds}s timeout.");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (!SelectionPhaseActive)
                return;

            // Count players who haven't selected a character yet
            int playersWithoutSelection = 0;
            foreach (var player in _runner.ActivePlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                if (playerData == null || playerData.SelectedCharacterId == 0)
                {
                    playersWithoutSelection++;
                }
            }

            // Case 1: All players have selected - complete immediately
            if (playersWithoutSelection == 0)
            {
                Debug.Log("[CharacterSelectionManager] All players selected characters. Ending selection phase early.");
                CompleteSelectionPhase();
                return;
            }

            // Countdown timer
            RemainingTime -= _runner.DeltaTime;

            // Fire event every frame for UI countdown update
            OnSelectionTimeRemainingEvent?.Raise(PlayerRef.None, _runner);

            // Case 2: Timer expired - auto-assign remaining players
            if (RemainingTime <= 0f && !_hasAutoAssignedRemaining)
            {
                RemainingTime = 0f;
                Debug.Log("[CharacterSelectionManager] Selection timeout. Auto-assigning remaining players.");
                AutoAssignRemainingPlayers();
                _hasAutoAssignedRemaining = true;
                CompleteSelectionPhase();
            }
        }

        /// <summary>
        /// Auto-assign a random available character to players who haven't selected yet.
        /// Only called by host (StateAuthority).
        /// Assigns to ALL players in the room who haven't selected.
        /// </summary>
        private void AutoAssignRemainingPlayers()
        {
            var selectedIds = GameManager.Instance.GetSelectedCharacterIds(_runner);
            int assignedCount = 0;

            Debug.Log($"[CharacterSelectionManager] Auto-assigning characters. Active players: {_runner.ActivePlayers.Count()}");

            foreach (var player in _runner.ActivePlayers)
            {
                var playerData = GameManager.Instance.GetPlayerData(player, _runner);
                if (playerData == null)
                {
                    Debug.LogWarning($"[CharacterSelectionManager] Player {player.PlayerId} has no PlayerSessionData!");
                    continue;
                }

                if (playerData.SelectedCharacterId == 0)
                {
                    var randomChar = CharacterDatabase.Instance.GetRandomAvailableCharacter(selectedIds);
                    if (randomChar != null)
                    {
                        // Call RPC on the player's data - host can now call this for auto-assignment
                        playerData.RPC_SetSelectedCharacter(randomChar.CharacterId);
                        selectedIds.Add(randomChar.CharacterId);
                        assignedCount++;
                        Debug.Log($"[CharacterSelectionManager] Auto-assigned character '{randomChar.CharacterName}' (ID {randomChar.CharacterId}) to player {player.PlayerId}");
                    }
                    else
                    {
                        Debug.LogWarning("[CharacterSelectionManager] No available characters to auto-assign.");
                    }
                }
                else
                {
                    Debug.Log($"[CharacterSelectionManager] Player {player.PlayerId} already selected character ID {playerData.SelectedCharacterId}");
                }
            }

            Debug.Log($"[CharacterSelectionManager] Auto-assignment complete. {assignedCount} players new assigned characters.");
        }

        /// <summary>
        /// Complete the selection phase and notify all listeners.
        /// </summary>
        private void CompleteSelectionPhase()
        {
            SelectionPhaseActive = false;
            OnCharacterSelectionCompleteEvent?.Raise(PlayerRef.None, _runner);
        }

        /// <summary>
        /// Called by UI/input system to select a character for the local player.
        /// Validates availability before allowing selection.
        /// </summary>
        public void SelectCharacterForLocalPlayer(int characterId)
        {
            if (!SelectionPhaseActive)
            {
                Debug.LogWarning("[CharacterSelectionManager] Character selection phase is not active.");
                return;
            }

            var localPlayer = _runner.LocalPlayer;
            var playerData = GameManager.Instance.GetPlayerData(localPlayer, _runner);

            if (playerData == null)
            {
                Debug.LogError("[CharacterSelectionManager] Local player data not found.");
                return;
            }

            // Validate character exists
            if (CharacterDatabase.Instance.GetCharacterById(characterId) == null)
            {
                Debug.LogWarning($"[CharacterSelectionManager] Invalid character ID: {characterId}");
                return;
            }

            // Validate character is available
            if (!GameManager.Instance.IsCharacterAvailable(characterId, _runner))
            {
                Debug.LogWarning($"[CharacterSelectionManager] Character {characterId} is not available.");
                return;
            }

            // Send RPC to set character (host authority)
            playerData.RPC_SetSelectedCharacter(characterId);
            Debug.Log($"[CharacterSelectionManager] Player {localPlayer.PlayerId} selected character {characterId}");
        }

        /// <summary>
        /// Called by UI to deselect current character (allows re-selection).
        /// </summary>
        public void DeselectCharacterForLocalPlayer()
        {
            if (!SelectionPhaseActive)
            {
                Debug.LogWarning("[CharacterSelectionManager] Character selection phase is not active.");
                return;
            }

            var localPlayer = _runner.LocalPlayer;
            var playerData = GameManager.Instance.GetPlayerData(localPlayer, _runner);

            if (playerData == null)
            {
                Debug.LogError("[CharacterSelectionManager] Local player data not found.");
                return;
            }

            playerData.RPC_ClearSelectedCharacter();
        }
    }
}
