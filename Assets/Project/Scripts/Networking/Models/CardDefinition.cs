using UnityEngine;

namespace Networking.Models
{
    public enum CardDecisionScope
    {
        None,
        Individual,
        Collective
    }

    [System.Serializable]
    public class CardDecisionChoice
    {
        [SerializeField] public string Label = "Option";
        [SerializeField] public int WaterDelta;
        [SerializeField] public bool WaterDeltaIsPercent;
        [SerializeField] public int MoneyDelta;
        [SerializeField] public bool MoneyDeltaIsPercent;
        [SerializeField] public int BasinDelta;
        [SerializeField] public int AllPlayersWaterDelta;
        [SerializeField] public bool AllPlayersWaterDeltaIsPercent;
        [SerializeField] public int AllPlayersMoneyDelta;
        [SerializeField] public bool AllPlayersMoneyDeltaIsPercent;
        [SerializeField] public int DiceModifier;
        [SerializeField] public bool GrantsNegativeShield;
        [SerializeField] public int RoundWaterGainPenalty;
        [SerializeField] public bool RoundWaterGainPenaltyIsPercent;
        [SerializeField] public int RoundWaterGainBonus;
        [SerializeField] public bool RoundWaterGainBonusIsPercent;
        [SerializeField] public int RoundMoneyGainPenalty;
        [SerializeField] public bool RoundMoneyGainPenaltyIsPercent;
        [SerializeField] public int RoundMoneyGainBonus;
        [SerializeField] public bool RoundMoneyGainBonusIsPercent;
        [SerializeField] public int RoundProjectMoneyPenalty;
        [SerializeField] public bool RoundProjectMoneyPenaltyIsPercent;
        [SerializeField] public int RoundProjectMoneyBonus;
        [SerializeField] public bool RoundProjectMoneyBonusIsPercent;
        [Header("Named Events (affect project passives this round)")]
        [Tooltip("Activates Drought for the rest of this round — nullifies water income of projects with NullifiedByDroughtEvent.")]
        [SerializeField] public bool IsDroughtEvent;
        [Tooltip("Activates a Climate event — boosts money income of projects with BonusMoneyFromClimateEvent.")]
        [SerializeField] public bool IsClimateEvent;
        [Tooltip("Activates Deforestation — applies a money penalty to projects with ReducedByDeforestationEvent.")]
        [SerializeField] public bool IsDeforestationEvent;
        [Tooltip("How much % to reduce deforestation-affected project money income (0-100).")]
        [SerializeField] public int DeforestationProjectMoneyPercentPenalty;
    }

    [CreateAssetMenu(fileName = "Card_", menuName = "Networking/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int _cardId = 1;
        [SerializeField] private string _displayName = "New Card";

        [Header("Direct Effects (scanning player)")]
        [SerializeField] private int _waterDelta;
        [SerializeField] private bool _waterDeltaIsPercent;
        [SerializeField] private int _moneyDelta;
        [SerializeField] private bool _moneyDeltaIsPercent;
        [SerializeField] private int _basinDelta;
        [SerializeField] private int _diceModifier;
        [SerializeField] private bool _grantsNegativeShield;

        [Header("Global Effects (all players)")]
        [SerializeField] private int _allPlayersWaterDelta;
        [SerializeField] private bool _allPlayersWaterDeltaIsPercent;
        [SerializeField] private int _allPlayersMoneyDelta;
        [SerializeField] private bool _allPlayersMoneyDeltaIsPercent;

        [Header("Round Modifier — Penalties")]
        [Tooltip("Reduces all players' positive water gains for the rest of this round. Toggle IsPercent to treat value as % (0-100).")]
        [SerializeField] private int _roundWaterGainPenalty;
        [SerializeField] private bool _roundWaterGainPenaltyIsPercent;
        [Tooltip("Reduces all players' positive money gains for the rest of this round. Toggle IsPercent to treat value as % (0-100).")]
        [SerializeField] private int _roundMoneyGainPenalty;
        [SerializeField] private bool _roundMoneyGainPenaltyIsPercent;
        [Tooltip("Reduces money income from owned projects for the rest of this round. Toggle IsPercent to treat value as % (0-100).")]
        [SerializeField] private int _roundProjectMoneyPenalty;
        [SerializeField] private bool _roundProjectMoneyPenaltyIsPercent;

        [Header("Round Modifier — Bonuses")]
        [Tooltip("Increases all players' positive water gains for the rest of this round. Toggle IsPercent to treat value as %.")]
        [SerializeField] private int _roundWaterGainBonus;
        [SerializeField] private bool _roundWaterGainBonusIsPercent;
        [Tooltip("Increases all players' positive money gains for the rest of this round. Toggle IsPercent to treat value as %.")]
        [SerializeField] private int _roundMoneyGainBonus;
        [SerializeField] private bool _roundMoneyGainBonusIsPercent;
        [Tooltip("Increases money income from owned projects for the rest of this round. Toggle IsPercent to treat value as %.")]
        [SerializeField] private int _roundProjectMoneyBonus;
        [SerializeField] private bool _roundProjectMoneyBonusIsPercent;

        [Header("Teleport")]
        [Tooltip("Move scanning player to this board index and resolve the tile. -1 = no teleport.")]
        [SerializeField] private int _selfMoveToTile = -1;

        [Header("Named Events (affect project passives this round)")]
        [Tooltip("Activates Drought for the rest of this round — nullifies water income of projects with NullifiedByDroughtEvent.")]
        [SerializeField] private bool _isDroughtEvent;
        [Tooltip("Activates a Climate event — boosts money income of projects with BonusMoneyFromClimateEvent.")]
        [SerializeField] private bool _isClimateEvent;
        [Tooltip("Activates Deforestation — applies a money penalty to projects with ReducedByDeforestationEvent.")]
        [SerializeField] private bool _isDeforestationEvent;
        [Tooltip("How much % to reduce deforestation-affected project money income (0-100).")]
        [SerializeField] private int _deforestationProjectMoneyPercentPenalty;

        [Header("Decision")]
        [SerializeField] private bool _requiresDecision;
        [SerializeField] private CardDecisionScope _decisionScope = CardDecisionScope.None;
        [SerializeField] private CardDecisionChoice _decisionChoiceA = new CardDecisionChoice { Label = "Option A" };
        [SerializeField] private CardDecisionChoice _decisionChoiceB = new CardDecisionChoice { Label = "Option B" };

        [Header("Climate (stub — not yet implemented)")]
        [Tooltip("Reserved for future climate system. -1 = no effect.")]
        [SerializeField] private int _climateEffect = -1;

        // --- Public API ---
        public int CardId             => _cardId;
        public string DisplayName     => _displayName;

        public int WaterDelta              => _waterDelta;
        public bool WaterDeltaIsPercent    => _waterDeltaIsPercent;
        public int MoneyDelta              => _moneyDelta;
        public bool MoneyDeltaIsPercent    => _moneyDeltaIsPercent;
        public int BasinDelta              => _basinDelta;
        public int DiceModifier            => _diceModifier;
        public bool GrantsNegativeShield   => _grantsNegativeShield;

        public int AllPlayersWaterDelta              => _allPlayersWaterDelta;
        public bool AllPlayersWaterDeltaIsPercent    => _allPlayersWaterDeltaIsPercent;
        public int AllPlayersMoneyDelta              => _allPlayersMoneyDelta;
        public bool AllPlayersMoneyDeltaIsPercent    => _allPlayersMoneyDeltaIsPercent;

        public int RoundWaterGainPenalty              => _roundWaterGainPenalty;
        public bool RoundWaterGainPenaltyIsPercent    => _roundWaterGainPenaltyIsPercent;
        public int RoundWaterGainBonus                => _roundWaterGainBonus;
        public bool RoundWaterGainBonusIsPercent      => _roundWaterGainBonusIsPercent;
        public int RoundMoneyGainPenalty              => _roundMoneyGainPenalty;
        public bool RoundMoneyGainPenaltyIsPercent    => _roundMoneyGainPenaltyIsPercent;
        public int RoundMoneyGainBonus                => _roundMoneyGainBonus;
        public bool RoundMoneyGainBonusIsPercent      => _roundMoneyGainBonusIsPercent;
        public int RoundProjectMoneyPenalty           => _roundProjectMoneyPenalty;
        public bool RoundProjectMoneyPenaltyIsPercent => _roundProjectMoneyPenaltyIsPercent;
        public int RoundProjectMoneyBonus             => _roundProjectMoneyBonus;
        public bool RoundProjectMoneyBonusIsPercent   => _roundProjectMoneyBonusIsPercent;

        public int SelfMoveToTile     => _selfMoveToTile;

        public bool RequiresDecision         => _requiresDecision;
        public CardDecisionScope DecisionScope => _decisionScope;
        public CardDecisionChoice DecisionChoiceA => _decisionChoiceA;
        public CardDecisionChoice DecisionChoiceB => _decisionChoiceB;

        public bool IsDroughtEvent                       => _isDroughtEvent;
        public bool IsClimateEvent                       => _isClimateEvent;
        public bool IsDeforestationEvent                 => _isDeforestationEvent;
        public int  DeforestationProjectMoneyPercentPenalty => _deforestationProjectMoneyPercentPenalty;
    }
}
