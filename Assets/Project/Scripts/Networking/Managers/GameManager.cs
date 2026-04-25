using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;
using System.Linq;

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

        public GameState State { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
            _activeTurnIndex = -1;

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

            playerData.BoardPosition = (playerData.BoardPosition + diceRoll) % Mathf.Max(1, _boardTileCount);
            Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerMovedEvent?.Raise(player, runner);

            State = GameState.TileResolve;
            bool shouldAdvanceTurn = ResolveTileAndApplyEffects(playerData, runner);
            if (shouldAdvanceTurn)
            {
                AdvanceTurn(runner);
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

        private void AdvanceTurn(NetworkRunner runner)
        {
            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null)
                {
                    data.IsActiveTurn = false;
                    data.HasRolledThisTurn = false;
                    data.HasScannedARThisTurn = false;
                    ClearPendingProjectState(data);
                }
            }

            _activeTurnIndex++;
            if (_activeTurnIndex >= _turnOrder.Count)
            {
                EndRound(runner);
                return;
            }

            var activePlayer = _turnOrder[_activeTurnIndex];
            var activeData = GetPlayerData(activePlayer, runner);
            if (activeData != null)
            {
                activeData.IsActiveTurn = true;
            }

            State = GameState.PlayerTurn;
            Networking.Events.NetworkEventDefinitions.Instance?.OnTurnStartedEvent?.Raise(activePlayer, runner);
        }

        private bool ResolveTileAndApplyEffects(Networking.Models.PlayerSessionData playerData, NetworkRunner runner)
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
                return BeginDrawCardTileFlow(playerData);
            }

            if (tileType == Networking.Services.SliceTileType.Trivia)
            {
                return true;
            }

            int waterDelta;
            int moneyDelta;
            int basinDelta;

            if (tileType == Networking.Services.SliceTileType.Hydric)
            {
                waterDelta = _tileService.ResolveHydricWaterDelta(_hydricWaterGain);
                moneyDelta = _hydricMoneyGain;
                basinDelta = _tileService.ResolveHydricBasinDelta(_hydricBasinBonus);
            }
            else
            {
                waterDelta = _tileService.ResolveCatastrophicWaterDelta(_catastrophicWaterPenalty);
                moneyDelta = -_catastrophicMoneyPenalty;
                basinDelta = _tileService.ResolveCatastrophicBasinDelta(_catastrophicBasinPenalty);
            }

            playerData.WaterAmount = Mathf.Max(0, playerData.WaterAmount + waterDelta);
            Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(playerData.Object.InputAuthority, runner);

            playerData.MoneyAmount = Mathf.Max(0, playerData.MoneyAmount + moneyDelta);

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

        private bool BeginDrawCardTileFlow(Networking.Models.PlayerSessionData playerData)
        {
            ClearPendingProjectState(playerData);

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

            Debug.Log($"[GameManager] Card scanned: '{card.DisplayName}' (id={card.CardId}), " +
                      $"water={card.WaterDelta}, money={card.MoneyDelta}, basin={card.BasinDelta}.");

            if (card.WaterDelta != 0)
            {
                playerData.WaterAmount = Mathf.Max(0, playerData.WaterAmount + card.WaterDelta);
                Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(player, runner);
            }

            if (card.MoneyDelta != 0)
            {
                playerData.MoneyAmount = Mathf.Max(0, playerData.MoneyAmount + card.MoneyDelta);
            }

            if (card.BasinDelta != 0)
            {
                _basinService.ApplyDelta(card.BasinDelta);
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

            playerData.IsAwaitingCardScan = false;
            ClearPendingProjectState(playerData);
            AdvanceTurn(runner);
        }

        public void HandleSkipCardScan(PlayerRef player, NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer) return;

            var playerData = GetPlayerData(player, runner);
            if (playerData == null || !playerData.IsActiveTurn || !playerData.IsAwaitingCardScan) return;

            Debug.Log($"[GameManager] Player {player.PlayerId} skipped card scan. Advancing turn.");
            playerData.IsAwaitingCardScan = false;
            ClearPendingProjectState(playerData);
            AdvanceTurn(runner);
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
            if (projectId <= 0 || _projectDatabase == null)
            {
                return;
            }

            if (!_projectDatabase.TryGetProject(projectId, out var project) || project == null)
            {
                return;
            }

            var (water, money) = project.GetIncomeForZone((Networking.Models.ColombiaZone)zoneValue);
            totalWater += water;
            totalMoney += money;
        }

        private void ApplyPassiveProjectEffects(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
            {
                return;
            }

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data == null)
                {
                    continue;
                }

                int waterDelta = 0;
                int moneyDelta = 0;
                AccumulateOwnedProjectPassive(data.OwnedProjectSlot0Id, data.OwnedProjectSlot0Zone, ref waterDelta, ref moneyDelta);
                AccumulateOwnedProjectPassive(data.OwnedProjectSlot1Id, data.OwnedProjectSlot1Zone, ref waterDelta, ref moneyDelta);
                AccumulateOwnedProjectPassive(data.OwnedProjectSlot2Id, data.OwnedProjectSlot2Zone, ref waterDelta, ref moneyDelta);

                if (waterDelta != 0)
                {
                    data.WaterAmount = Mathf.Max(0, data.WaterAmount + waterDelta);
                    Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(player, runner);
                }

                if (moneyDelta != 0)
                {
                    data.MoneyAmount = Mathf.Max(0, data.MoneyAmount + moneyDelta);
                }
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

        private void EndRound(NetworkRunner runner)
        {
            _roundInProgress = false;
            State = GameState.PassiveEffects;

            if (runner != null && runner.IsServer)
            {
                ApplyPassiveProjectEffects(runner);
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
            Debug.Log($"[GameManager] All players ready. Sending everyone to minigame.");

            foreach (var player in runner.ActivePlayers)
            {
                var data = GetPlayerData(player, runner);
                if (data != null)
                    data.RPC_LoadMinigameScene();
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

        private void ResetMatchRuntimeState()
        {
            _turnOrder.Clear();
            _isTurnOrderLocked = false;
            _activeTurnIndex = -1;
            _roundInProgress = false;
            _currentRound = 0;
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
            AdvanceTurn(runner);
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
            AdvanceTurn(runner);
        }

        public bool IsCharacterAvailable(int characterId, NetworkRunner runner)
        {
            var selectedIds = GetSelectedCharacterIds(runner);
            return CharacterDatabase.Instance.IsCharacterAvailable(characterId, selectedIds);
        }
    }
}
