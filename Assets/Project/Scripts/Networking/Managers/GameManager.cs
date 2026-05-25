using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;
using System.Linq;
using Networking.Models;

namespace Networking.Managers
{
    /// <summary>
    /// Exact replication of OtherGame GameManager.
    /// Manages game state and player data tracking.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnRunnerShutDownEvent;

        private Dictionary<PlayerRef, Networking.Models.PlayerSessionData> _playerData = new Dictionary<PlayerRef, Networking.Models.PlayerSessionData>();
        private readonly List<PlayerRef> _turnOrder = new List<PlayerRef>();
        private int _activeTurnIndex = -1;
        private bool _roundInProgress;
        private int _currentRound;
        private bool _isTurnOrderLocked;
        private bool _invertTurnOrderNextRound;
        private int _roundWaterGainFlatPenalty;
        private int _roundWaterGainPercentPenalty;
        private int _roundWaterGainFlatBonus;
        private int _roundWaterGainPercentBonus;
        private int _roundMoneyGainFlatPenalty;
        private int _roundMoneyGainPercentPenalty;
        private int _roundMoneyGainFlatBonus;
        private int _roundMoneyGainPercentBonus;
        private int _roundProjectMoneyFlatPenalty;
        private int _roundProjectMoneyPercentPenalty;
        private int _roundProjectMoneyFlatBonus;
        private int _roundProjectMoneyPercentBonus;
        private bool _droughtEventActive;
        private bool _climateEventActive;
        private bool _deforestationEventActive;
        private int  _deforestationProjectMoneyPercentPenalty;
        private int _pendingDecisionCardId = -1;
        private PlayerRef _pendingDecisionScanningPlayer;
        private Networking.Models.CardDecisionScope _pendingDecisionScope;

        // ── Active weather state (server-side only, reset each match) ──────────
        private int _activeWeatherCardId = -1;
        private int _weatherStartRound;
        private int _weatherDurationRounds;
        private int _weatherHydricWaterFlatBonus;
        private int _weatherAllPlayersWaterPerTurnDelta;
        private int _weatherAllPlayersWaterOnTileResolveDelta;
        private int _weatherDiceRollFlatBonus;
        private bool _weatherRollDependentRewards;
        private int _weatherProjectMoneyPercentPenalty;
        private bool _weatherApplyBasinDeltaAtRoundEnd;
        private int _weatherBasinFlatPerRound;
        private WeatherTag _activeWeatherTag = WeatherTag.None;
        private int _activeWeatherVersion = 0;

        public WeatherTag ActiveWeatherTag
        {
            get
            {
                var runner = Networking.Services.FusionNetworkService.LocalRunner;
                if (runner != null && !runner.IsServer)
                {
                    var localData = GetPlayerData(runner.LocalPlayer, runner);
                    if (localData != null) return localData.ActiveWeatherTag;
                }
                return _activeWeatherTag;
            }
        }

        public int ActiveWeatherVersion
        {
            get
            {
                var runner = Networking.Services.FusionNetworkService.LocalRunner;
                if (runner != null && !runner.IsServer)
                {
                    var localData = GetPlayerData(runner.LocalPlayer, runner);
                    if (localData != null) return localData.WeatherVersion;
                }
                return _activeWeatherVersion;
            }
        }
private int  _weatherProjectMoneyFlatBonusPerRound;
        private int  _weatherProjectWaterFlatBonusPerRound;
        private bool _weatherDoubleBasinRecovery;
        private bool _weatherLockBasin;
        private bool _weatherNullifyHydricWater;

        public int CurrentRound
        {
            get
            {
                var runner = Networking.Services.FusionNetworkService.LocalRunner;
                if (runner != null && !runner.IsServer)
                {
                    var localData = GetPlayerData(runner.LocalPlayer, runner);
                    if (localData != null) return localData.CurrentRound;
                }
                return _currentRound;
            }
        }

        public int MaxRoundsToWin => _maxRoundsToWin;
        public int StartingWater => _startingWater;
        public int StartingMoney => _startingMoney;
        public int StartingBasinHealth => _startingBasinHealth;
        public int InitialBoardPosition => 0;
        public Networking.Models.ProjectDatabase ProjectDatabase => _projectDatabase;
        public Networking.Models.CardDatabase CardDatabase => _cardDatabase;

        private const int MaxOwnedProjects = 3;

        [Header("Round Slice Config")]
        [SerializeField] private int _boardTileCount = 24;
        [SerializeField] private int _startingWater = 10;
        [SerializeField] private int _startingMoney = 0;
        [SerializeField] private int _startingBasinHealth = 100;
        [SerializeField] private int _hydricWaterGain = 2;
        [SerializeField] private int _hydricMoneyGain = 1;
        [SerializeField] private int _hydricBasinBonus = 1;
        [SerializeField] private int _catastrophicWaterPenalty = 2;
        [SerializeField] private int _catastrophicMoneyPenalty = 1;
        [SerializeField] private int _catastrophicBasinPenalty = 5;
        [SerializeField] private int _maxRoundsToWin = 3;
        [SerializeField] private float _nextRoundDelaySeconds = 1.25f;
        [SerializeField] private Networking.Models.BoardTileConfig _boardTileConfig;
        [SerializeField] private Networking.Models.ProjectDatabase _projectDatabase;
        [SerializeField] private Networking.Models.CardDatabase _cardDatabase;
        [SerializeField] private Networking.Models.TriviaDatabase _triviaDatabase;
        [SerializeField] private int _triviaWaterRewardMin = 2;
        [SerializeField] private int _triviaWaterRewardMax = 5;
        [SerializeField] private int _triviaMoneyRewardMin = 1;
        [SerializeField] private int _triviaMoneyRewardMax = 3;

        [Header("Animation & Timing Config")]
        [SerializeField] private float _turnAdvancementDelaySeconds = 4.5f;
        [SerializeField] private float _diceJumpDurationPerUnit = 0.45f;
        [SerializeField] private float _diceJumpBufferSeconds = 4.5f;

        private Networking.Services.BasinService _basinService;
        private Networking.Services.TileService _tileService;

        public enum GameState
        {
            Lobby,
            Setup,
            CharacterSelection,
            RollOrder,
            PlayerTurn,
            TileResolve,
            Decision,
            BasinCheck,
            Loading,
            Minigame,
            PassiveEffects,
            Victory,
            Defeat,
            TurnOrderInitialization = RollOrder,
            Playing = PlayerTurn
        }

        public int ActiveWeatherCardId => _activeWeatherCardId;

        public string ActiveWeatherCardName
        {
            get
            {
                if (_activeWeatherCardId < 0 || _cardDatabase == null) return string.Empty;
                if (_cardDatabase.TryGetCard(_activeWeatherCardId, out var card))
                {
                    return card.DisplayName;
                }
                return string.Empty;
            }
        }

        public GameState State { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Application.targetFrameRate = 60;
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Debug.Log($"[GameManager] Target frame rate set to 60. VSync count set to 0. Run in background enabled.");
            }
            else if (Instance != this)
            {
                Destroy(this.transform.parent.gameObject);
            }
            DontDestroyOnLoad(transform.parent);

            _basinService = new Networking.Services.BasinService();
            _tileService = new Networking.Services.TileService(_boardTileConfig);
        }

        private void OnEnable()
        {
            OnPlayerLeftEvent.RegisterResponse(PlayerDisconnected);
            OnRunnerShutDownEvent.RegisterResponse(DisconnectedFromSession);

            var definitions = Networking.Events.NetworkEventDefinitions.Instance;
            if (definitions != null && definitions.OnDiceRolledEvent != null)
            {
                definitions.OnDiceRolledEvent.RegisterResponse(OnDiceRolled);
            }
        }

        private void OnDisable()
        {
            OnPlayerLeftEvent.RemoveResponse(PlayerDisconnected);
            OnRunnerShutDownEvent.RemoveResponse(DisconnectedFromSession);

            var definitions = Networking.Events.NetworkEventDefinitions.Instance;
            if (definitions != null && definitions.OnDiceRolledEvent != null)
            {
                definitions.OnDiceRolledEvent.RemoveResponse(OnDiceRolled);
            }
        }

        public void SetGameState(GameState state)
        {
            State = state;

            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (state == GameState.PlayerTurn && runner != null && runner.IsServer && !_roundInProgress)
            {
                StartRound(runner);
            }
        }

        private void OnDiceRolled(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            if (State == GameState.RollOrder)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || playerData.LastDiceRoll <= 0)
            {
                return;
            }

            HandleValidatedTurnRoll(player, playerData.LastDiceRoll, runner);
        }

        public void StartRound(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            if (_roundInProgress)
            {
                return;
            }

            _currentRound++;
            _roundInProgress = true;
            ApplyProjectRoundStartEffects(runner);
            _activeTurnIndex = -1;
_roundWaterGainFlatPenalty    = 0; _roundWaterGainPercentPenalty  = 0;
            _roundWaterGainFlatBonus      = 0; _roundWaterGainPercentBonus    = 0;
            _roundMoneyGainFlatPenalty    = 0; _roundMoneyGainPercentPenalty  = 0;
            _roundMoneyGainFlatBonus      = 0; _roundMoneyGainPercentBonus    = 0;
            _roundProjectMoneyFlatPenalty = 0; _roundProjectMoneyPercentPenalty = 0;
            _roundProjectMoneyFlatBonus   = 0; _roundProjectMoneyPercentBonus = 0;
            _droughtEventActive = false;
            _climateEventActive = false;
            _deforestationEventActive = false;
            _deforestationProjectMoneyPercentPenalty = 0;
            _pendingDecisionCardId = -1;

            // ── Weather: expire check or re-apply for this round ─────────────────
            if (_activeWeatherCardId >= 0)
            {
                if (_currentRound > _weatherStartRound + _weatherDurationRounds)
                {
                    Debug.Log($"[GameManager] Weather (card {_activeWeatherCardId}) expired at round {_currentRound}.");
                    ClearActiveWeather();
                    SyncWeatherToAllPlayers(runner);
                }
else
                {
                    _roundProjectMoneyPercentPenalty += _weatherProjectMoneyPercentPenalty;
                    _roundProjectMoneyFlatBonus      += _weatherProjectMoneyFlatBonusPerRound;

                    if (!_weatherApplyBasinDeltaAtRoundEnd && _weatherBasinFlatPerRound != 0 && !_weatherLockBasin)
                    {
                        _basinService.ApplyDelta(_weatherBasinFlatPerRound);
                        SyncBasinHealthToAllPlayers(runner);
                        if (_basinService.IsDefeated)
                        {
                            SetGameState(GameState.Defeat);
                            _roundInProgress = false;
                            foreach (var p in runner.ActivePlayers)
                            {
                                var d = GetPlayerData(p, runner);
                                if (d != null) { d.IsGameOver = true; d.IsDefeat = true; }
                            }
                            return;
                        }
                    }

                }
            }

            // Sync round number to all clients via [Networked] property
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null) data.CurrentRound = _currentRound;
            }

            if (!_isTurnOrderLocked)
            {
                _turnOrder.Clear();
            }

            if (_currentRound == 1)
            {
                _basinService.Initialize(_startingBasinHealth);
            }

            if (_invertTurnOrderNextRound)
            {
                _turnOrder.Reverse();
                for (int i = 0; i < _turnOrder.Count; i++)
                {
                    var data = GetPlayerData(_turnOrder[i], runner);
                    if (data != null) data.TurnOrder = i;
                }
                _invertTurnOrderNextRound = false;
                Debug.Log("[GameManager] Turn order inverted for the new round.");
            }

            State = GameState.PlayerTurn;
            if (!_isTurnOrderLocked)
            {
                DetermineTurnOrder(runner);
                _isTurnOrderLocked = true;
            }
            InitializeRoundPlayerState(runner);
            SyncBasinHealthToAllPlayers(runner);

            Networking.Events.NetworkEventDefinitions.Instance?.OnRoundStartedEvent?.Raise(default, runner);
            AdvanceTurn(runner);
        }

        public PlayerRef GetActivePlayer(NetworkRunner runner)
        {
            if (runner == null)
            {
                return default;
            }

            if (runner.IsServer && _activeTurnIndex >= 0 && _activeTurnIndex < _turnOrder.Count)
            {
                return _turnOrder[_activeTurnIndex];
            }

            // Client-side fallback: infer active player from synchronized flag.
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null && data.IsActiveTurn)
                {
                    return player;
                }
            }

            return default;
        }

        public Networking.Services.SliceTileType GetTileTypeAtPosition(int boardPosition)
        {
            if (_tileService == null)
            {
                return Networking.Services.SliceTileType.Start;
            }

            return _tileService.GetTileType(boardPosition);
        }

        public Networking.Models.ColombiaZone GetZoneAtPosition(int boardPosition)
        {
            if (_tileService == null)
            {
                return Networking.Models.ColombiaZone.Andean;
            }

            return _tileService.GetTileZone(boardPosition);
        }

        public Color GetTileColorAtPosition(int boardPosition)
        {
            if (_tileService == null)
            {
                return Color.white;
            }

            return _tileService.GetTileColor(boardPosition);
        }

        public void HandleValidatedTurnRoll(PlayerRef player, int diceRoll, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer || !_roundInProgress)
            {
                return;
            }

            var activePlayer = GetActivePlayer(runner);
            if (activePlayer != player)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null)
            {
                return;
            }

            int effectiveRoll = Mathf.Max(1, diceRoll + playerData.PendingDiceModifier + _weatherDiceRollFlatBonus);
            playerData.PendingDiceModifier = 0;
            playerData.LastDiceRoll = effectiveRoll;
            if (effectiveRoll != diceRoll)
                Debug.Log($"[GameManager] Player {player.PlayerId} dice roll modified: {diceRoll} → {effectiveRoll}.");

            int oldPosition = playerData.BoardPosition;
            int nextPosition = oldPosition + effectiveRoll;
            bool passedStart = nextPosition >= _boardTileCount;

            playerData.BoardPosition = nextPosition % Mathf.Max(1, _boardTileCount);

            if (passedStart)
            {
                Debug.Log($"[GameManager] Player {player.PlayerId} completed a lap! Awarding 3 money.");
                ApplyMoneyDelta(playerData, player, runner, 3, respectShield: false);
            }

            Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerMovedEvent?.Raise(player, runner);

            State = GameState.TileResolve;
            bool shouldAdvanceTurn = ResolveTileAndApplyEffects(playerData, runner);
            if (shouldAdvanceTurn)
            {
                float delay = (effectiveRoll * _diceJumpDurationPerUnit) + _diceJumpBufferSeconds;
                AdvanceTurn(runner, delay);
            }
        }

        public void HandleTeleportLanding(Networking.Models.PlayerSessionData playerData, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer) return;

            State = GameState.TileResolve;
            bool shouldAdvanceTurn = ResolveTileAndApplyEffects(playerData, runner, fromTeleport: true);
            if (shouldAdvanceTurn)
            {
                AdvanceTurn(runner, _turnAdvancementDelaySeconds);
            }
        }

        private void DetermineTurnOrder(NetworkRunner runner)
        {
            var rollPairs = new List<(PlayerRef player, int roll)>();
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null && data.LastDiceRoll > 0)
                {
                    rollPairs.Add((player, data.LastDiceRoll));
                }
                else
                {
                    int fallbackRoll = UnityEngine.Random.Range(1, 11);
                    if (data != null)
                    {
                        data.LastDiceRoll = fallbackRoll;
                        data.LastDiceRollTime = (float)runner.SimulationTime;
                    }

                    rollPairs.Add((player, fallbackRoll));
                }
            }

            _turnOrder.AddRange(rollPairs
                .OrderByDescending(x => x.roll)
                .ThenBy(x => x.player.PlayerId)
                .Select(x => x.player));

            for (int i = 0; i < _turnOrder.Count; i++)
            {
                var data = GetPlayerData(_turnOrder[i], runner);
                if (data != null)
                {
                    data.TurnOrder = i;
                }
            }
        }

        private void InitializeRoundPlayerState(NetworkRunner runner)
        {
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null || !runner.IsServer)
                {
                    continue;
                }

                data.LastDiceRoll = 0;
                data.LastDiceRollTime = 0f;
                data.IsActiveTurn = false;
                data.HasRolledThisTurn = false;
                data.HasScannedARThisTurn = false;
                data.IsInMinigameReadyPhase = false;
                data.IsReadyForMinigame = false;
                data.IsAwaitingDecisionVote = false;
                data.ActiveWeatherTag = _activeWeatherTag;
                data.PendingDecisionVote = 0;
data.IsPendingTeleportTileResolution = false;
                ClearPendingProjectState(data);

                if (data.WaterAmount <= 0)
                {
                    data.WaterAmount = _startingWater;
                }

                if (data.MoneyAmount <= 0)
                {
                    data.MoneyAmount = _startingMoney;
                }
            }
        }
        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"[GameManager] Application pause status: {pauseStatus}. Peer mode: {Networking.Services.FusionNetworkService.LocalRunner?.Config?.PeerMode}");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"[GameManager] Application focus status: {hasFocus}");
        }

        public void InitializePreRoundPlayerState(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            if (_currentRound <= 0)
            {
                _basinService.Initialize(_startingBasinHealth);
                SyncBasinHealthToAllPlayers(runner);
            }

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null)
                {
                    continue;
                }

                data.BoardPosition = InitialBoardPosition;
                ClearPendingProjectState(data);

                if (data.WaterAmount <= 0)
                {
                    data.WaterAmount = _startingWater;
                }

                if (data.MoneyAmount <= 0)
                {
                    data.MoneyAmount = _startingMoney;
                }
            }
        }

        private void AdvanceTurn(NetworkRunner runner, float delay = 0f)
        {
            // Clear current active player status immediately so no one is "Active" during the delay
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null && data.IsActiveTurn)
                {
                    data.IsActiveTurn = false;
                    data.HasRolledThisTurn = false;
                    data.HasScannedARThisTurn = false;
                    ClearPendingProjectState(data);
                }
            }

            if (delay > 0f)
            {
                StartCoroutine(AdvanceTurnCoroutine(runner, delay));
            }
            else
            {
                ExecuteAdvanceTurn(runner);
            }
        }

        private IEnumerator AdvanceTurnCoroutine(NetworkRunner runner, float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteAdvanceTurn(runner);
        }

        private void ExecuteAdvanceTurn(NetworkRunner runner)
        {
            _activeTurnIndex++;
            if (_activeTurnIndex >= _turnOrder.Count)
            {
                // Delay the transition to minigame to allow the last player's animations to finish
                StartCoroutine(EndRoundDelayedCoroutine(runner, _turnAdvancementDelaySeconds));
                return;
            }

            var activePlayer = _turnOrder[_activeTurnIndex];
            var activeData = GetPlayerData(activePlayer, runner);
            if (activeData != null)
            {
                activeData.IsActiveTurn = true;
                // Weather: per-turn water delta (e.g. "all players lose 1 water at turn start")
                if (_activeWeatherCardId >= 0 && _weatherAllPlayersWaterPerTurnDelta != 0)
                    ApplyWaterDelta(activeData, activePlayer, runner, _weatherAllPlayersWaterPerTurnDelta, respectShield: false);

                // Weather: roll-dependent rewards at turn start
                if (_activeWeatherCardId >= 0 && _weatherRollDependentRewards)
                {
                    int dieRoll = Random.Range(1, 7);
                    int waterDelta = dieRoll >= 4 ? 4 : -3;
                    int moneyDelta = dieRoll >= 4 ? 3 : -3;

                    activeData.RPC_SyncWeatherRollResult(dieRoll, waterDelta, moneyDelta);

                    ApplyWaterDelta(activeData, activePlayer, runner, waterDelta, respectShield: false);
                    ApplyMoneyDelta(activeData, activePlayer, runner, moneyDelta, respectShield: false);
                    Debug.Log($"[GameManager] Player {activePlayer.PlayerId} rolled {dieRoll}: {waterDelta} water, {moneyDelta} money.");
                }
            }

            State = GameState.PlayerTurn;
            Networking.Events.NetworkEventDefinitions.Instance?.OnTurnStartedEvent?.Raise(activePlayer, runner);
        }

        private IEnumerator EndRoundDelayedCoroutine(NetworkRunner runner, float delay)
        {
            yield return new WaitForSeconds(delay);
            EndRound(runner);
        }

        private bool ResolveTileAndApplyEffects(Networking.Models.PlayerSessionData playerData, NetworkRunner runner, bool fromTeleport = false)
        {
            State = GameState.BasinCheck;

            var tileType = _tileService.GetTileType(playerData.BoardPosition);

            if (tileType == Networking.Services.SliceTileType.Start)
            {
                // Start tile has no effect
                return true;
            }

            if (tileType == Networking.Services.SliceTileType.Project)
            {
                return BeginProjectTileFlow(playerData);
            }

            if (tileType == Networking.Services.SliceTileType.DrawCard)
            {
                // Prevent a teleport card that lands on a DrawCard tile from also triggering
                // another nested teleport via a second card scan.
                if (fromTeleport)
                {
                    Debug.Log($"[GameManager] Player {playerData.Object.InputAuthority.PlayerId} teleported to a DrawCard tile — normal card scan flow begins.");
                }
                return BeginDrawCardTileFlow(playerData);
            }

            if (tileType == Networking.Services.SliceTileType.Trivia)
            {
                return BeginTriviaTileFlow(playerData);
            }

            int waterDelta;
            int moneyDelta;
            int basinDelta;

            if (tileType == Networking.Services.SliceTileType.Hydric)
            {
                if (_weatherNullifyHydricWater)
                {
                    waterDelta = 0;
                }
                else
                {
                    waterDelta = _tileService.ResolveHydricWaterDelta(_hydricWaterGain);
                    // Weather: signed hydric bonus (positive increases, negative decreases), minimum 1
                    if (_activeWeatherCardId >= 0 && _weatherHydricWaterFlatBonus != 0)
                        waterDelta = Mathf.Max(1, waterDelta + _weatherHydricWaterFlatBonus);
                }
                moneyDelta = _hydricMoneyGain;
                basinDelta = _tileService.ResolveHydricBasinDelta(_hydricBasinBonus);
            }
            else
            {
                waterDelta = _tileService.ResolveCatastrophicWaterDelta(_catastrophicWaterPenalty);
                moneyDelta = -_catastrophicMoneyPenalty;
                basinDelta = _tileService.ResolveCatastrophicBasinDelta(_catastrophicBasinPenalty);
            }

            ApplyWaterDelta(playerData, playerData.Object.InputAuthority, runner, waterDelta, respectShield: true, 
                isHydric: tileType == Networking.Services.SliceTileType.Hydric, 
                isCatastrophic: tileType == Networking.Services.SliceTileType.Catastrophic);
            ApplyMoneyDelta(playerData, playerData.Object.InputAuthority, runner, moneyDelta, respectShield: true);

            if (_activeWeatherCardId >= 0 && _weatherAllPlayersWaterOnTileResolveDelta != 0)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    var d = GetPlayerData(p, runner);
                    if (d == null) continue;
                    ApplyWaterDelta(d, p, runner, _weatherAllPlayersWaterOnTileResolveDelta, respectShield: false);
                }
            }

            if (!_weatherLockBasin)
            {
                _basinService.ApplyDelta(basinDelta);
                SyncBasinHealthToAllPlayers(runner);

                if (_basinService.IsDefeated)
                {
                    SetGameState(GameState.Defeat);
                    _roundInProgress = false;

                    foreach (var p in runner.ActivePlayers)
                    {
                        var d = GetPlayerData(p, runner);
                        if (d != null)
                        {
                            d.IsGameOver = true;
                            d.IsDefeat = true;
                        }
                    }

                    Debug.Log("[GameManager] Basin defeated! All players lose.");
                }
            }

            return true;
        }

        private bool BeginProjectTileFlow(Networking.Models.PlayerSessionData playerData)
        {
            ClearPendingProjectState(playerData);

            if (_projectDatabase == null)
            {
                Debug.LogWarning("[GameManager] Project tile ignored because no ProjectDatabase is assigned.");
                return true;
            }

            if (CountOwnedProjects(playerData) >= MaxOwnedProjects)
            {
                Debug.Log($"[GameManager] Player {playerData.Object.InputAuthority.PlayerId} already has the maximum number of projects.");
                return true;
            }

            playerData.IsAwaitingProjectScan = true;
            State = GameState.Decision;
            Debug.Log($"[GameManager] Player {playerData.Object.InputAuthority.PlayerId} landed on a Project tile. Waiting for scan.");
            return false;
        }

        private bool BeginTriviaTileFlow(Networking.Models.PlayerSessionData playerData)
        {
            ClearPendingProjectState(playerData);
            playerData.IsAwaitingTrivia = true;
            State = GameState.Decision;
            Debug.Log($"[GameManager] Player {playerData.Object.InputAuthority.PlayerId} landed on Trivia tile. Waiting for answer.");
            return false;
        }

        public void HandleTriviaAnswer(PlayerRef player, NetworkRunner runner, bool correct)
        {
            if (runner == null || !runner.IsServer) return;

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsAwaitingTrivia) return;

            playerData.IsAwaitingTrivia = false;

            if (correct)
            {
                int multiplier = playerData.DoubleTriviaReward ? 2 : 1;
                playerData.DoubleTriviaReward = false; // Reset after use

                bool giveWater = UnityEngine.Random.value < 0.5f;
                if (giveWater)
                {
                    int water = UnityEngine.Random.Range(_triviaWaterRewardMin, _triviaWaterRewardMax + 1) * multiplier;
                    ApplyWaterDelta(playerData, player, runner, water, respectShield: false);
                    Debug.Log($"[GameManager] Trivia correct — awarded {water} water to player {player.PlayerId} (mult={multiplier}).");
                }
                else
                {
                    int money = UnityEngine.Random.Range(_triviaMoneyRewardMin, _triviaMoneyRewardMax + 1) * multiplier;
                    ApplyMoneyDelta(playerData, player, runner, money, respectShield: false);
                    Debug.Log($"[GameManager] Trivia correct — awarded {money} money to player {player.PlayerId} (mult={multiplier}).");
                }
            }
            else
            {
                Debug.Log($"[GameManager] Trivia incorrect — no reward for player {player.PlayerId}.");
            }

            AdvanceTurn(runner, _turnAdvancementDelaySeconds);
        }

        private bool BeginDrawCardTileFlow(Networking.Models.PlayerSessionData playerData)
        {
            ClearPendingProjectState(playerData);
            ClearPendingCardDisplay(playerData);

            if (_cardDatabase == null)
            {
                Debug.LogWarning("[GameManager] DrawCard tile ignored because no CardDatabase is assigned.");
                return true;
            }

            playerData.IsAwaitingCardScan = true;
            State = GameState.Decision;
            Debug.Log($"[GameManager] Player {playerData.Object.InputAuthority.PlayerId} landed on a DrawCard tile. Waiting for scan.");
            return false;
        }

        public void HandleCardScan(PlayerRef player, NetworkRunner runner, int cardId)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsActiveTurn || !playerData.IsAwaitingCardScan)
            {
                return;
            }

            if (_cardDatabase == null || !_cardDatabase.TryGetCard(cardId, out var card) || card == null)
            {
                Debug.LogWarning($"[GameManager] Card scan rejected: unknown card ID {cardId}.");
                return;
            }

            // Keep a player-facing copy of this scan for Vuforia overlay UI.
            playerData.PendingCardTitle = card.DisplayName;
            playerData.PendingCardLore = card.LoreText;
            playerData.PendingCardEffect = card.EffectDescription;

            Debug.Log($"[GameManager] Card scanned: '{card.DisplayName}' (id={card.CardId}), " +
                      $"water={card.WaterDelta}, money={card.MoneyDelta}, basin={card.BasinDelta}.");

            // ── 0. Threshold Doubling ────────────────────────────────────────────
            int thresholdMultiplier = 1;
            if (card.UseBasinThresholdDoubleDeltas)
            {
                int currentBasin = _basinService.BasinHealth;
                int threshold = Mathf.RoundToInt(_startingBasinHealth * card.BasinThresholdPercentage);
                if (currentBasin <= threshold)
                {
                    thresholdMultiplier = 2;
                }
            }

            // ── 1. Self water / money ────────────────────────────────────────────
            // If the card has a weather-conditional override and the active weather matches, use it instead.
            int cardWaterDelta = (card.ConditionalWaterIfWeatherTag != WeatherTag.None &&
                                  _activeWeatherTag == card.ConditionalWaterIfWeatherTag)
                ? card.ConditionalWaterDelta
                : card.WaterDelta;

            cardWaterDelta *= thresholdMultiplier;
            if (cardWaterDelta != 0)
                ApplyWaterDelta(playerData, player, runner, cardWaterDelta, respectShield: true);

            int cardMoneyDelta = card.MoneyDelta * thresholdMultiplier;
            if (cardMoneyDelta != 0)
                ApplyMoneyDelta(playerData, player, runner, cardMoneyDelta, respectShield: true);

            // ── 1b. Conditional money based on owned project count ───────────────
            if (card.ConditionalMoneyOnProjects)
            {
                int projectCount = CountOwnedProjects(playerData);
                int cd = (projectCount >= 1) ? card.MoneyWithActiveProject : card.MoneyWithoutActiveProject;
                if (cd != 0) ApplyMoneyDelta(playerData, player, runner, cd, respectShield: true);
            }

            // ── 1c. Conditional money based on Basin Threshold ───────────────────
            if (card.UseBasinThresholdMoneyDelta)
            {
                int currentBasin = _basinService.BasinHealth;
                int threshold = Mathf.RoundToInt(_startingBasinHealth * card.BasinThresholdPercentage);
                int md = (currentBasin > threshold) ? card.MoneyDeltaAboveThreshold : card.MoneyDeltaBelowThreshold;
                if (md != 0) ApplyMoneyDelta(playerData, player, runner, md, respectShield: true);
            }

            // ── 1d. Weather card — overwrites any active weather ─────────────────
            if (card.IsWeatherCard)
            {
                ClearActiveWeather();
                _activeWeatherCardId               = card.CardId;
                _weatherStartRound                 = _currentRound;
                _weatherDurationRounds             = card.WeatherDurationRounds;
                _weatherHydricWaterFlatBonus        = card.WeatherHydricWaterFlatBonus;
                _weatherAllPlayersWaterPerTurnDelta = card.WeatherAllPlayersWaterPerTurnDelta;
                _weatherAllPlayersWaterOnTileResolveDelta = card.WeatherAllPlayersWaterOnTileResolveDelta;
                _weatherDiceRollFlatBonus           = card.WeatherDiceRollFlatBonus;
                _weatherRollDependentRewards        = card.WeatherRollDependentRewards;
                _weatherProjectMoneyPercentPenalty  = card.WeatherProjectMoneyPercentPenalty;
                _weatherApplyBasinDeltaAtRoundEnd   = card.WeatherApplyBasinDeltaAtRoundEnd;
                _weatherBasinFlatPerRound             = card.WeatherBasinFlatPerRound;
                _weatherProjectMoneyFlatBonusPerRound  = card.WeatherProjectMoneyFlatBonusPerRound;
                _weatherProjectWaterFlatBonusPerRound   = card.WeatherProjectWaterFlatBonusPerRound;
                _weatherDoubleBasinRecovery             = card.WeatherDoubleBasinRecovery;
                _activeWeatherTag                      = card.WeatherTag;
                _weatherLockBasin                      = card.WeatherLockBasin;
                _weatherNullifyHydricWater              = card.WeatherNullifyHydricWater;
                SyncWeatherToAllPlayers(runner);
                // Apply project money penalty/bonus immediately for the current (in-progress) round
_roundProjectMoneyPercentPenalty += _weatherProjectMoneyPercentPenalty;
                _roundProjectMoneyFlatBonus      += _weatherProjectMoneyFlatBonusPerRound;
                Debug.Log($"[GameManager] Weather card '{card.DisplayName}' activated. " +
                          $"Duration: {_weatherDurationRounds} extra rounds from round {_currentRound}.");
            }

            // ── 1e. Weather termination ──────────────────────────────────────────
            if (card.TerminatesActiveWeather && _activeWeatherCardId >= 0
                && (card.TerminatesWeatherTag == WeatherTag.None || _activeWeatherTag == card.TerminatesWeatherTag))
            {
                int basinFlat = card.BasinFlatOnWeatherTerminate;
                ClearActiveWeather();
                if (basinFlat != 0)
                {
                    _basinService.ApplyDelta(basinFlat);
                    SyncBasinHealthToAllPlayers(runner);
                }
                SyncWeatherToAllPlayers(runner);
                Debug.Log($"[GameManager] '{card.DisplayName}' terminated active weather. Basin flat delta: {basinFlat}.");
            }

            // ── 2. Basin delta ───────────────────────────────────────────────────
            if (card.BasinDelta != 0 && !_weatherLockBasin)
            {
                int calculatedBasinDelta = card.BasinDelta * thresholdMultiplier;
                int basinDelta = (_weatherDoubleBasinRecovery && calculatedBasinDelta > 0) ? calculatedBasinDelta * 2 : calculatedBasinDelta;
                _basinService.ApplyDelta(basinDelta);
                SyncBasinHealthToAllPlayers(runner);

                if (_basinService.IsDefeated)
                {
                    SetGameState(GameState.Defeat);
                    _roundInProgress = false;
                    foreach (var p in runner.ActivePlayers)
                    {
                        var d = GetPlayerData(p, runner);
                        if (d != null) { d.IsGameOver = true; d.IsDefeat = true; }
                    }
                    playerData.IsAwaitingCardScan = false;
                    return;
                }
            }

            // ── 2b. Basin threshold delta ────────────────────────────────────────
            if (card.UseBasinThresholdDelta && card.ApplyBasinThresholdOnScan && !_weatherLockBasin)
            {
                int currentBasin = _basinService.BasinHealth;
                int threshold = Mathf.RoundToInt(_startingBasinHealth * card.BasinThresholdPercentage);

                int thresholdDelta = (currentBasin > threshold)
                    ? card.BasinDeltaAboveThreshold
                    : card.BasinDeltaBelowThreshold;

                if (thresholdDelta != 0)
                {
                    int finalDelta = (_weatherDoubleBasinRecovery && thresholdDelta > 0) ? thresholdDelta * 2 : thresholdDelta;
                    _basinService.ApplyDelta(finalDelta);
                    SyncBasinHealthToAllPlayers(runner);

                    if (_basinService.IsDefeated)
                    {
                        SetGameState(GameState.Defeat);
                        _roundInProgress = false;
                        foreach (var p in runner.ActivePlayers)
                        {
                            var d = GetPlayerData(p, runner);
                            if (d != null) { d.IsGameOver = true; d.IsDefeat = true; }
                        }
                        playerData.IsAwaitingCardScan = false;
                        return;
                    }
                }
            }

            // ── 3. All-players water / money ─────────────────────────────────────
            if (card.AllPlayersWaterDelta != 0)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    var d = GetPlayerData(p, runner);
                    if (d == null) continue;
                    ApplyWaterDelta(d, p, runner, card.AllPlayersWaterDelta, respectShield: true);
                }
            }

            if (card.AllPlayersMoneyDelta != 0)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    var d = GetPlayerData(p, runner);
                    if (d == null) continue;
                    ApplyMoneyDelta(d, p, runner, card.AllPlayersMoneyDelta, respectShield: true);
                }
            }

            // ── 4. Round water-gain penalty ──────────────────────────────────────
            AccumulateRoundModifier(card.RoundWaterGainPenalty,    false, ref _roundWaterGainFlatPenalty,    ref _roundWaterGainPercentPenalty,    capPercent: true);
            AccumulateRoundModifier(card.RoundWaterGainBonus,      false, ref _roundWaterGainFlatBonus,       ref _roundWaterGainPercentBonus,      capPercent: false);
            AccumulateRoundModifier(card.RoundMoneyGainPenalty,    false, ref _roundMoneyGainFlatPenalty,     ref _roundMoneyGainPercentPenalty,    capPercent: true);
            AccumulateRoundModifier(card.RoundMoneyGainBonus,      false, ref _roundMoneyGainFlatBonus,       ref _roundMoneyGainPercentBonus,      capPercent: false);
            AccumulateRoundModifier(card.RoundProjectMoneyPenalty, false, ref _roundProjectMoneyFlatPenalty,  ref _roundProjectMoneyPercentPenalty, capPercent: true);
            AccumulateRoundModifier(card.RoundProjectMoneyBonus,   false, ref _roundProjectMoneyFlatBonus,    ref _roundProjectMoneyPercentBonus,   capPercent: false);
            AccumulateCardEventFlags(card.IsDroughtEvent, card.IsClimateEvent,
                card.IsDeforestationEvent, card.DeforestationProjectMoneyPercentPenalty);

            // ── 5. Dice modifier ─────────────────────────────────────────────────
            if (card.DiceModifier != 0)
                playerData.PendingDiceModifier += card.DiceModifier;

            if (card.InvertsTurnOrder)
                _invertTurnOrderNextRound = true;

            // ── 6. Negative shield ───────────────────────────────────────────────
            if (card.GrantsNegativeShield)
                playerData.HasNegativeShield = true;

            if (card.GrantsDoubleTriviaReward)
                playerData.DoubleTriviaReward = true;

            // ── 7. Teleport ──────────────────────────────────────────────────────
            if (card.TeleportMode == Networking.Models.TeleportMode.ToSpecificIndex && card.SelfMoveToTile >= 0)
            {
                int oldPos = playerData.BoardPosition;
                int nextPos = card.SelfMoveToTile % Mathf.Max(1, _boardTileCount);

                if (nextPos < oldPos)
                {
                    Debug.Log($"[GameManager] Player {player.PlayerId} completed a lap via teleport! Awarding 3 money.");
                    ApplyMoneyDelta(playerData, player, runner, 3, respectShield: false);
                }

                playerData.BoardPosition = nextPos;
                playerData.IsPendingTeleportTileResolution = true;
                Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerMovedEvent?.Raise(player, runner);
            }
            else if (card.TeleportMode == Networking.Models.TeleportMode.ToNearestTileType)
            {
                int currentPos = playerData.BoardPosition;
                int nextTile = -1;
                int distance = -1;
                var targetType = card.TeleportTargetTileType;

                // Search forward for the next tile of this type
                for (int i = 1; i <= _boardTileCount; i++)
                {
                    int checkIdx = (currentPos + i) % Mathf.Max(1, _boardTileCount);
                    if (_tileService.GetTileType(checkIdx) == targetType)
                    {
                        nextTile = checkIdx;
                        distance = i;
                        break;
                    }
                }

                if (nextTile >= 0)
                {
                    Debug.Log($"[GameManager] TeleportToNext: Player {player.PlayerId} at {currentPos} moving to {targetType} tile {nextTile} (dist {distance}).");

                    if (nextTile < currentPos)
                    {
                        Debug.Log($"[GameManager] Player {player.PlayerId} completed a lap via search-teleport! Awarding 3 money.");
                        ApplyMoneyDelta(playerData, player, runner, 3, respectShield: false);
                    }

                    playerData.BoardPosition = nextTile;
                    playerData.IsPendingTeleportTileResolution = true;
                    Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerMovedEvent?.Raise(player, runner);
                }
            }

            playerData.IsAwaitingCardScan = false;
            ClearPendingProjectState(playerData);

            // ── 8. Decision ──────────────────────────────────────────────────────
            if (card.RequiresDecision && card.DecisionScope != Networking.Models.CardDecisionScope.None)
            {
                _pendingDecisionCardId = card.CardId;
                _pendingDecisionScanningPlayer = player;
                _pendingDecisionScope = card.DecisionScope;

                if (card.DecisionScope == Networking.Models.CardDecisionScope.Individual)
                {
                    playerData.IsAwaitingDecisionVote = true;
                }
                else
                {
                    foreach (var p in runner.ActivePlayers)
                    {
                        var d = GetPlayerData(p, runner);
                        if (d != null) d.IsAwaitingDecisionVote = true;
                    }
                }

                State = GameState.Decision;
                Debug.Log($"[GameManager] Card '{card.DisplayName}' opened a {card.DecisionScope} decision.");
                // AdvanceTurn is called once all votes are in — see HandleDecisionVote.
                return;
            }

            if (!playerData.IsPendingTeleportTileResolution)
            {
                AdvanceTurn(runner, _turnAdvancementDelaySeconds);
            }
        }

        public void HandleDecisionVote(PlayerRef player, NetworkRunner runner, int choice)
        {
            if (runner == null || !runner.IsServer) return;
            if (choice != 1 && choice != 2) return;

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsAwaitingDecisionVote) return;

            playerData.PendingDecisionVote = choice;
            playerData.IsAwaitingDecisionVote = false;
            Debug.Log($"[GameManager] Player {player.PlayerId} voted {(choice == 1 ? "A" : "B")} on decision card {_pendingDecisionCardId}.");

            // Check if all required voters have now submitted a vote.
            bool allVoted = true;
            foreach (var p in runner.ActivePlayers)
            {
                var d = GetPlayerData(p, runner);
                if (d == null) continue;

                bool thisVoterNeeded = _pendingDecisionScope == Networking.Models.CardDecisionScope.Collective
                    || p == _pendingDecisionScanningPlayer;

                if (thisVoterNeeded && d.IsAwaitingDecisionVote)
                {
                    allVoted = false;
                    break;
                }
            }

            if (!allVoted) return;

            // ── Tally ────────────────────────────────────────────────────────────
            int votesA = 0, votesB = 0;
            foreach (var p in runner.ActivePlayers)
            {
                var d = GetPlayerData(p, runner);
                if (d == null || d.PendingDecisionVote == 0) continue;
                if (d.PendingDecisionVote == 1) votesA++;
                else votesB++;
            }

            if (!_cardDatabase.TryGetCard(_pendingDecisionCardId, out var decisionCard) || decisionCard == null)
            {
                Debug.LogWarning($"[GameManager] Decision tally failed: card {_pendingDecisionCardId} not found.");
            }
            else
            {
                bool chooseA = votesA >= votesB; // tie → A wins
                var winning = chooseA ? decisionCard.DecisionChoiceA : decisionCard.DecisionChoiceB;
                Debug.Log($"[GameManager] Decision resolved: {(chooseA ? "A" : "B")} wins ({votesA}v{votesB}). Applying '{winning?.Label}'.");
                ApplyCardDecisionChoice(winning, _pendingDecisionScanningPlayer, runner);
            }

            // ── Reset decision state ──────────────────────────────────────────────
            foreach (var p in runner.ActivePlayers)
            {
                var d = GetPlayerData(p, runner);
                if (d == null) continue;
                d.IsAwaitingDecisionVote = false;
                d.PendingDecisionVote = 0;
            }
            _pendingDecisionCardId = -1;

            var scanningPlayerData = GetPlayerData(_pendingDecisionScanningPlayer, runner);
            if (scanningPlayerData == null || !scanningPlayerData.IsPendingTeleportTileResolution)
            {
                AdvanceTurn(runner, _turnAdvancementDelaySeconds);
            }
        }

        public void HandleSkipCardScan(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer) return;

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsActiveTurn || !playerData.IsAwaitingCardScan) return;

            Debug.Log($"[GameManager] Player {player.PlayerId} skipped card scan. Advancing turn.");
            playerData.IsAwaitingCardScan = false;
            ClearPendingProjectState(playerData);
            ClearPendingCardDisplay(playerData);
            AdvanceTurn(runner, _turnAdvancementDelaySeconds);
        }

        private void ClearPendingProjectState(Networking.Models.PlayerSessionData data)
        {
            if (data == null)
            {
                return;
            }

            data.PendingProjectId = 0;
            data.PendingProjectName = default;
            data.PendingProjectPrice = 0;
            data.PendingProjectWaterIncome = 0;
            data.PendingProjectMoneyIncome = 0;
            data.PendingProjectZone = 0;
            data.IsAwaitingProjectScan = false;
            data.IsAwaitingProjectDecision = false;
            data.IsAwaitingCardScan = false;
            data.IsAwaitingTrivia = false;
        }

        private void ClearPendingCardDisplay(Networking.Models.PlayerSessionData data)
        {
            if (data == null)
            {
                return;
            }

            data.PendingCardTitle = default;
            data.PendingCardLore = default;
            data.PendingCardEffect = default;
        }

        private int CountOwnedProjects(Networking.Models.PlayerSessionData data)
        {
            int count = 0;
            if (data.OwnedProjectSlot0Id > 0) count++;
            if (data.OwnedProjectSlot1Id > 0) count++;
            if (data.OwnedProjectSlot2Id > 0) count++;
            return count;
        }

        private bool TryAssignOwnedProject(Networking.Models.PlayerSessionData data, int projectId, Networking.Models.ColombiaZone zone)
        {
            if (data.OwnedProjectSlot0Id <= 0)
            {
                data.OwnedProjectSlot0Id = projectId;
                data.OwnedProjectSlot0Zone = (int)zone;
                return true;
            }

            if (data.OwnedProjectSlot1Id <= 0)
            {
                data.OwnedProjectSlot1Id = projectId;
                data.OwnedProjectSlot1Zone = (int)zone;
                return true;
            }

            if (data.OwnedProjectSlot2Id <= 0)
            {
                data.OwnedProjectSlot2Id = projectId;
                data.OwnedProjectSlot2Zone = (int)zone;
                return true;
            }

            return false;
        }

        private void AccumulateOwnedProjectPassive(int projectId, int zoneValue, ref int totalWater, ref int totalMoney)
        {
            if (projectId <= 0 || _projectDatabase == null) return;
            if (!_projectDatabase.TryGetProject(projectId, out var project) || project == null) return;
            var (water, money) = project.GetIncomeForZone((Networking.Models.ColombiaZone)zoneValue);
            totalWater += water;
            totalMoney += money;
        }

        private void ApplyProjectPassive(int projectId, int zoneValue,
            Networking.Models.PlayerSessionData data, PlayerRef player, NetworkRunner runner)
        {
            if (projectId <= 0 || _projectDatabase == null) return;
            if (!_projectDatabase.TryGetProject(projectId, out var project) || project == null) return;

            var (water, money) = project.GetIncomeForZone((Networking.Models.ColombiaZone)zoneValue);

            // ── Water logic ─────────────────────────────────────────────────────
            bool isDroughtActive = _droughtEventActive || (_activeWeatherCardId >= 0 && _activeWeatherTag == Networking.Models.WeatherTag.Drought);
            if (isDroughtActive && project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.NullifiedByDroughtEvent))
            {
                water = 0;
                Debug.Log($"[GameManager] Project '{project.DisplayName}' water nullified by Drought.");
            }
            else if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.DoublesWaterBelowBasinThreshold))
            {
                int threshold = Mathf.RoundToInt(project.BasinThresholdForBonus * _startingBasinHealth);
                if (_basinService.BasinHealth < threshold)
                {
                    water *= 2;
                    Debug.Log($"[GameManager] Project '{project.DisplayName}' water doubled (basin {_basinService.BasinHealth} < {threshold}).");
                }
            }
            else if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.DoublesWaterAboveBasinThreshold))
            {
                int threshold = Mathf.RoundToInt(project.BasinThresholdForBonus * _startingBasinHealth);
                if (_basinService.BasinHealth > threshold)
                {
                    water *= 2;
                    Debug.Log($"[GameManager] Project '{project.DisplayName}' water doubled (basin {_basinService.BasinHealth} > {threshold}).");
                }
            }

            if (_weatherProjectWaterFlatBonusPerRound != 0 && water > 0)
                water += _weatherProjectWaterFlatBonusPerRound;

            if (water > 0)
            {
                if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.BypassesRoundWaterPenalty))
                {
                    // Write directly — bypasses ApplyWaterDelta round modifiers.
                    data.WaterAmount = Mathf.Max(0, data.WaterAmount + water);
                    Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(player, runner);
                }
                else
                {
                    ApplyWaterDelta(data, player, runner, water, respectShield: false);
                }
            }

            // ── Money logic ─────────────────────────────────────────────────────
            bool isAnyWeatherActive = _activeWeatherCardId >= 0;
            bool isDroughtWeather = isAnyWeatherActive && _activeWeatherTag == Networking.Models.WeatherTag.Drought;
            bool isRainWeather = isAnyWeatherActive && _activeWeatherTag == Networking.Models.WeatherTag.Rain;

            if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.SymmetricWeatherMoney))
            {
                if (isRainWeather)
                {
                    money += project.ClimateEventMoneyBonus;
                    Debug.Log($"[GameManager] Project '{project.DisplayName}' money symmetric boost +{project.ClimateEventMoneyBonus} by Rain.");
                }
                else if (isDroughtWeather)
                {
                    money -= project.ClimateEventMoneyBonus;
                    Debug.Log($"[GameManager] Project '{project.DisplayName}' money symmetric penalty -{project.ClimateEventMoneyBonus} by Drought.");
                }
            }
            else if (isDroughtWeather && project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.BonusMoneyDuringDrought))
            {
                money += project.ClimateEventMoneyBonus;
                Debug.Log($"[GameManager] Project '{project.DisplayName}' money boosted +{project.ClimateEventMoneyBonus} by Drought weather.");
            }
            else if (isAnyWeatherActive && project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.BonusMoneyDuringWeather))
            {
                money += project.ClimateEventMoneyBonus;
                Debug.Log($"[GameManager] Project '{project.DisplayName}' money boosted +{project.ClimateEventMoneyBonus} by active weather.");
            }

            if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.NullifiedMoneyBelowBasinThreshold))
            {
                int threshold = Mathf.RoundToInt(project.BasinThresholdForBonus * _startingBasinHealth);
                if (_basinService.BasinHealth < threshold)
                {
                    money = 0;
                    Debug.Log($"[GameManager] Project '{project.DisplayName}' money nullified (basin {_basinService.BasinHealth} < {threshold}).");
                }
            }

            if (_deforestationEventActive && project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.ReducedByDeforestationEvent))
{
                float deforestationMult = 1f - Mathf.Clamp(_deforestationProjectMoneyPercentPenalty, 0, 100) / 100f;
                money = Mathf.Max(0, Mathf.RoundToInt(money * deforestationMult));
                Debug.Log($"[GameManager] Project '{project.DisplayName}' money reduced by Deforestation ({_deforestationProjectMoneyPercentPenalty}%).");
            }

            // Apply global round project-money modifiers.
            bool hasProjMoneyMod = _roundProjectMoneyFlatPenalty > 0 || _roundProjectMoneyPercentPenalty > 0
                                || _roundProjectMoneyFlatBonus   > 0 || _roundProjectMoneyPercentBonus   > 0;
            if (hasProjMoneyMod && money > 0)
            {
                int afterFlat = Mathf.Max(0, money - _roundProjectMoneyFlatPenalty + _roundProjectMoneyFlatBonus);
                float penaltyMult = 1f - Mathf.Clamp(_roundProjectMoneyPercentPenalty, 0, 100) / 100f;
                float bonusMult   = 1f + _roundProjectMoneyPercentBonus / 100f;
                money = Mathf.Max(0, Mathf.RoundToInt(afterFlat * penaltyMult * bonusMult));
            }

            if (money != 0)
                ApplyMoneyDelta(data, player, runner, money, respectShield: false);
        }

        // ── Water / Money helpers ────────────────────────────────────────────────

        /// <summary>
        /// Central method for all server-side water mutations.
        /// Negative deltas can be blocked by HasNegativeShield (unless respectShield is false).
        /// Positive deltas are reduced by the active round water-gain penalty.
        /// </summary>
        private void ApplyWaterDelta(Networking.Models.PlayerSessionData data, PlayerRef player, NetworkRunner runner, int delta, bool respectShield = true, bool isHydric = false, bool isCatastrophic = false)
        {
            if (data == null || runner == null || !runner.IsServer || delta == 0) return;

            if (delta < 0 && respectShield && data.HasNegativeShield)
            {
                data.HasNegativeShield = false;
                Debug.Log($"[GameManager] Player {player.PlayerId} shield absorbed a negative water effect ({delta}).");
                return;
            }

            int effectiveDelta = delta;

            // ── Project behavior: Contextual Water Modifier ─────────────────────
            if (_projectDatabase != null)
            {
                int[] projectIds = { data.OwnedProjectSlot0Id, data.OwnedProjectSlot1Id, data.OwnedProjectSlot2Id };
                foreach (int id in projectIds)
                {
                    if (id > 0 && _projectDatabase.TryGetProject(id, out var project) && project != null)
                    {
                        if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.ContextualWaterModifier))
                        {
                            if (delta > 0)
                            {
                                bool isLaNina = _activeWeatherCardId >= 0 && _activeWeatherTag == Networking.Models.WeatherTag.Rain;
                                int bonus = (isLaNina || isHydric) ? project.ContextualWaterBonusEnhanced : project.ContextualWaterBonusNormal;
                                effectiveDelta += bonus;
                                Debug.Log($"[GameManager] Project '{project.DisplayName}' bonus: +{bonus} water added to gain of {delta} (LaNina={isLaNina}, Hydric={isHydric}).");
                            }
                            else if (delta < 0 && isCatastrophic)
                            {
                                effectiveDelta -= project.ContextualWaterPenalty;
                                Debug.Log($"[GameManager] Project '{project.DisplayName}' penalty: -{project.ContextualWaterPenalty} water added to loss of {delta} (Catastrophic=true).");
                            }
                        }
                    }
                }
            }

            if (effectiveDelta > 0)
{
                bool hasMod = _roundWaterGainFlatPenalty > 0 || _roundWaterGainPercentPenalty > 0
                           || _roundWaterGainFlatBonus   > 0 || _roundWaterGainPercentBonus   > 0;
                if (hasMod)
                {
                    int afterFlat = Mathf.Max(0, effectiveDelta - _roundWaterGainFlatPenalty + _roundWaterGainFlatBonus);
                    float penaltyMult = 1f - Mathf.Clamp(_roundWaterGainPercentPenalty, 0, 100) / 100f;
                    float bonusMult   = 1f + _roundWaterGainPercentBonus / 100f;
                    effectiveDelta = Mathf.Max(0, Mathf.RoundToInt(afterFlat * penaltyMult * bonusMult));
                    if (effectiveDelta == 0)
                    {
                        Debug.Log($"[GameManager] Player {player.PlayerId} water gain of {delta} fully negated by round modifier.");
                        return;
                    }
                }
            }

            data.WaterAmount = Mathf.Max(0, data.WaterAmount + effectiveDelta);
            Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(player, runner);
        }

        private bool HasProjectWithBehaviour(Networking.Models.PlayerSessionData data, Networking.Models.ProjectPassiveBehaviour behaviour)
        {
            if (_projectDatabase == null) return false;
            
            int[] projectIds = { data.OwnedProjectSlot0Id, data.OwnedProjectSlot1Id, data.OwnedProjectSlot2Id };
            foreach (int id in projectIds)
            {
                if (id > 0 && _projectDatabase.TryGetProject(id, out var project) && project != null)
                {
                    if (project.HasBehaviour(behaviour)) return true;
                }
            }
            return false;
        }

        /// <summary>
/// Central method for all server-side money mutations.
        /// Negative deltas can be blocked by HasNegativeShield (unless respectShield is false).
        /// Positive deltas are reduced by the active round money-gain penalty.
        /// </summary>
        private void ApplyMoneyDelta(Networking.Models.PlayerSessionData data, PlayerRef player, NetworkRunner runner, int delta, bool respectShield = true)
        {
            if (data == null || runner == null || !runner.IsServer || delta == 0) return;

            if (delta < 0 && respectShield && data.HasNegativeShield)
            {
                data.HasNegativeShield = false;
                Debug.Log($"[GameManager] Player {player.PlayerId} shield absorbed a negative money effect ({delta}).");
                return;
            }

            int effectiveDelta = delta;
            if (delta > 0)
            {
                bool hasMod = _roundMoneyGainFlatPenalty > 0 || _roundMoneyGainPercentPenalty > 0
                           || _roundMoneyGainFlatBonus   > 0 || _roundMoneyGainPercentBonus   > 0;
                if (hasMod)
                {
                    int afterFlat = Mathf.Max(0, delta - _roundMoneyGainFlatPenalty + _roundMoneyGainFlatBonus);
                    float penaltyMult = 1f - Mathf.Clamp(_roundMoneyGainPercentPenalty, 0, 100) / 100f;
                    float bonusMult   = 1f + _roundMoneyGainPercentBonus / 100f;
                    effectiveDelta = Mathf.Max(0, Mathf.RoundToInt(afterFlat * penaltyMult * bonusMult));
                    if (effectiveDelta == 0)
                    {
                        Debug.Log($"[GameManager] Player {player.PlayerId} money gain of {delta} fully negated by round modifier.");
                        return;
                    }
                }
            }

            data.MoneyAmount = Mathf.Max(0, data.MoneyAmount + effectiveDelta);
        }

        // ── Decision-choice application helper ──────────────────────────────────

        private void ApplyCardDecisionChoice(Networking.Models.CardDecisionChoice choice, PlayerRef scanningPlayer, NetworkRunner runner)
        {
            if (choice == null || runner == null || !runner.IsServer) return;

            var scannerData = GetPlayerData(scanningPlayer, runner);

            if (choice.WaterDelta != 0 && scannerData != null)
                ApplyWaterDelta(scannerData, scanningPlayer, runner, choice.WaterDelta);

            if (choice.MoneyDelta != 0 && scannerData != null)
                ApplyMoneyDelta(scannerData, scanningPlayer, runner, choice.MoneyDelta);

            if (choice.BasinDelta != 0 && !_weatherLockBasin)
            {
                int basinDelta = (_weatherDoubleBasinRecovery && choice.BasinDelta > 0) ? choice.BasinDelta * 2 : choice.BasinDelta;
                _basinService.ApplyDelta(basinDelta);
                SyncBasinHealthToAllPlayers(runner);
            }

            if (choice.AllPlayersWaterDelta != 0)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    var d = GetPlayerData(p, runner);
                    if (d == null) continue;
                    ApplyWaterDelta(d, p, runner, choice.AllPlayersWaterDelta);
                }
            }

            if (choice.AllPlayersMoneyDelta != 0)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    var d = GetPlayerData(p, runner);
                    if (d == null) continue;
                    ApplyMoneyDelta(d, p, runner, choice.AllPlayersMoneyDelta);
                }
            }

            AccumulateRoundModifier(choice.RoundWaterGainPenalty,    false, ref _roundWaterGainFlatPenalty,    ref _roundWaterGainPercentPenalty,    capPercent: true);
            AccumulateRoundModifier(choice.RoundWaterGainBonus,      false, ref _roundWaterGainFlatBonus,       ref _roundWaterGainPercentBonus,      capPercent: false);
            AccumulateRoundModifier(choice.RoundMoneyGainPenalty,    false, ref _roundMoneyGainFlatPenalty,     ref _roundMoneyGainPercentPenalty,    capPercent: true);
            AccumulateRoundModifier(choice.RoundMoneyGainBonus,      false, ref _roundMoneyGainFlatBonus,       ref _roundMoneyGainPercentBonus,      capPercent: false);
            AccumulateRoundModifier(choice.RoundProjectMoneyPenalty, false, ref _roundProjectMoneyFlatPenalty,  ref _roundProjectMoneyPercentPenalty, capPercent: true);
            AccumulateRoundModifier(choice.RoundProjectMoneyBonus,   false, ref _roundProjectMoneyFlatBonus,    ref _roundProjectMoneyPercentBonus,   capPercent: false);

            if (choice.DiceModifier != 0 && scannerData != null)
                scannerData.PendingDiceModifier += choice.DiceModifier;

            if (choice.GrantsNegativeShield && scannerData != null)
                scannerData.HasNegativeShield = true;

            AccumulateCardEventFlags(choice.IsDroughtEvent, choice.IsClimateEvent,
                choice.IsDeforestationEvent, choice.DeforestationProjectMoneyPercentPenalty);
        }

        // ── Named-event accumulation helper ─────────────────────────────────────

        private void AccumulateCardEventFlags(bool isDrought, bool isClimate, bool isDeforestation, int deforestationPenalty)
        {
            if (isDrought)     _droughtEventActive     = true;
            if (isClimate)     _climateEventActive     = true;
            if (isDeforestation)
            {
                _deforestationEventActive = true;
                _deforestationProjectMoneyPercentPenalty = Mathf.Min(100,
                    _deforestationProjectMoneyPercentPenalty + deforestationPenalty);
            }
        }

        // ── Round-modifier accumulation helper ──────────────────────────────────

        /// <summary>
        /// Routes a card's round-modifier value into the correct flat or percent accumulator.
        /// capPercent=true prevents penalties from exceeding 100% (which would result in negative gains).
        /// </summary>
        private static void AccumulateRoundModifier(int value, bool isPercent, ref int flatAcc, ref int percentAcc, bool capPercent)
        {
            if (value <= 0) return;
            if (isPercent)
            {
                percentAcc += value;
                if (capPercent) percentAcc = Mathf.Min(percentAcc, 100);
            }
            else
            {
                flatAcc += value;
            }
        }

        // ────────────────────────────────────────────────────────────────────────

        private void ApplyPassiveProjectEffects(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null) continue;

                ApplyProjectPassive(data.OwnedProjectSlot0Id, data.OwnedProjectSlot0Zone, data, player, runner);
                ApplyProjectPassive(data.OwnedProjectSlot1Id, data.OwnedProjectSlot1Zone, data, player, runner);
                ApplyProjectPassive(data.OwnedProjectSlot2Id, data.OwnedProjectSlot2Zone, data, player, runner);
            }
        }

        private void SyncBasinHealthToAllPlayers(NetworkRunner runner)
        {
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null)
                {
                    data.BasinHealth = _basinService.BasinHealth;
                }
            }
        }

        private void SyncWeatherToAllPlayers(NetworkRunner runner)
        {
            _activeWeatherVersion++;
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null)
                {
                    data.ActiveWeatherTag = _activeWeatherTag;
                    data.WeatherVersion = _activeWeatherVersion;
                }
            }
        }

        private void EndRound(NetworkRunner runner)
        {
            _roundInProgress = false;
            State = GameState.PassiveEffects;

            if (runner != null && runner.IsServer)
            {
                ApplyPassiveProjectEffects(runner);

                // Weather: basin delta is resolved at end of each round.
                if (_activeWeatherCardId >= 0 && _weatherApplyBasinDeltaAtRoundEnd && _weatherBasinFlatPerRound != 0 && !_weatherLockBasin)
                {
                    _basinService.ApplyDelta(_weatherBasinFlatPerRound);
                    SyncBasinHealthToAllPlayers(runner);

                    if (_basinService.IsDefeated)
                    {
                        SetGameState(GameState.Defeat);
                        foreach (var p in runner.ActivePlayers)
                        {
                            var d = GetPlayerData(p, runner);
                            if (d != null) { d.IsGameOver = true; d.IsDefeat = true; }
                        }
                        Networking.Events.NetworkEventDefinitions.Instance?.OnRoundEndedEvent?.Raise(default, runner);
                        return;
                    }
                }

                // Weather: basin threshold effect check (e.g. at end of duration)
                if (_activeWeatherCardId >= 0 && _cardDatabase.TryGetCard(_activeWeatherCardId, out var weatherCard))
                {
                    if (weatherCard.UseBasinThresholdDelta && _currentRound == _weatherStartRound + _weatherDurationRounds)
                    {
                        int basinHealth = _basinService.BasinHealth;
                        int threshold = Mathf.RoundToInt(_startingBasinHealth * weatherCard.BasinThresholdPercentage);

                        int thresholdDelta = (basinHealth > threshold)
                            ? weatherCard.BasinDeltaAboveThreshold
                            : weatherCard.BasinDeltaBelowThreshold;

                        if (thresholdDelta != 0)
                        {
                            int finalDelta = (_weatherDoubleBasinRecovery && thresholdDelta > 0) ? thresholdDelta * 2 : thresholdDelta;
                            _basinService.ApplyDelta(finalDelta);
                            SyncBasinHealthToAllPlayers(runner);

                            if (_basinService.IsDefeated)
                            {
                                SetGameState(GameState.Defeat);
                                foreach (var p in runner.ActivePlayers)
                                {
                                    var d = GetPlayerData(p, runner);
                                    if (d != null) { d.IsGameOver = true; d.IsDefeat = true; }
                                }
                                Networking.Events.NetworkEventDefinitions.Instance?.OnRoundEndedEvent?.Raise(default, runner);
                                return;
                            }
                        }
                    }
                }
            }

            Networking.Events.NetworkEventDefinitions.Instance?.OnRoundEndedEvent?.Raise(default, runner);

            if (runner == null || !runner.IsServer)
            {
                return;
            }

            EnterMinigameReadyPhase(runner);
        }

        private void EnterMinigameReadyPhase(NetworkRunner runner)
        {
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null)
                {
                    continue;
                }

                data.IsActiveTurn = false;
                data.HasRolledThisTurn = false;
                data.IsInMinigameReadyPhase = true;
                data.IsReadyForMinigame = false;
            }

            Debug.Log($"[GameManager] Round {_currentRound} ended. Waiting for all players to ready up for minigame.");
        }

        public void HandlePlayerReadyForMinigame(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsInMinigameReadyPhase || playerData.IsReadyForMinigame)
            {
                return;
            }

            playerData.IsReadyForMinigame = true;
            Debug.Log($"[GameManager] Player {player.PlayerId} is ready for the minigame.");

            foreach (var activePlayer in runner.ActivePlayers)
            {
                var activeData = GetPlayerData(activePlayer, runner);
                if (activeData == null || !activeData.IsReadyForMinigame)
                {
                    return;
                }
            }

            StartCoroutine(LoadMinigameWhenReady(runner));
        }

        private IEnumerator LoadMinigameWhenReady(NetworkRunner runner)
        {
            if (_nextRoundDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(_nextRoundDelaySeconds);
            }

            if (runner == null || !runner.IsServer)
            {
                yield break;
            }

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null)
                {
                    continue;
                }

                data.IsInMinigameReadyPhase = false;
                data.IsReadyForMinigame = false;
            }

            State = GameState.Minigame;
            
            // Randomly select between the available minigames
            string[] minigameScenes = { "Minigame", "PipeMinigame", "WeatherMinigame", "RainMinigame" };
            string selectedScene = minigameScenes[Random.Range(0, minigameScenes.Length)];

            // Debug override
            selectedScene = Networking.Services.MinigameDebugService.GetMinigameScene(selectedScene);
            
            Debug.Log($"[GameManager] All players ready. Sending everyone to {selectedScene}.");

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null)
                    data.RPC_LoadMinigameScene(selectedScene);
            }
        }

        /// <summary>
        /// Called when returning from minigame scene to start the next round.
        /// Triggered automatically by LobbyCanvas when it detects return from minigame.
        /// </summary>
        public void ResumeAfterMinigame(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer) return;
            if (State == GameState.Defeat || State == GameState.Victory) return;

            if (_currentRound >= Mathf.Max(1, _maxRoundsToWin))
            {
                ApplyProjectEndGameBonuses(runner);
                SetGameState(GameState.Victory);

                foreach (var p in runner.ActivePlayers)
{
                    var d = GetPlayerData(p, runner);
                    if (d != null) d.IsGameOver = true;
                }

                Debug.Log($"[GameManager] Victory reached after round {_currentRound} minigame.");
                return;
            }

            Debug.Log($"[GameManager] Resuming after minigame. Starting round {_currentRound + 1}.");
            StartRound(runner);
        }

        public Networking.Models.PlayerSessionData GetPlayerData(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null)
            {
                Debug.LogWarning($"[GameManager.GetPlayerData] Runner is null for player {player.PlayerId}.");
                return null;
            }

            if (!player.IsRealPlayer)
            {
                Debug.LogWarning("[GameManager.GetPlayerData] Invalid PlayerRef (not a real player).");
                return null;
            }

            if (!runner.IsRunning)
            {
                Debug.LogWarning($"[GameManager.GetPlayerData] Runner is not running for player {player.PlayerId}.");
                return null;
            }

            // Try to get from stored dictionary first (most reliable)
            if (_playerData.ContainsKey(player))
            {
                return _playerData[player];
            }

            // Fallback to runner lookup if not in dictionary
            NetworkObject NO;
            bool foundPlayerObject = false;
            try
            {
                foundPlayerObject = runner.TryGetPlayerObject(player, out NO);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameManager.GetPlayerData] TryGetPlayerObject failed for player {player.PlayerId}: {ex.Message}");
                return null;
            }

            if (foundPlayerObject)
            {
                Networking.Models.PlayerSessionData data = NO.GetComponent<Networking.Models.PlayerSessionData>();
                Debug.Log($"[GameManager.GetPlayerData] Player {player.PlayerId}: Using runner lookup. MinigameClickCount={data?.MinigameClickCount ?? -1}");
                return data;
            }
            else
            {
                Debug.LogWarning($"[GameManager.GetPlayerData] Player {player.PlayerId}: Not found in dictionary or runner!");
                return null;
            }
        }

        public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
        {
            if (_playerData.ContainsKey(player))
            {
                if (_playerData[player].Instance != null)
                {
                    runner.Despawn(_playerData[player].Instance);
                }
                runner.Despawn(_playerData[player].Object);
                _playerData.Remove(player);
            }
        }

        public void LeaveRoom()
        {
            _ = LeaveRoomAsync();
        }

        private async Task LeaveRoomAsync()
        {
            await ShutdownRunner();
        }

        private async Task ShutdownRunner()
        {
            if (Networking.Services.FusionNetworkService.LocalRunner != null)
            {
                await Networking.Services.FusionNetworkService.LocalRunner.Shutdown();
            }
            ResetMatchRuntimeState();
            SetGameState(GameState.Lobby);
            _playerData.Clear();
        }

        private bool IsPositiveWeatherActive()
        {
            if (_activeWeatherCardId < 0) return false;
            // Assuming Rain is the only 'positive' weather for now.
            return _activeWeatherTag == Networking.Models.WeatherTag.Rain;
        }

        private void ApplyProjectRoundStartEffects(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer) return;

            bool positiveWeather = IsPositiveWeatherActive();

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null) continue;

                int[] projectIds = { data.OwnedProjectSlot0Id, data.OwnedProjectSlot1Id, data.OwnedProjectSlot2Id };
                foreach (int id in projectIds)
                {
                    if (id > 0 && _projectDatabase.TryGetProject(id, out var project) && project != null)
                    {
                        if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.BasinRecoveryPerRound))
                        {
                            int recovery = positiveWeather ? project.BasinRecoveryEnhancedAmount : project.BasinRecoveryNormalAmount;
                            if (recovery > 0)
                            {
                                _basinService.ApplyDelta(recovery);
                                Debug.Log($"[GameManager] Project '{project.DisplayName}' recovered {recovery} basin health (weather={positiveWeather}).");
                            }
                        }

                        if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.BasinLossPerRound))
                        {
                            bool isDrought = _activeWeatherCardId >= 0 && _activeWeatherTag == Networking.Models.WeatherTag.Drought;
                            int loss = isDrought ? project.BasinLossDroughtAmount : project.BasinLossNormalAmount;
                            if (loss > 0)
                            {
                                _basinService.ApplyDelta(-loss);
                                Debug.Log($"[GameManager] Project '{project.DisplayName}' reduced basin health by {loss} (weather=Drought:{isDrought}).");
                            }
                        }
}
                }
            }
            SyncBasinHealthToAllPlayers(runner);
        }

        private void ApplyProjectEndGameBonuses(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer) return;

            int currentBasin = _basinService.BasinHealth;

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null) continue;

                int totalBonusWater = 0;
                int[] projectIds = { data.OwnedProjectSlot0Id, data.OwnedProjectSlot1Id, data.OwnedProjectSlot2Id };
                foreach (int id in projectIds)
                {
                    if (id > 0 && _projectDatabase.TryGetProject(id, out var project) && project != null)
                    {
                        if (project.HasBehaviour(Networking.Models.ProjectPassiveBehaviour.EndGameBasinBonus))
                        {
                            int threshold = Mathf.RoundToInt(_startingBasinHealth * project.EndGameBasinThreshold);
                            if (currentBasin > threshold)
                            {
                                totalBonusWater += project.EndGameWaterBonus;
                                Debug.Log($"[GameManager] Player {player.PlayerId} earned end-game bonus from '{project.DisplayName}': {project.EndGameWaterBonus} water.");
                            }
                        }
                    }
                }

                if (totalBonusWater > 0)
                {
                    ApplyWaterDelta(data, player, runner, totalBonusWater, respectShield: false);
                }
            }
        }

        private void ResetMatchRuntimeState()
{
            _turnOrder.Clear();
            _isTurnOrderLocked = false;
            _activeTurnIndex = -1;
            _roundInProgress = false;
            _currentRound = 0;
            _roundWaterGainFlatPenalty    = 0; _roundWaterGainPercentPenalty  = 0;
            _roundWaterGainFlatBonus      = 0; _roundWaterGainPercentBonus    = 0;
            _roundMoneyGainFlatPenalty    = 0; _roundMoneyGainPercentPenalty  = 0;
            _roundMoneyGainFlatBonus      = 0; _roundMoneyGainPercentBonus    = 0;
            _roundProjectMoneyFlatPenalty = 0; _roundProjectMoneyPercentPenalty = 0;
            _roundProjectMoneyFlatBonus   = 0; _roundProjectMoneyPercentBonus = 0;
            _droughtEventActive = false;
            _climateEventActive = false;
            _deforestationEventActive = false;
            _deforestationProjectMoneyPercentPenalty = 0;
            _pendingDecisionCardId = -1;
            ClearActiveWeather();
        }

        private void ClearActiveWeather()
        {
            _activeWeatherCardId               = -1;
            _weatherStartRound                 = 0;
            _weatherDurationRounds             = 0;
            _weatherHydricWaterFlatBonus        = 0;
            _weatherAllPlayersWaterPerTurnDelta = 0;
            _weatherAllPlayersWaterOnTileResolveDelta = 0;
            _weatherDiceRollFlatBonus           = 0;
            _weatherRollDependentRewards        = false;
            _weatherProjectMoneyPercentPenalty  = 0;
            _weatherApplyBasinDeltaAtRoundEnd   = false;
            _weatherBasinFlatPerRound             = 0;
            _weatherProjectMoneyFlatBonusPerRound  = 0;
            _weatherProjectWaterFlatBonusPerRound   = 0;
            _weatherDoubleBasinRecovery             = false;
            _activeWeatherTag                      = WeatherTag.None;
            _weatherLockBasin                      = false;
            _weatherNullifyHydricWater             = false;
        }

        public void DisconnectedFromSession(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("Disconnected from the session");
            ExitSession();
        }

        public void ExitSession()
        {
            _ = ShutdownRunner();
            SceneManager.LoadScene(0);
        }

        public void ExitGame()
        {
            _ = ShutdownRunner();
            Application.Quit();
        }

        public void SetPlayerDataObject(PlayerRef objectInputAuthority, Networking.Models.PlayerSessionData playerData)
        {
            if (!_playerData.ContainsKey(objectInputAuthority))
            {
                _playerData.Add(objectInputAuthority, playerData);
            }
        }

        /// <summary>
        /// Get list of available characters (not selected by any player).
        /// Used by character selection UI to display available options.
        /// </summary>
        public List<Networking.Models.CharacterConfig> GetAvailableCharacters(NetworkRunner runner)
        {
            var selectedIds = new HashSet<int>();

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = GetPlayerData(player, runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                {
                    selectedIds.Add(playerData.SelectedCharacterId);
                }
            }

            return CharacterDatabase.Instance.GetAvailableCharacters(selectedIds);
        }

        /// <summary>
        /// Get set of all selected character IDs across all connected players.
        /// </summary>
        public HashSet<int> GetSelectedCharacterIds(NetworkRunner runner)
        {
            var selectedIds = new HashSet<int>();

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = GetPlayerData(player, runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                {
                    selectedIds.Add(playerData.SelectedCharacterId);
                }
            }

            return selectedIds;
        }

        /// <summary>
        /// Check if a specific character is available.
        /// </summary>
        public void HandleProjectCardScan(PlayerRef player, NetworkRunner runner, int projectId)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsActiveTurn || !playerData.IsAwaitingProjectScan)
            {
                return;
            }

            if (_tileService.GetTileType(playerData.BoardPosition) != Networking.Services.SliceTileType.Project)
            {
                return;
            }

            if (_projectDatabase == null || !_projectDatabase.TryGetProject(projectId, out var project) || project == null)
            {
                Debug.LogWarning($"[GameManager] Project scan rejected: unknown project ID {projectId}.");
                return;
            }

            var zone = GetZoneAtPosition(playerData.BoardPosition);
            var (waterIncome, moneyIncome) = project.GetIncomeForZone(zone);

            Debug.Log($"[GameManager] Project scan: project='{project.DisplayName}' (id={project.ProjectId}), " +
                      $"boardPos={playerData.BoardPosition}, resolvedZone={zone}, " +
                      $"waterIncome={waterIncome}, moneyIncome={moneyIncome}.");

            playerData.PendingProjectId = project.ProjectId;
            playerData.PendingProjectName = project.DisplayName;
            playerData.PendingProjectDescription = project.Description;
            playerData.PendingProjectPrice = project.Price;
            playerData.PendingProjectWaterIncome = waterIncome;
            playerData.PendingProjectMoneyIncome = moneyIncome;
            playerData.PendingProjectZone = (int)zone;
            playerData.IsAwaitingProjectScan = false;
            playerData.IsAwaitingProjectDecision = true;
            State = GameState.Decision;
        }

        public void HandleBuyPendingProject(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsActiveTurn || !playerData.IsAwaitingProjectDecision)
            {
                return;
            }

            if (playerData.PendingProjectId <= 0 || playerData.PendingProjectPrice > playerData.MoneyAmount)
            {
                return;
            }

            if (!TryAssignOwnedProject(playerData, playerData.PendingProjectId, (Networking.Models.ColombiaZone)playerData.PendingProjectZone))
            {
                return;
            }

            playerData.MoneyAmount = Mathf.Max(0, playerData.MoneyAmount - playerData.PendingProjectPrice);
            ClearPendingProjectState(playerData);
            State = GameState.BasinCheck;
            AdvanceTurn(runner, _turnAdvancementDelaySeconds);
        }

        public void HandleDeclinePendingProject(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsActiveTurn)
            {
                return;
            }

            ClearPendingProjectState(playerData);
            State = GameState.BasinCheck;
            AdvanceTurn(runner, _turnAdvancementDelaySeconds);
        }

        public bool IsCharacterAvailable(int characterId, NetworkRunner runner)
        {
            var selectedIds = GetSelectedCharacterIds(runner);
            return CharacterDatabase.Instance.IsCharacterAvailable(characterId, selectedIds);
        }
    }
}
