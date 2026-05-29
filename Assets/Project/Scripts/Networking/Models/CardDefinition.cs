using UnityEngine;
using UnityEngine.Serialization;

namespace Networking.Models
{
    public enum CardDecisionScope
    {
        None,
        Individual,
        Collective
    }

    public enum WeatherTag
    {
        None,
        Rain,
        Drought,
        Flood,
        Freeze
    }

    [System.Serializable]
    public class CardDecisionChoice
    {
        [SerializeField] public string Label = "Option";
        [SerializeField] public int WaterDelta;
        [SerializeField] public int MoneyDelta;
        [SerializeField] public int BasinDelta;
        [SerializeField] public int AllPlayersWaterDelta;
        [SerializeField] public int AllPlayersMoneyDelta;
        [SerializeField] public int DiceModifier;
        [SerializeField] public bool GrantsNegativeShield;
        [SerializeField] public int RoundWaterGainPenalty;
        [SerializeField] public int RoundWaterGainBonus;
        [SerializeField] public int RoundMoneyGainPenalty;
        [SerializeField] public int RoundMoneyGainBonus;
        [SerializeField] public int RoundProjectMoneyPenalty;
        [SerializeField] public int RoundProjectMoneyBonus;
        [Header("Named Events (affect project passives this round)")]
        [Tooltip("Activates Drought for the rest of this round — nullifies water income of projects with NullifiedByDroughtEvent.")]
        [SerializeField] public bool IsDroughtEvent;
        [Tooltip("Activates a Climate event.")]
        [SerializeField] public bool IsClimateEvent;
        [Tooltip("Activates Deforestation — applies a money penalty to projects with ReducedByDeforestationEvent.")]
        [SerializeField] public bool IsDeforestationEvent;
        [Tooltip("How much % to reduce deforestation-affected project money income (0-100).")]
        [SerializeField] public int DeforestationProjectMoneyPercentPenalty;
    }

    public enum TeleportMode
    {
        None,
        ToSpecificIndex,
        ToNearestTileType
    }

    [CreateAssetMenu(fileName = "Card_", menuName = "Networking/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int _cardId = 1;
        [SerializeField] private string _displayName = "New Card";
        [TextArea(2, 6)]
        [SerializeField] private string _loreText = "";
        [TextArea(2, 6)]
        [SerializeField] private string _effectDescription = "";

        [Header("Direct Effects (scanning player)")]
        [SerializeField] private int _waterDelta;
        [SerializeField] private int _moneyDelta;
        [SerializeField] private int _basinDelta;
        [SerializeField] private int _diceModifier;
        [SerializeField] private bool _grantsNegativeShield;
        [Tooltip("If true, the next trivia reward obtained by the scanning player is doubled.")]
        [SerializeField] private bool _grantsDoubleTriviaReward;

        [Header("Global Effects (all players)")]
        [SerializeField] private int _allPlayersWaterDelta;
        [SerializeField] private int _allPlayersMoneyDelta;

        [Header("Round Modifier — Penalties")]
        [Tooltip("Reduces all players' positive water gains for the rest of this round.")]
        [SerializeField] private int _roundWaterGainPenalty;
        [Tooltip("Reduces all players' positive money gains for the rest of this round.")]
        [SerializeField] private int _roundMoneyGainPenalty;
        [Tooltip("Reduces money income from owned projects for the rest of this round.")]
        [SerializeField] private int _roundProjectMoneyPenalty;

        [Header("Round Modifier — Bonuses")]
        [Tooltip("Increases all players' positive water gains for the rest of this round.")]
        [SerializeField] private int _roundWaterGainBonus;
        [Tooltip("Increases all players' positive money gains for the rest of this round.")]
        [SerializeField] private int _roundMoneyGainBonus;
        [Tooltip("Increases money income from owned projects for the rest of this round.")]
        [SerializeField] private int _roundProjectMoneyBonus;

        [Header("Teleport")]
        [Tooltip("Method of teleportation for the scanning player.")]
        [SerializeField] private TeleportMode _teleportMode = TeleportMode.None;
        [Tooltip("Used if TeleportMode is ToSpecificIndex. -1 = no teleport.")]
        [SerializeField] private int _selfMoveToTile = -1;
        [Tooltip("Used if TeleportMode is ToNearestTileType. Moves to the nearest tile of this type.")]
        [SerializeField] private Networking.Services.SliceTileType _teleportTargetTileType;

        [Header("Named Events (affect project passives this round)")]
        [Tooltip("Activates Drought for the rest of this round — nullifies water income of projects with NullifiedByDroughtEvent.")]
        [SerializeField] private bool _isDroughtEvent;
        [Tooltip("Activates a Climate event.")]
        [SerializeField] private bool _isClimateEvent;
        [Tooltip("Activates Deforestation — applies a money penalty to projects with ReducedByDeforestationEvent.")]
        [SerializeField] private bool _isDeforestationEvent;
        [Tooltip("How much % to reduce deforestation-affected project money income (0-100).")]
        [SerializeField] private int _deforestationProjectMoneyPercentPenalty;

        [Header("Basin Threshold Effect")]
        [Tooltip("If true, applies a specific basin delta based on whether the current health is above or below a threshold.")]
        [SerializeField] private bool _useBasinThresholdDelta;
        [Tooltip("If true, the threshold effect is applied immediately when the card is scanned.")]
        [SerializeField] private bool _applyBasinThresholdOnScan = true;
        [Tooltip("The threshold percentage (0.0 to 1.0) of the starting basin health.")]
        [Range(0f, 1f)]
        [SerializeField] private float _basinThresholdPercentage = 0.5f;
        [Tooltip("Basin delta applied if health is ABOVE the threshold.")]
        [SerializeField] private int _basinDeltaAboveThreshold = 8;
        [Tooltip("Basin delta applied if health is BELOW or EQUAL to the threshold.")]
        [SerializeField] private int _basinDeltaBelowThreshold = -8;

        [Header("Money Basin Threshold Effect")]
        [Tooltip("If true, applies a specific money delta based on whether the current basin health is above or below a threshold.")]
        [SerializeField] private bool _useBasinThresholdMoneyDelta;
        [Tooltip("Money delta applied if health is ABOVE the threshold.")]
        [SerializeField] private int _moneyDeltaAboveThreshold;
        [Tooltip("Money delta applied if health is BELOW or EQUAL to the threshold.")]
        [SerializeField] private int _moneyDeltaBelowThreshold;

        [Header("Basin Threshold Double Deltas")]
        [Tooltip("If true, doubles WaterDelta, MoneyDelta, and BasinDelta if current basin health is BELOW or EQUAL to the threshold.")]
        [SerializeField] private bool _useBasinThresholdDoubleDeltas;

        [Header("Turn Order")]
        [Tooltip("If true, scanning this card will cause the turn order to be reversed at the start of the next round.")]
        [SerializeField] private bool _invertsTurnOrder;

        [Header("Decision")]
        [SerializeField] private bool _requiresDecision;
        [SerializeField] private CardDecisionScope _decisionScope = CardDecisionScope.None;
        [SerializeField] private CardDecisionChoice _decisionChoiceA = new CardDecisionChoice { Label = "Option A" };
        [SerializeField] private CardDecisionChoice _decisionChoiceB = new CardDecisionChoice { Label = "Option B" };

        [Header("Conditional Money (project ownership)")]
        [Tooltip("If true, ignores MoneyDelta and applies MoneyWithActiveProject or MoneyWithoutActiveProject based on how many projects the scanning player owns.")]
        [SerializeField] private bool _conditionalMoneyOnProjects;
        [Tooltip("Money delta applied when the player owns at least 1 project.")]
        [SerializeField] private int _moneyWithActiveProject;
        [Tooltip("Money delta applied when the player owns no projects.")]
        [SerializeField] private int _moneyWithoutActiveProject;

        [Header("Weather Card")]
        [Tooltip("If true, this card activates a multi-round weather effect that overwrites any currently active weather.")]
        [SerializeField] private bool _isWeatherCard;
        [Tooltip("Identifies this weather card's type so that other cards can conditionally react to it.")]
        [SerializeField] private WeatherTag _weatherTag;
        [Tooltip("Extra rounds the weather persists after the scan round. Scan round = round 0, so duration 2 = active on scan round + 2 more rounds (3 total).")]
        [SerializeField] private int _weatherDurationRounds;
        [Tooltip("Signed hydric bonus while weather is active. Positive increases water gain, negative decreases it. Final hydric gain is clamped to minimum 1.")]
        [FormerlySerializedAs("_weatherHydricWaterFlatPenalty")]
        [SerializeField] private int _weatherHydricWaterFlatBonus;
        [Tooltip("Water delta applied to each player at the start of their individual turn while weather is active (negative = loss). Ignores shield.")]
        [SerializeField] private int _weatherAllPlayersWaterPerTurnDelta;
        [Tooltip("Water delta applied to all players whenever any board-effect tile is resolved while weather is active.")]
        [SerializeField] private int _weatherAllPlayersWaterOnTileResolveDelta;
        [Tooltip("Flat bonus added to all validated turn dice rolls while weather is active.")]
        [SerializeField] private int _weatherDiceRollFlatBonus;
        [Tooltip("While active, each player automatically rolls a die at turn start. Result 4+: +4 water, +3 money. Result ≤3: -3 water, -3 money.")]
        [SerializeField] private bool _weatherRollDependentRewards;
        [Tooltip("Percentage by which all project income is reduced while weather is active (0-100).")]
        [SerializeField] private int _weatherProjectMoneyPercentPenalty;
        [Tooltip("Flat money bonus added to each player's project income per round while weather is active. Positive = bonus, negative = penalty. Only applies to players with at least one active project.")]
        [SerializeField] private int _weatherProjectMoneyFlatBonusPerRound;
        [Tooltip("If enabled, weather basin delta is applied at end of each round. If disabled, it applies at start of each round.")]
        [SerializeField] private bool _weatherApplyBasinDeltaAtRoundEnd = true;
        [Tooltip("Flat basin delta applied each round while weather is active. Positive value recovers basin health, negative value lowers it.")]
        [FormerlySerializedAs("_weatherBasinPercentPerRound")]
        [SerializeField] private int _weatherBasinFlatPerRound;
        [Tooltip("While this weather is active, all basin delta calls from any source are suppressed — the basin cannot be damaged or healed.")]
        [SerializeField] private bool _weatherLockBasin;
        [Tooltip("While this weather is active, hydric zones grant 0 water instead of their normal yield.")]
        [SerializeField] private bool _weatherNullifyHydricWater;
        [Tooltip("Flat water bonus added each round to each project that already generates water (BaseWaterPerRound > 0). Applied per project slot.")]
        [SerializeField] private int _weatherProjectWaterFlatBonusPerRound;
        [Tooltip("While active, any card scan with a positive BasinDelta has that delta doubled before it is applied.")]
        [SerializeField] private bool _weatherDoubleBasinRecovery;

        [Header("Conditional Water (weather-dependent)")]
        [Tooltip("If the currently active weather matches this tag, WaterDelta is replaced by ConditionalWaterDelta. Set to None to disable.")]
        [SerializeField] private WeatherTag _conditionalWaterIfWeatherTag;
        [Tooltip("Water delta applied instead of WaterDelta when the matching weather is active.")]
        [SerializeField] private int _conditionalWaterDelta;

        [Header("Weather Interaction")]
        [Tooltip("If true, scanning this card immediately terminates the currently active weather (if any).")]
        [SerializeField] private bool _terminatesActiveWeather;
        [Tooltip("Only terminates weather if the active tag matches this value. Set to None to terminate any weather regardless of tag.")]
        [SerializeField] private WeatherTag _terminatesWeatherTag;
        [Tooltip("Flat basin delta applied only when this card terminates weather. Positive = recover, negative = damage. E.g. 10 = recover 10 basin health.")]
        [SerializeField] private int _basinFlatOnWeatherTerminate;

        // --- Public API ---
        public int CardId             => _cardId;
        public string DisplayName     => _displayName;
        public string LoreText        => _loreText;
        public string EffectDescription => _effectDescription;

        public int WaterDelta            => _waterDelta;
        public int MoneyDelta            => _moneyDelta;
        public int BasinDelta            => _basinDelta;
        public int DiceModifier          => _diceModifier;
        public bool GrantsNegativeShield => _grantsNegativeShield;
        public bool GrantsDoubleTriviaReward => _grantsDoubleTriviaReward;

        public int AllPlayersWaterDelta => _allPlayersWaterDelta;
        public int AllPlayersMoneyDelta => _allPlayersMoneyDelta;

        public int RoundWaterGainPenalty    => _roundWaterGainPenalty;
        public int RoundWaterGainBonus      => _roundWaterGainBonus;
        public int RoundMoneyGainPenalty    => _roundMoneyGainPenalty;
        public int RoundMoneyGainBonus      => _roundMoneyGainBonus;
        public int RoundProjectMoneyPenalty => _roundProjectMoneyPenalty;
        public int RoundProjectMoneyBonus   => _roundProjectMoneyBonus;

        public int SelfMoveToTile     => _selfMoveToTile;
        public TeleportMode TeleportMode => _teleportMode;
        public Networking.Services.SliceTileType TeleportTargetTileType => _teleportTargetTileType;

        public bool  UseBasinThresholdDelta    => _useBasinThresholdDelta;
        public bool  ApplyBasinThresholdOnScan => _applyBasinThresholdOnScan;
        public float BasinThresholdPercentage   => _basinThresholdPercentage;
        public int   BasinDeltaAboveThreshold  => _basinDeltaAboveThreshold;
        public int   BasinDeltaBelowThreshold  => _basinDeltaBelowThreshold;

        public bool  UseBasinThresholdMoneyDelta => _useBasinThresholdMoneyDelta;
        public int   MoneyDeltaAboveThreshold    => _moneyDeltaAboveThreshold;
        public int   MoneyDeltaBelowThreshold    => _moneyDeltaBelowThreshold;

        public bool  UseBasinThresholdDoubleDeltas => _useBasinThresholdDoubleDeltas;
        public bool  InvertsTurnOrder              => _invertsTurnOrder;

        public bool RequiresDecision         => _requiresDecision;
        public CardDecisionScope DecisionScope => _decisionScope;
        public CardDecisionChoice DecisionChoiceA => _decisionChoiceA;
        public CardDecisionChoice DecisionChoiceB => _decisionChoiceB;

        public bool IsDroughtEvent                       => _isDroughtEvent;
        public bool IsClimateEvent                       => _isClimateEvent;
        public bool IsDeforestationEvent                 => _isDeforestationEvent;
        public int  DeforestationProjectMoneyPercentPenalty => _deforestationProjectMoneyPercentPenalty;

        public bool ConditionalMoneyOnProjects  => _conditionalMoneyOnProjects;
        public int  MoneyWithActiveProject      => _moneyWithActiveProject;
        public int  MoneyWithoutActiveProject   => _moneyWithoutActiveProject;

        public bool       IsWeatherCard                          => _isWeatherCard;
        public WeatherTag WeatherTag                            => _weatherTag;
        public int        WeatherDurationRounds                  => _weatherDurationRounds;
        public int        WeatherHydricWaterFlatBonus            => _weatherHydricWaterFlatBonus;
        public int        WeatherAllPlayersWaterPerTurnDelta     => _weatherAllPlayersWaterPerTurnDelta;
        public int        WeatherAllPlayersWaterOnTileResolveDelta => _weatherAllPlayersWaterOnTileResolveDelta;
        public int        WeatherDiceRollFlatBonus               => _weatherDiceRollFlatBonus;
        public bool       WeatherRollDependentRewards            => _weatherRollDependentRewards;
        public int        WeatherProjectMoneyPercentPenalty      => _weatherProjectMoneyPercentPenalty;
        public bool       WeatherApplyBasinDeltaAtRoundEnd       => _weatherApplyBasinDeltaAtRoundEnd;
        public int        WeatherBasinFlatPerRound               => _weatherBasinFlatPerRound;
        public int        WeatherProjectMoneyFlatBonusPerRound   => _weatherProjectMoneyFlatBonusPerRound;
        public bool       WeatherLockBasin                       => _weatherLockBasin;
        public bool       WeatherNullifyHydricWater              => _weatherNullifyHydricWater;
        public int        WeatherProjectWaterFlatBonusPerRound   => _weatherProjectWaterFlatBonusPerRound;
        public bool       WeatherDoubleBasinRecovery             => _weatherDoubleBasinRecovery;

        public WeatherTag ConditionalWaterIfWeatherTag  => _conditionalWaterIfWeatherTag;
        public int        ConditionalWaterDelta         => _conditionalWaterDelta;

        public bool       TerminatesActiveWeather        => _terminatesActiveWeather;
        public WeatherTag TerminatesWeatherTag             => _terminatesWeatherTag;
        public int        BasinFlatOnWeatherTerminate      => _basinFlatOnWeatherTerminate;
    }
}
