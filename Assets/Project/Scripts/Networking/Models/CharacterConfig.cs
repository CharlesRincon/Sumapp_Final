using UnityEngine;
using Fusion;

namespace Networking.Models
{
    /// <summary>
    /// ScriptableObject containing configuration data for a single playable character.
    /// Data-driven approach allows designers to create/modify characters without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "Character_", menuName = "Networking/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        [field: SerializeField]
        public int CharacterId { get; private set; }

        [field: SerializeField]
        public string CharacterName { get; private set; }

        [field: SerializeField]
        [field: TextArea(2, 4)]
        public string Description { get; private set; }

        [field: SerializeField]
        public Sprite CharacterSprite { get; private set; }

        [field: SerializeField]
        public Sprite TurnImage { get; private set; }

        [field: SerializeField]
        public Color CharacterColor { get; private set; } = Color.white;

        [field: SerializeField]
        public NetworkPrefabRef CharacterPrefab { get; private set; }

        /// <summary>
        /// Optional: character-specific stats for gameplay (attack, defense, health, etc.)
        /// Can be expanded for game-specific mechanics.
        /// </summary>
        [field: SerializeField]
        public CharacterStats Stats { get; private set; }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"CharacterConfig asset name is empty. Please rename asset to 'Character_{CharacterId}'", this);
            }
        }
    }

    /// <summary>
    /// Character-specific gameplay stats (expandable based on game design).
    /// </summary>
    [System.Serializable]
    public struct CharacterStats
    {
        [SerializeField]
        public int Health;

        [SerializeField]
        public int AttackPower;

        [SerializeField]
        public int Defense;

        [SerializeField]
        public float AttackSpeed;

        [SerializeField]
        public float MovementSpeed;

        public CharacterStats(int health = 100, int attack = 10, int defense = 5, float atkSpeed = 1f, float moveSpeed = 5f)
        {
            Health = health;
            AttackPower = attack;
            Defense = defense;
            AttackSpeed = atkSpeed;
            MovementSpeed = moveSpeed;
        }
    }
}
