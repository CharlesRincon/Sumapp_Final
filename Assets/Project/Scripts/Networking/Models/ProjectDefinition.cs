using System;
using UnityEngine;

namespace Networking.Models
{
    [Flags]
    public enum ProjectPassiveBehaviour
    {
        None                       = 0,
        /// <summary>Water income ignores all round water-gain penalties from cards.</summary>
        BypassesRoundWaterPenalty  = 1 << 0,
        /// <summary>Water income is doubled when basin health is below BasinThresholdForBonus.</summary>
        DoublesWaterBelowBasinThreshold = 1 << 1,
        /// <summary>Water income is set to zero while a Drought event is active this round.</summary>
        NullifiedByDroughtEvent    = 1 << 2,
        /// <summary>Money income is reduced by the active Deforestation event penalty this round.</summary>
        ReducedByDeforestationEvent = 1 << 4,
        /// <summary>Adds ClimateEventMoneyBonus to money income while any weather is active.</summary>
        BonusMoneyDuringWeather = 1 << 5,
        /// <summary>Grants contextual water bonuses (on gain) or penalties (on loss) based on tile type and weather.</summary>
        ContextualWaterModifier = 1 << 6,
        /// <summary>Recovers basin health at the start of each round.</summary>
        BasinRecoveryPerRound = 1 << 7,
        /// <summary>Grants a water bonus at the end of the game if basin health is above a threshold.</summary>
        EndGameBasinBonus = 1 << 8,
        /// <summary>Water income is doubled when basin health is ABOVE BasinThresholdForBonus.</summary>
        DoublesWaterAboveBasinThreshold = 1 << 9,
        /// <summary>Adds ClimateEventMoneyBonus to money income specifically during Drought weather.</summary>
        BonusMoneyDuringDrought = 1 << 10,
        /// <summary>Decreases basin health at the start of each round.</summary>
        BasinLossPerRound = 1 << 11,
        /// <summary>Money income is set to zero when basin health is BELOW BasinThresholdForBonus.</summary>
        NullifiedMoneyBelowBasinThreshold = 1 << 12,
        /// <summary>Adds ClimateEventMoneyBonus during Rain and subtracts it during Drought.</summary>
        SymmetricWeatherMoney = 1 << 13,
    }

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
        [Header("Description")]
        [SerializeField, TextArea(3, 10)] private string _description;
        [Header("Base income (always applied)")]
        [SerializeField] private int _baseWaterPerRound = 1;
        [SerializeField] private int _baseMoneyPerRound = 1;
        [Header("Per-zone bonuses (optional, added on top of base)")]
        [SerializeField] private ProjectZoneEffect[] _zoneEffects = Array.Empty<ProjectZoneEffect>();

        [Header("Passive Behaviours")]
        [Tooltip("Flags that control special runtime rules for this project's passive income.")]
        [SerializeField] private ProjectPassiveBehaviour _passiveBehaviours = ProjectPassiveBehaviour.None;
        [Tooltip("Basin health fraction (0-1) below which DoublesWaterBelowBasinThreshold activates. Default 0.3 = 30%.")]
        [SerializeField] private float _basinThresholdForBonus = 0.3f;
        [Tooltip("Extra money added per round when BonusMoneyDuringWeather is active.")]
        [SerializeField] private int _climateEventMoneyBonus = 1;

        [Header("Contextual Water Modifier Settings")]
        [Tooltip("Bonus water added on any water gain if ContextualWaterModifier is enabled.")]
        [SerializeField] private int _contextualWaterBonusNormal = 1;
        [Tooltip("Bonus water added during enhanced conditions (e.g. La Niña or Hydric zones) if ContextualWaterModifier is enabled.")]
        [SerializeField] private int _contextualWaterBonusEnhanced = 2;
        [Tooltip("Penalty water subtracted during negative conditions (e.g. Catastrophic zones) if ContextualWaterModifier is enabled.")]
        [SerializeField] private int _contextualWaterPenalty = 1;

        [Header("Basin Recovery Settings")]
        [Tooltip("Flat amount of basin health recovered per round if BasinRecoveryPerRound is enabled.")]
        [SerializeField] private int _basinRecoveryNormalAmount = 3;
        [Tooltip("Flat amount of basin health recovered per round during positive weather if BasinRecoveryPerRound is enabled.")]
        [SerializeField] private int _basinRecoveryEnhancedAmount = 5;

        [Header("Basin Loss Settings")]
        [Tooltip("Flat amount of basin health lost per round if BasinLossPerRound is enabled.")]
        [SerializeField] private int _basinLossNormalAmount = 3;
        [Tooltip("Flat amount of basin health lost per round during Drought weather if BasinLossPerRound is enabled.")]
        [SerializeField] private int _basinLossDroughtAmount = 8;

        [Header("End Game Bonus Settings")]
[Tooltip("Water bonus granted at the end of the game if EndGameBasinBonus is enabled and threshold met.")]
        [SerializeField] private int _endGameWaterBonus = 5;
        [Tooltip("Basin health fraction (0-1) required at end of game for water bonus.")]
        [SerializeField] private float _endGameBasinThreshold = 0.5f;

        public int ProjectId => _projectId;
public string DisplayName => _displayName;
        public int Price => _price;
        public string MarkerId => _markerId;
        public string Description => _description;
        public int BaseWaterPerRound => _baseWaterPerRound;
        public int BaseMoneyPerRound => _baseMoneyPerRound;
        public ProjectPassiveBehaviour PassiveBehaviours => _passiveBehaviours;
        public float BasinThresholdForBonus => _basinThresholdForBonus;
        public int ClimateEventMoneyBonus => _climateEventMoneyBonus;
        public int ContextualWaterBonusNormal => _contextualWaterBonusNormal;
        public int ContextualWaterBonusEnhanced => _contextualWaterBonusEnhanced;
        public int ContextualWaterPenalty => _contextualWaterPenalty;
        public int BasinRecoveryNormalAmount => _basinRecoveryNormalAmount;
        public int BasinRecoveryEnhancedAmount => _basinRecoveryEnhancedAmount;
        public int BasinLossNormalAmount => _basinLossNormalAmount;
        public int BasinLossDroughtAmount => _basinLossDroughtAmount;
        public int EndGameWaterBonus => _endGameWaterBonus;
public float EndGameBasinThreshold => _endGameBasinThreshold;

        public bool HasBehaviour(ProjectPassiveBehaviour flag) => (_passiveBehaviours & flag) != 0;

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