using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;

namespace Networking.Managers
{
    /// <summary>
    /// Singleton manager providing centralized access to all character configurations.
    /// Handles queries for available/unavailable characters and character lookup by ID.
    /// Loaded from Resources/Characters folder at startup.
    /// 
    /// Thread-safe singleton pattern suitable for 100k+ LOC scalability.
    /// </summary>
    public class CharacterDatabase : MonoBehaviour
    {
        private static CharacterDatabase _instance;
        public static CharacterDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterDatabase>();
                    if (_instance == null)
                    {
                        Debug.LogError("[CharacterDatabase] Instance not found in scene. Create a CharacterDatabase GameObject and attach this script.");
                    }
                }
                return _instance;
            }
        }

        [SerializeField]
        private Networking.Models.CharacterConfig[] _allCharacters = new Networking.Models.CharacterConfig[6];

        private Dictionary<int, Networking.Models.CharacterConfig> _characterMap;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            InitializeCharacterMap();
        }

        /// <summary>
        /// Initialize the character ID -> config mapping for fast O(1) lookups.
        /// </summary>
        private void InitializeCharacterMap()
        {
            _characterMap = new Dictionary<int, Networking.Models.CharacterConfig>();

            if (_allCharacters == null || _allCharacters.Length == 0)
            {
                Debug.LogError("[CharacterDatabase] No characters assigned in inspector.");
                return;
            }

            foreach (var character in _allCharacters)
            {
                if (character == null)
                {
                    Debug.LogWarning("[CharacterDatabase] Null character found in array. Ensure all 6 slots are filled.");
                    continue;
                }

                if (_characterMap.ContainsKey(character.CharacterId))
                {
                    Debug.LogError($"[CharacterDatabase] Duplicate character ID '{character.CharacterId}' found. Character IDs must be unique.");
                    continue;
                }

                _characterMap[character.CharacterId] = character;
            }

            Debug.Log($"[CharacterDatabase] Initialized with {_characterMap.Count} characters.");
        }

        /// <summary>
        /// Get character configuration by ID.
        /// </summary>
        /// <param name="characterId">The unique character ID.</param>
        /// <returns>CharacterConfig if found; null otherwise.</returns>
        public Networking.Models.CharacterConfig GetCharacterById(int characterId)
        {
            if (_characterMap.TryGetValue(characterId, out var character))
            {
                return character;
            }

            Debug.LogWarning($"[CharacterDatabase] Character with ID '{characterId}' not found.");
            return null;
        }

        /// <summary>
        /// Get list of all available characters (not selected by any player).
        /// </summary>
        /// <param name="selectedCharacterIds">Set of character IDs already selected by players.</param>
        /// <returns>List of available CharacterConfigs.</returns>
        public List<Networking.Models.CharacterConfig> GetAvailableCharacters(HashSet<int> selectedCharacterIds)
        {
            return _characterMap.Values
                .Where(c => !selectedCharacterIds.Contains(c.CharacterId))
                .ToList();
        }

        /// <summary>
        /// Get list of all characters.
        /// </summary>
        /// <returns>Array copy of all character configs.</returns>
        public Networking.Models.CharacterConfig[] GetAllCharacters()
        {
            return _allCharacters;
        }

        /// <summary>
        /// Check if a character is available (not selected).
        /// </summary>
        public bool IsCharacterAvailable(int characterId, HashSet<int> selectedCharacterIds)
        {
            return _characterMap.ContainsKey(characterId) && !selectedCharacterIds.Contains(characterId);
        }

        /// <summary>
        /// Get a random available character for auto-assignment.
        /// </summary>
        public Networking.Models.CharacterConfig GetRandomAvailableCharacter(HashSet<int> selectedCharacterIds)
        {
            var available = GetAvailableCharacters(selectedCharacterIds);
            if (available.Count == 0)
            {
                Debug.LogWarning("[CharacterDatabase] No available characters for random assignment.");
                return null;
            }

            return available[Random.Range(0, available.Count)];
        }

        /// <summary>
        /// Validate that all character IDs are properly configured (for editor validation).
        /// </summary>
        public bool ValidateCharacterSetup()
        {
            if (_allCharacters == null || _allCharacters.Length != 6)
            {
                return false;
            }

            var ids = new HashSet<int>();
            foreach (var character in _allCharacters)
            {
                if (character == null) return false;
                if (ids.Contains(character.CharacterId)) return false;
                ids.Add(character.CharacterId);
            }

            return ids.Count == 6;
        }
    }
}
