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

        [Header("Round Slice Config")]
        [SerializeField] private int _boardTileCount = 24;
        [SerializeField] private int _startingWater = 10;
        [SerializeField] private int _startingBasinHealth = 100;
        [SerializeField] private int _hydricWaterGain = 2;
        [SerializeField] private int _hydricBasinBonus = 1;
        [SerializeField] private int _catastrophicWaterPenalty = 2;
        [SerializeField] private int _catastrophicBasinPenalty = 5;
        [SerializeField] private int _maxRoundsToWin = 3;
        [SerializeField] private float _nextRoundDelaySeconds = 1.25f;

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
            _tileService = new Networking.Services.TileService();
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
            ResolveTileAndApplyEffects(playerData, runner);
            AdvanceTurn(runner);
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
                if (data == null)
                {
                    continue;
                }

                data.LastDiceRoll = 0;
                data.LastDiceRollTime = 0f;
                data.IsActiveTurn = false;
                data.HasRolledThisTurn = false;

                if (data.WaterAmount <= 0)
                {
                    data.WaterAmount = _startingWater;
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

        private void ResolveTileAndApplyEffects(Networking.Models.PlayerSessionData playerData, NetworkRunner runner)
        {
            State = GameState.BasinCheck;

            var tileType = _tileService.GetTileType(playerData.BoardPosition);
            int waterDelta;
            int basinDelta;

            if (tileType == Networking.Services.SliceTileType.Hydric)
            {
                waterDelta = _tileService.ResolveHydricWaterDelta(_hydricWaterGain);
                basinDelta = _tileService.ResolveHydricBasinDelta(_hydricBasinBonus);
            }
            else
            {
                waterDelta = _tileService.ResolveCatastrophicWaterDelta(_catastrophicWaterPenalty);
                basinDelta = _tileService.ResolveCatastrophicBasinDelta(_catastrophicBasinPenalty);
            }

            playerData.WaterAmount = Mathf.Max(0, playerData.WaterAmount + waterDelta);
            Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(playerData.Object.InputAuthority, runner);

            _basinService.ApplyDelta(basinDelta);
            SyncBasinHealthToAllPlayers(runner);
            Networking.Events.NetworkEventDefinitions.Instance?.OnBasinStateChangedEvent?.Raise(playerData.Object.InputAuthority, runner);

            if (_basinService.IsDefeated)
            {
                SetGameState(GameState.Defeat);
                _roundInProgress = false;
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
            Networking.Events.NetworkEventDefinitions.Instance?.OnRoundEndedEvent?.Raise(default, runner);

            if (runner == null || !runner.IsServer)
            {
                return;
            }

            if (_currentRound >= Mathf.Max(1, _maxRoundsToWin))
            {
                SetGameState(GameState.Victory);
                Debug.Log($"[GameManager] Victory reached at round {_currentRound}.");
                return;
            }

            StartCoroutine(StartNextRoundRoutine(runner));
        }

        private IEnumerator StartNextRoundRoutine(NetworkRunner runner)
        {
            if (_nextRoundDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(_nextRoundDelaySeconds);
            }

            if (runner == null || !runner.IsServer)
            {
                yield break;
            }

            if (State == GameState.Defeat || State == GameState.Victory)
            {
                yield break;
            }

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
        public bool IsCharacterAvailable(int characterId, NetworkRunner runner)
        {
            var selectedIds = GetSelectedCharacterIds(runner);
            return CharacterDatabase.Instance.IsCharacterAvailable(characterId, selectedIds);
        }
    }
}
