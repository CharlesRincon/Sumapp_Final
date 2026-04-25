using System;
using UnityEngine;

namespace Networking.Models
{
    [Serializable]
    public struct ProjectZoneEffect
    {
        public ColombiaZone Zone;
        [Tooltip("Added on top of the project's base water income when the player is in this zone.")]
        public int BonusWaterPerRound;
        [Tooltip("Added on top of the project's base money income when the player is in this zone.")]
        public int BonusMoneyPerRound;
    }

    [CreateAssetMenu(fileName = "Project_", menuName = "Networking/Project Definition")]
    public class ProjectDefinition : ScriptableObject
    {
        [SerializeField] private int _projectId = 1;
        [SerializeField] private string _displayName = "New Project";
        [SerializeField] private int _price = 3;
        [SerializeField] private string _markerId;
        [Header("Base income (always applied)")]
        [SerializeField] private int _baseWaterPerRound = 1;
        [SerializeField] private int _baseMoneyPerRound = 1;
        [Header("Per-zone bonuses (optional, added on top of base)")]
        [SerializeField] private ProjectZoneEffect[] _zoneEffects = Array.Empty<ProjectZoneEffect>();

        public int ProjectId => _projectId;
        public string DisplayName => _displayName;
        public int Price => _price;
        public string MarkerId => _markerId;
        public int BaseWaterPerRound => _baseWaterPerRound;
        public int BaseMoneyPerRound => _baseMoneyPerRound;

        /// <summary>
        /// Returns the total water and money income for the given zone.
        /// Always includes the base income; zone effects add a bonus on top.
        /// </summary>
        public (int water, int money) GetIncomeForZone(ColombiaZone zone)
        {
            int water = _baseWaterPerRound;
            int money = _baseMoneyPerRound;

            if (_zoneEffects != null)
            {
                for (int i = 0; i < _zoneEffects.Length; i++)
                {
                    if (_zoneEffects[i].Zone == zone)
                    {
                        water += _zoneEffects[i].BonusWaterPerRound;
                        money += _zoneEffects[i].BonusMoneyPerRound;
                        break;
                    }
                }
            }

            return (water, money);
        }

        /// <summary>Legacy overload kept for backward compatibility.</summary>
        public ProjectZoneEffect GetEffectForZone(ColombiaZone zone)
        {
            var (water, money) = GetIncomeForZone(zone);
            return new ProjectZoneEffect { Zone = zone, BonusWaterPerRound = water, BonusMoneyPerRound = money };
        }
    }
}