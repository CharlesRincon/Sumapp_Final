using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;
using System.Linq;

namespace Networking.UI
{
    public class LobbyCanvas : MonoBehaviour
    {
        private GameMode _gameMode;

        public string Nickname = "Player";
        public Networking.Managers.GameLauncher Launcher;

        public FusionEvent OnPlayerJoinedEvent;
        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnShutdownEvent;
        public FusionEvent OnPlayerDataSpawnedEvent;
        public FusionEvent OnCharacterSelectionCompleteEvent;
        public FusionEvent OnGameLoadEvent;

        [Space]
        [SerializeField] private GameObject _initPanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private TextMeshProUGUI _lobbyPlayerText;
        [SerializeField] private TextMeshProUGUI _lobbyRoomName;
        [SerializeField] private Button _startButton;
        [Space]
        [SerializeField] private GameObject _gameLobbyPanel;
        [SerializeField] private Image _gameLobbyCharacterImage;
        [SerializeField] private TextMeshProUGUI _diceResultText;
        [SerializeField] private Button _rollDiceButton;
        [SerializeField] private Button _openVuforiaButton;
        [SerializeField] private TextMeshProUGUI _roundStatusText;
        [SerializeField] private TextMeshProUGUI _turnStatusText;
        [SerializeField] private Image _basinHealthImage;
        [SerializeField] private Transform _rivalPlayersContainer;
        [SerializeField] private GameObject _rivalPlayerPrefab;
        [SerializeField] private GameObject _minigameReadyPanel;
        [SerializeField] private TextMeshProUGUI _minigameReadyText;
        [SerializeField] private TextMeshProUGUI _minigameReadyTileText;
        [SerializeField] private TextMeshProUGUI _minigameReadyWaterText;
        [SerializeField] private Button _minigameReadyButton;
        [Space]
        [SerializeField] private TurnOrderPanel _turnOrderPanel;
        [Space]
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private Transform _victoryRankingContainer;
        [SerializeField] private GameObject _victoryRankingEntryPrefab;
        [SerializeField] private Button _victoryReturnButton;
        [Space]
        [SerializeField] private GameObject _vuforiaPanel;
        [SerializeField] private GameObject _backgroundImage;
        [SerializeField] private GameObject _vuforiaARCamera;
        [Space]
        [SerializeField] private GameObject _modeButtons;
        [SerializeField] private GameObject _roomInputsPanel;
        [SerializeField] private TextMeshProUGUI _roomActionText;
        [SerializeField] private TextMeshProUGUI _roomActionButtonText;
        [SerializeField] private TMP_InputField _nickname;
        [SerializeField] private TMP_InputField _room;

        /// <summary>
        /// Character Selection UI panel (set in inspector).
        /// Displayed when host presses "Start Game" button.
        /// </summary>
        [Space]
        [SerializeField] private CharacterSelectionPanel _characterSelectionPanel;

        /// <summary>
        /// Prefab for CharacterSelectionManager NetworkBehaviour.
        /// Spawned when character selection phase begins (only on host).
        /// </summary>
        [SerializeField] private NetworkPrefabRef _characterSelectionManagerPrefab;

        /// <summary>
        /// Get the character selection panel reference (for external testing/debugging).
        /// </summary>
        public CharacterSelectionPanel CharacterSelectionPanel => _characterSelectionPanel;

        private readonly Dictionary<PlayerRef, RivalPlayerCardView> _rivalPlayerCards = new Dictionary<PlayerRef, RivalPlayerCardView>();

        private sealed class RivalPlayerCardView
        {
            public GameObject Root;
            public Image CharacterImage;
            public TextMeshProUGUI WaterText;
            public TextMeshProUGUI RivalNameText;
        }

        private void OnEnable()
        {
            OnPlayerJoinedEvent.RegisterResponse(ShowLobbyCanvas);
            OnShutdownEvent.RegisterResponse(ResetCanvas);
            OnPlayerLeftEvent.RegisterResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RegisterResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RegisterResponse(UpdateGameLobbyList);

            // Load character selection complete event from resources if not assigned in inspector
            if (OnCharacterSelectionCompleteEvent == null)
            {
                OnCharacterSelectionCompleteEvent = Resources.Load<FusionEvent>("Events/OnCharacterSelectionCompleteEvent");
            }
            if (OnCharacterSelectionCompleteEvent != null)
            {
                OnCharacterSelectionCompleteEvent.RegisterResponse(OnCharacterSelectionComplete);
            }

            // Load game load event from resources if not assigned in inspector
            if (OnGameLoadEvent == null)
            {
                OnGameLoadEvent = Resources.Load<FusionEvent>("Events/OnGameLoadEvent");
            }
            if (OnGameLoadEvent != null)
            {
                OnGameLoadEvent.RegisterResponse(OnGameLoad);
            }

            // Auto-find TurnOrderPanel if not assigned
            if (_turnOrderPanel == null)
            {
                _turnOrderPanel = FindFirstObjectByType<TurnOrderPanel>();
                if (_turnOrderPanel != null)
                {
                    Debug.Log("[LobbyCanvas] TurnOrderPanel auto-found and assigned.");
                }
                else
                {
                    Debug.LogError("[LobbyCanvas] TurnOrderPanel not assigned in inspector and not found in scene!");
                }
            }

            // Wire Vuforia button if assigned
            if (_openVuforiaButton != null)
            {
                _openVuforiaButton.onClick.AddListener(OpenVuforiaPanel);
            }

            // Wire roll dice button if assigned
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
                _rollDiceButton.interactable = false;  // Disabled until game is ready
            }

            if (_minigameReadyButton != null)
            {
                _minigameReadyButton.onClick.AddListener(OnMinigameReadyClicked);
            }

            if (_victoryReturnButton != null)
            {
                _victoryReturnButton.onClick.AddListener(OnVictoryReturnClicked);
            }

            if (_minigameReadyPanel != null)
            {
                _minigameReadyPanel.SetActive(false);
            }

            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(false);
            }

            InitializeMinigameReadyPlayerStatus();
        }

        private void OnDisable()
        {
            OnPlayerJoinedEvent.RemoveResponse(ShowLobbyCanvas);
            OnShutdownEvent.RemoveResponse(ResetCanvas);
            OnPlayerLeftEvent.RemoveResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RemoveResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RemoveResponse(UpdateGameLobbyList);

            if (OnCharacterSelectionCompleteEvent != null)
            {
                OnCharacterSelectionCompleteEvent.RemoveResponse(OnCharacterSelectionComplete);
            }

            if (OnGameLoadEvent != null)
            {
                OnGameLoadEvent.RemoveResponse(OnGameLoad);
            }

            // Wire Vuforia button if assigned
            if (_openVuforiaButton != null)
            {
                _openVuforiaButton.onClick.RemoveListener(OpenVuforiaPanel);
            }

            // Wire roll dice button if assigned
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.RemoveListener(OnRollDiceClicked);
            }

            if (_minigameReadyButton != null)
            {
                _minigameReadyButton.onClick.RemoveListener(OnMinigameReadyClicked);
            }

            if (_victoryReturnButton != null)
            {
                _victoryReturnButton.onClick.RemoveListener(OnVictoryReturnClicked);
            }
        }

        private void Start()
        {
            Debug.Log("[LobbyCanvas] Start() called.");
            InitializeMinigameReadyPlayerStatus();
        }

        private bool _sessionRestored;
        private bool _diceRolling;

        private void Update()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;

            RefreshMinigameReadyPlayerStatus(runner);

            // On first frame with a runner, check if we're restoring an existing session
            if (!_sessionRestored && runner != null)
            {
                _sessionRestored = true;

                // Check if any player has a selected character (meaning selection already happened)
                bool anyCharacterSelected = false;
                foreach (var player in runner.ActivePlayers)
                {
                    var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                    if (playerData != null && playerData.SelectedCharacterId > 0)
                    {
                        anyCharacterSelected = true;
                        break;
                    }
                }

                // If characters are selected but no CharacterSelectionManager, we're returning from minigame
                if (anyCharacterSelected && FindFirstObjectByType<Networking.Managers.CharacterSelectionManager>() == null)
                {
                    Debug.Log("[LobbyCanvas] Detected return from minigame. Showing GameLobbyPanel.");

                    // Hide init/lobby panels
                    if (_initPanel != null)
                        _initPanel.SetActive(false);
                    if (_lobbyPanel != null)
                        _lobbyPanel.SetActive(false);
                    if (_modeButtons != null)
                        _modeButtons.SetActive(false);
                    if (_characterSelectionPanel != null && _characterSelectionPanel.gameObject.activeSelf)
                        _characterSelectionPanel.Hide();

                    // Show game lobby panel
                    if (_gameLobbyPanel != null)
                    {
                        _gameLobbyPanel.SetActive(true);
                        UpdateGameLobbyList(PlayerRef.None, runner);
                        Debug.Log("[LobbyCanvas] Game lobby panel shown (restored session).");
                    }

                    // Host auto-starts the next round
                    if (runner.IsServer)
                    {
                        Networking.Managers.GameManager.Instance?.ResumeAfterMinigame(runner);
                    }

                    return; // Skip the rest of Update() logic
                }
                else if (!anyCharacterSelected)
                {
                    Debug.Log("[LobbyCanvas] Fresh session - no characters selected yet. Showing normal startup.");
                }
            }

            // Normal Update() logic for fresh sessions
            // For non-host clients: if CharacterSelectionManager exists and lobby is still showing, 
            // hide it and show character selection panel instead
            if (runner != null && !runner.IsServer && _lobbyPanel.activeSelf)
            {
                var selectionManager = FindFirstObjectByType<Networking.Managers.CharacterSelectionManager>();
                if (selectionManager != null)
                {
                    // Character selection phase has started on host, show panel for non-host clients
                    _lobbyPanel.SetActive(false);
                    if (_characterSelectionPanel != null)
                    {
                        _characterSelectionPanel.Show();
                    }
                }
            }

            // If returning from minigame (players present AND characters selected but no CharacterSelectionManager),
            // show GameLobbyPanel instead of lobby
            if (runner != null && runner.ActivePlayers.Count() > 0 && _lobbyPanel.activeSelf)
            {
                var selectionManager = FindFirstObjectByType<Networking.Managers.CharacterSelectionManager>();
                if (selectionManager == null)
                {
                    // Check if any character has been selected (minigame return indicator)
                    bool anyCharacterSelected = false;
                    foreach (var player in runner.ActivePlayers)
                    {
                        var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                        if (playerData != null && playerData.SelectedCharacterId > 0)
                        {
                            anyCharacterSelected = true;
                            break;
                        }
                    }

                    // Only show GameLobbyPanel if characters are selected (minigame return)
                    if (anyCharacterSelected)
                    {
                        _lobbyPanel.SetActive(false);
                        if (_gameLobbyPanel != null)
                        {
                            _gameLobbyPanel.SetActive(true);
                            UpdateGameLobbyList(PlayerRef.None, runner);
                            Debug.Log("[LobbyCanvas] Showing game lobby panel (returning from minigame).");
                        }
                    }
                }
            }

            // Poll turn state while game lobby is active
            if (_gameLobbyPanel != null && _gameLobbyPanel.activeSelf && runner != null)
            {
                RefreshTurnUI(runner);
                RefreshRivalPlayersUI(runner);
                RefreshMinigameReadyPanel(runner);
                RefreshBasinHealthImage(runner);
                RefreshVictoryPanel(runner);
            }
        }

        /// <summary>
        /// Polls [Networked] IsActiveTurn every frame to update button and turn indicator.
        /// FusionEvent is local-only so AdvanceTurn (host) events don't reach clients;
        /// reading the synced property directly works on every client.
        /// </summary>
        private void RefreshTurnUI(NetworkRunner runner)
        {
            var gm = Networking.Managers.GameManager.Instance;
            if (gm == null) return;

            var localData = gm.GetPlayerData(runner.LocalPlayer, runner);
            bool isMyTurn = localData != null && localData.IsActiveTurn;

            // Button: enabled only when it's our turn and we haven't started rolling
            if (_rollDiceButton != null)
                _rollDiceButton.interactable = isMyTurn && !_diceRolling;

            if (_roundStatusText != null)
            {
                _roundStatusText.text = BuildRoundStatusText(gm);
            }

            // Turn indicator text (skip while DiceUI is animating)
            if (_turnStatusText != null && !_diceRolling)
            {
                _turnStatusText.text = BuildTurnStatusText(gm, runner, isMyTurn);
            }
        }

        private void RefreshMinigameReadyPanel(NetworkRunner runner)
        {
            if (_minigameReadyPanel == null)
            {
                return;
            }

            var gameManager = Networking.Managers.GameManager.Instance;
            var localData = gameManager?.GetPlayerData(runner.LocalPlayer, runner);
            bool showPanel = localData != null && localData.IsInMinigameReadyPhase && !_diceRolling;

            if (_minigameReadyPanel.activeSelf != showPanel)
            {
                _minigameReadyPanel.SetActive(showPanel);
            }

            if (!showPanel)
            {
                return;
            }

            int readyCount = 0;
            int totalPlayers = 0;
            foreach (var player in runner.ActivePlayers)
            {
                var data = gameManager.GetPlayerData(player, runner);
                if (data == null || !data.IsInMinigameReadyPhase)
                {
                    continue;
                }

                totalPlayers++;
                if (data.IsReadyForMinigame)
                {
                    readyCount++;
                }
            }

            if (_minigameReadyText != null)
            {
                _minigameReadyText.text = $"Waiting for the other players... {readyCount}/{Mathf.Max(1, totalPlayers)} ready";
            }

            if (_minigameReadyTileText != null)
            {
                var tileType = gameManager.GetTileTypeAtPosition(localData.BoardPosition);
                _minigameReadyTileText.text = BuildMinigameTileText(localData.BoardPosition, tileType);
            }

            if (_minigameReadyWaterText != null)
            {
                _minigameReadyWaterText.text = BuildMinigameWaterText(localData.WaterAmount);
            }

            if (_minigameReadyButton != null)
            {
                _minigameReadyButton.interactable = !localData.IsReadyForMinigame;
            }
        }

        private void InitializeMinigameReadyPlayerStatus()
        {
            RefreshMinigameReadyPlayerStatus(null);
        }

        private void RefreshMinigameReadyPlayerStatus(NetworkRunner runner)
        {
            var gameManager = Networking.Managers.GameManager.Instance;
            var localData = runner != null && gameManager != null
                ? gameManager.GetPlayerData(runner.LocalPlayer, runner)
                : null;

            int boardPosition = localData != null
                ? localData.BoardPosition
                : gameManager != null ? gameManager.InitialBoardPosition : 0;

            int waterAmount = localData != null
                ? localData.WaterAmount
                : gameManager != null ? gameManager.StartingWater : 10;

            var tileType = gameManager != null
                ? gameManager.GetTileTypeAtPosition(boardPosition)
                : Networking.Services.SliceTileType.Start;

            if (_minigameReadyTileText != null)
            {
                _minigameReadyTileText.text = BuildMinigameTileText(boardPosition, tileType);
            }

            if (_minigameReadyWaterText != null)
            {
                _minigameReadyWaterText.text = BuildMinigameWaterText(waterAmount);
            }
        }

        private bool _victoryShown;

        private void RefreshVictoryPanel(NetworkRunner runner)
        {
            if (_victoryPanel == null || _victoryShown) return;

            var gm = Networking.Managers.GameManager.Instance;
            if (gm == null) return;

            var localData = gm.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || !localData.IsGameOver) return;

            _victoryShown = true;

            // Hide other panels
            if (_minigameReadyPanel != null) _minigameReadyPanel.SetActive(false);
            if (_rollDiceButton != null) _rollDiceButton.interactable = false;
            if (_openVuforiaButton != null) _openVuforiaButton.interactable = false;

            if (localData.IsDefeat)
            {
                PopulateDefeatMessage();
            }
            else
            {
                PopulateVictoryRanking(runner, gm);
            }

            _victoryPanel.SetActive(true);
            Debug.Log($"[LobbyCanvas] {(localData.IsDefeat ? "Defeat" : "Victory")} panel shown.");
        }

        private void PopulateVictoryRanking(NetworkRunner runner, Networking.Managers.GameManager gm)
        {
            if (_victoryRankingContainer == null || _victoryRankingEntryPrefab == null) return;

            // Clear previous entries
            foreach (Transform child in _victoryRankingContainer)
            {
                Destroy(child.gameObject);
            }

            // Gather all players sorted by water descending
            var ranked = runner.ActivePlayers
                .Select(player => gm.GetPlayerData(player, runner))
                .Where(data => data != null)
                .OrderByDescending(data => data.WaterAmount)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                var data = ranked[i];
                var entry = Instantiate(_victoryRankingEntryPrefab, _victoryRankingContainer);
                entry.name = $"Rank_{i + 1}";

                var label = entry.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"#{i + 1}. {data.Nick} — Water: {data.WaterAmount}";
                }
            }
        }

        private void PopulateDefeatMessage()
        {
            if (_victoryRankingContainer == null || _victoryRankingEntryPrefab == null) return;

            foreach (Transform child in _victoryRankingContainer)
            {
                Destroy(child.gameObject);
            }

            var entry = Instantiate(_victoryRankingEntryPrefab, _victoryRankingContainer);
            entry.name = "DefeatMessage";

            var label = entry.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "<color=#FF4444><b>Everyone Lost!</b></color>\nThe basin has been destroyed.";
            }
        }

        private void RefreshBasinHealthImage(NetworkRunner runner)
        {
            if (_basinHealthImage == null) return;

            var gm = Networking.Managers.GameManager.Instance;
            if (gm == null) return;

            var localData = gm.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null) return;

            int basinHealth = localData.BasinHealth;
            if (localData.CurrentRound <= 0 && basinHealth <= 0)
            {
                basinHealth = gm.StartingBasinHealth;
            }

            float percentage = (float)basinHealth / Mathf.Max(1, gm.StartingBasinHealth) * 100f;

            Color color;
            if (percentage > 80f)
                color = Color.green;
            else if (percentage > 20f)
                color = Color.yellow;
            else
                color = Color.red;

            _basinHealthImage.color = color;
        }

        private void OnVictoryReturnClicked()
        {
            Debug.Log("[LobbyCanvas] Victory return button clicked — ending session.");
            Networking.Managers.GameManager.Instance?.ExitSession();
        }

        public void NotifyDiceRollCompleted()
        {
            _diceRolling = false;
        }

        private void OnMinigameReadyClicked()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner
                         ?? FindFirstObjectByType<NetworkRunner>();
            if (runner == null)
            {
                Debug.LogError("[LobbyCanvas] NetworkRunner not found for minigame ready request.");
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null)
            {
                Debug.LogError("[LobbyCanvas] Local PlayerSessionData not found for minigame ready request.");
                return;
            }

            localData.RPC_RequestMinigameReady();

            if (_minigameReadyButton != null)
            {
                _minigameReadyButton.interactable = false;
            }
        }

        private static string BuildRoundStatusText(Networking.Managers.GameManager gameManager)
        {
            int currentRound = Mathf.Max(1, gameManager.CurrentRound);
            return $"Round {currentRound}/{gameManager.MaxRoundsToWin}";
        }

        private static string BuildTurnStatusText(Networking.Managers.GameManager gameManager, NetworkRunner runner, bool isMyTurn)
        {
            if (isMyTurn)
            {
                return $"<color=#00FF00><b>It's your turn!</b></color> <size=95%>Roll the dice!</size>";
            }

            var activePlayer = gameManager.GetActivePlayer(runner);
            if (activePlayer.IsRealPlayer)
            {
                var activeData = gameManager.GetPlayerData(activePlayer, runner);
                string name = activeData != null ? (string)activeData.Nick : $"Player {activePlayer.PlayerId}";
                return $"<color=#FFFF00><b>Waiting for {name} to roll</b></color>";
            }

            return gameManager.State == Networking.Managers.GameManager.GameState.RollOrder
                ? "<color=#FFFF00><b>Waiting for players to roll</b></color>"
                : string.Empty;
        }

        private static string BuildMinigameTileText(int boardPosition, Networking.Services.SliceTileType tileType)
        {
            int displayTileNumber = boardPosition + 1;
            return $"Current tile: {displayTileNumber} ({tileType})";
        }

        private static string BuildMinigameWaterText(int waterAmount)
        {
            return $"{waterAmount}";
        }

        private void RefreshRivalPlayersUI(NetworkRunner runner)
        {
            if (_rivalPlayersContainer == null || _rivalPlayerPrefab == null)
            {
                return;
            }

            var gameManager = Networking.Managers.GameManager.Instance;
            if (gameManager == null)
            {
                return;
            }

            var rivalPlayers = runner.ActivePlayers
                .Where(player => player != runner.LocalPlayer)
                .Select(player => new
                {
                    Player = player,
                    Data = gameManager.GetPlayerData(player, runner)
                })
                .Where(entry => entry.Data != null)
                .OrderBy(entry => entry.Data.TurnOrder)
                .ThenBy(entry => entry.Player.PlayerId)
                .ToList();

            var currentPlayers = new HashSet<PlayerRef>(rivalPlayers.Select(entry => entry.Player));
            var stalePlayers = _rivalPlayerCards.Keys.Where(player => !currentPlayers.Contains(player)).ToList();
            foreach (var player in stalePlayers)
            {
                if (_rivalPlayerCards.TryGetValue(player, out var staleView) && staleView.Root != null)
                {
                    Destroy(staleView.Root);
                }

                _rivalPlayerCards.Remove(player);
            }

            for (int i = 0; i < rivalPlayers.Count; i++)
            {
                var rival = rivalPlayers[i];
                var cardView = GetOrCreateRivalPlayerCard(rival.Player);
                if (cardView == null)
                {
                    continue;
                }

                var characterConfig = Networking.Managers.CharacterDatabase.Instance != null
                    ? Networking.Managers.CharacterDatabase.Instance.GetCharacterById(rival.Data.SelectedCharacterId)
                    : null;

                if (cardView.CharacterImage != null)
                {
                    cardView.CharacterImage.sprite = characterConfig != null ? characterConfig.CharacterSprite : null;
                    cardView.CharacterImage.enabled = cardView.CharacterImage.sprite != null;
                }

                if (cardView.WaterText != null)
                {
                    cardView.WaterText.text = $"{rival.Data.WaterAmount}";
                }

                if (cardView.RivalNameText != null)
                {
                    cardView.RivalNameText.text = rival.Data.Nick.ToString();
                }

                cardView.Root.transform.SetSiblingIndex(i);
            }
        }

        private RivalPlayerCardView GetOrCreateRivalPlayerCard(PlayerRef player)
        {
            if (_rivalPlayerCards.TryGetValue(player, out var existingView) && existingView.Root != null)
            {
                return existingView;
            }

            if (_rivalPlayersContainer == null || _rivalPlayerPrefab == null)
            {
                return null;
            }

            var instance = Instantiate(_rivalPlayerPrefab, _rivalPlayersContainer);
            instance.name = $"RivalPlayer_{player.PlayerId}";

            var createdView = new RivalPlayerCardView
            {
                Root = instance,
                CharacterImage = FindRequiredComponent<Image>(instance.transform, "CharacterImage"),
                WaterText = FindRequiredComponent<TextMeshProUGUI>(instance.transform, "WaterText"),
                RivalNameText = FindRequiredComponent<TextMeshProUGUI>(instance.transform, "RivalName")
            };

            _rivalPlayerCards[player] = createdView;
            return createdView;
        }

        private static T FindRequiredComponent<T>(Transform root, string childName) where T : Component
        {
            var child = FindChildByName(root, childName);
            if (child == null)
            {
                Debug.LogWarning($"[LobbyCanvas] Could not find child '{childName}' under '{root.name}'.");
                return null;
            }

            var component = child.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[LobbyCanvas] Child '{childName}' under '{root.name}' is missing component '{typeof(T).Name}'.");
            }

            return component;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        //Called from button
        public void SetGameMode(int gameMode)
        {
            if (Networking.Managers.GameManager.Instance != null)
            {
                Networking.Managers.GameManager.Instance.SetGameState(Networking.Managers.GameManager.GameState.Lobby);
            }

            _gameMode = (GameMode)gameMode;
            if (_modeButtons != null)
            {
                _modeButtons.SetActive(false);
            }

            if (_roomActionText != null)
            {
                _roomActionText.text = _gameMode == GameMode.Client ? "Entrar a Sala" : "Crear Sala";
            }

            if (_roomActionButtonText != null)
            {
                _roomActionButtonText.text = _gameMode == GameMode.Client ? "Entrar" : "Crear";
            }

            var roomInputsRoot = GetRoomInputsRoot();
            if (roomInputsRoot != null)
            {
                roomInputsRoot.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[LobbyCanvas] Room inputs panel is not assigned and nickname parent could not be resolved.");
            }
        }

        //Called from button
        public void StartLauncher()
        {
            Launcher = FindFirstObjectByType<Networking.Managers.GameLauncher>();
            Nickname = _nickname.text;
            PlayerPrefs.SetString("Nick", Nickname);
            Launcher.Launch(_gameMode, _room.text);

            var roomInputsRoot = GetRoomInputsRoot();
            if (roomInputsRoot != null)
            {
                roomInputsRoot.SetActive(false);
            }
        }

        //Called from button
        public void ExitGame()
        {
            Networking.Managers.GameManager.Instance.ExitGame();
        }

        //Called from button
        public void LeaveLobby()
        {
            _ = LeaveLobbyAsync();
        }

        //Called from button
        public void StartGame()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;

            // Only host can start the game
            if (!runner.IsServer)
            {
                Debug.LogWarning("[LobbyCanvas] Only the host can start the game.");
                return;
            }

            // Close lobby to new players
            runner.SessionInfo.IsOpen = false;
            runner.SessionInfo.IsVisible = false;

            Debug.Log("[LobbyCanvas] Host initiated character selection phase.");

            // Spawn CharacterSelectionManager (NetworkBehaviour) on host
            // All clients will automatically know about this spawned object
            var selectionManager = runner.Spawn(
                _characterSelectionManagerPrefab,
                inputAuthority: runner.LocalPlayer
            ).GetComponent<Networking.Managers.CharacterSelectionManager>();

            if (selectionManager == null)
            {
                Debug.LogError("[LobbyCanvas] Failed to spawn CharacterSelectionManager.");
                return;
            }

            // Hide lobby panel immediately on host
            _lobbyPanel.SetActive(false);

            // Initialize character selection panel with the manager
            if (_characterSelectionPanel != null)
            {
                _characterSelectionPanel.Initialize(selectionManager, runner);

                // Subscribe to completion event
                if (_characterSelectionPanel.CompleteEvent != null)
                {
                    _characterSelectionPanel.CompleteEvent.RegisterResponse(OnCharacterSelectionComplete);
                }
            }
            else
            {
                Debug.LogError("[LobbyCanvas] CharacterSelectionPanel not assigned in inspector.");
            }

            // Non-host clients will auto-detect the spawned manager and their panels
            // will auto-initialize when they become active (see CharacterSelectionPanel.cs)
        }

        /// <summary>
        /// Called when character selection phase completes.
        /// Shows game lobby panel with selected characters visible.
        /// Then triggers turn order initialization phase.
        /// Called on ALL clients via the global OnCharacterSelectionCompleteEvent.
        /// </summary>
        private void OnCharacterSelectionComplete(PlayerRef player, NetworkRunner runner)
        {
            bool isHost = runner != null && runner.IsServer;
            Debug.Log($"[LobbyCanvas] ◆◆◆ OnCharacterSelectionComplete fired on {(isHost ? "HOST" : "CLIENT")} ◆◆◆");

            // Hide character selection panel
            if (_characterSelectionPanel != null)
            {
                _characterSelectionPanel.Hide();
                Debug.Log("[LobbyCanvas] Character selection panel hidden.");
            }

            // Show game lobby panel
            if (_gameLobbyPanel != null)
            {
                _gameLobbyPanel.SetActive(true);
                UpdateGameLobbyList(player, runner ?? FindFirstObjectByType<NetworkRunner>());
                Debug.Log("[LobbyCanvas] Game lobby panel shown with selected characters.");
            }
            else
            {
                Debug.LogError("[LobbyCanvas] Game lobby panel not assigned in inspector!");
            }

            // Start turn order initialization (Roll for turn order - Round 1 only)
            Debug.Log($"[LobbyCanvas] About to call StartTurnOrderPhase on {(isHost ? "HOST" : "CLIENT")}");
            StartTurnOrderPhase(runner);
        }

        /// <summary>
        /// Initiated turn order initialization phase (SUMAK Round 1: ROLL_ORDER state).
        /// Players will roll D10 to determine turn order for all subsequent rounds.
        /// </summary>
        private void StartTurnOrderPhase(NetworkRunner runner)
        {
            bool isHost = runner != null && runner.IsServer;
            Debug.Log($"[LobbyCanvas] StartTurnOrderPhase called on {(isHost ? "HOST" : "CLIENT")}");

            // Update game state
            var gameManager = Networking.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                if (isHost)
                {
                    gameManager.InitializePreRoundPlayerState(runner);
                }

                gameManager.SetGameState(Networking.Managers.GameManager.GameState.RollOrder);
                Debug.Log($"[LobbyCanvas] GameState set to RollOrder on {(isHost ? "HOST" : "CLIENT")}");
            }

            // Find or assign TurnOrderPanel if not already assigned
            if (_turnOrderPanel == null)
            {
                Debug.LogWarning("[LobbyCanvas] TurnOrderPanel not assigned. Attempting to find it...");
                _turnOrderPanel = FindFirstObjectByType<TurnOrderPanel>();
                if (_turnOrderPanel != null)
                {
                    Debug.Log("[LobbyCanvas] TurnOrderPanel found and assigned!");
                }
            }

            // Start the TurnOrderPanel
            Debug.Log($"[LobbyCanvas] Checking TurnOrderPanel reference: {(_turnOrderPanel != null ? "ASSIGNED" : "NULL")}");

            if (_turnOrderPanel != null)
            {
                Debug.Log($"[LobbyCanvas] TurnOrderPanel found. Calling StartTurnOrderPhase on {(isHost ? "HOST" : "CLIENT")}");
                _turnOrderPanel.StartTurnOrderPhase();
                Debug.Log($"[LobbyCanvas] ✓ Turn order phase started on {(isHost ? "HOST" : "CLIENT")}");
            }
            else
            {
                Debug.LogError($"[LobbyCanvas] ✗✗✗ TurnOrderPanel reference is NULL on {(isHost ? "HOST" : "CLIENT")}! Panel will NOT be shown. Make sure it's in the scene with the TurnOrderPanel component!");
            }
        }

        /// <summary>
        /// Update game lobby with only the local player's selected character sprite.
        /// </summary>
        public void UpdateGameLobbyList(PlayerRef playerRef, NetworkRunner runner)
        {
            Sprite selectedCharacterSprite = null;

            if (runner != null)
            {
                var localPlayerData = Networking.Managers.GameManager.Instance.GetPlayerData(runner.LocalPlayer, runner);

                if (localPlayerData != null)
                {
                    Debug.Log($"[LobbyCanvas] Local player SelectedCharacterId: {localPlayerData.SelectedCharacterId}");

                    if (localPlayerData.SelectedCharacterId > 0)
                    {
                        var charConfig = Networking.Managers.CharacterDatabase.Instance.GetCharacterById(localPlayerData.SelectedCharacterId);
                        if (charConfig != null)
                        {
                            selectedCharacterSprite = charConfig.CharacterSprite;
                            Debug.Log($"[LobbyCanvas] Character sprite found for: {charConfig.CharacterName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[LobbyCanvas] Character ID {localPlayerData.SelectedCharacterId} not found in database!");
                        }
                    }
                    else
                    {
                        // Normal during early sync before character selection completes.
                    }
                }
                else
                {
                    Debug.LogError("[LobbyCanvas] Local player data is null!");
                }
            }

            if (_gameLobbyCharacterImage != null)
            {
                _gameLobbyCharacterImage.sprite = selectedCharacterSprite;
                _gameLobbyCharacterImage.enabled = selectedCharacterSprite != null;
            }

            if (runner != null)
            {
                RefreshRivalPlayersUI(runner);
            }

            // Enable dice button when game lobby is ready
            EnableDiceButton();
        }

        /// <summary>
        /// Called from "Load Game" button (host only).
        /// Sends all players to the minigame scene.
        /// </summary>
        public void LoadGame()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;

            Debug.Log("[LobbyCanvas] LoadGame() called!");

            if (runner == null)
            {
                Debug.LogError("[LobbyCanvas] NetworkRunner is NULL! Cannot load game.");
                return;
            }

            if (!runner.IsServer)
            {
                Debug.LogWarning("[LobbyCanvas] Only the host can load the game. IsServer: " + runner.IsServer);
                return;
            }

            Debug.Log("[LobbyCanvas] Host initiating game load. Broadcasting RPC to all players to load minigame scene.");

            // Call RPC on all player data objects to load minigame on all clients
            foreach (var player in runner.ActivePlayers)
            {
                var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                if (playerData != null)
                {
                    Debug.Log($"[LobbyCanvas] Calling RPC_LoadMinigameScene for player {player}");
                    playerData.RPC_LoadMinigameScene();
                }
                else
                {
                    Debug.LogWarning($"[LobbyCanvas] PlayerData is NULL for player {player}. Cannot call RPC.");
                }
            }
        }

        /// <summary>
        /// Called when game load event fires on all clients.
        /// Loads the minigame scene for this client.
        /// </summary>
        private void OnGameLoad(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("[LobbyCanvas] OnGameLoad event callback received. Loading minigame scene...");
            LoadMinigameScene();
        }

        /// <summary>
        /// Loads the minigame scene.
        /// Update the scene name to match your actual minigame scene.
        /// </summary>
        private void LoadMinigameScene()
        {
            // Change "Minigame" to your actual scene name
            string sceneToLoad = "Minigame";

            Debug.Log($"[LobbyCanvas] Attempting to load scene: '{sceneToLoad}'");

            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError("[LobbyCanvas] Minigame scene name is empty!");
                return;
            }

            try
            {
                Debug.Log($"[LobbyCanvas] Loading scene: {sceneToLoad}");
                SceneManager.LoadScene(sceneToLoad);
                Debug.Log($"[LobbyCanvas] Scene load initiated for: {sceneToLoad}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LobbyCanvas] Error loading scene '{sceneToLoad}': {ex.Message}");
            }
        }

        /// <summary>
        /// Called from "Open Vuforia" button.
        /// Switches from GameLobbyPanel to VuforiaPanel and activates AR camera.
        /// </summary>
        public void OpenVuforiaPanel()
        {
            Debug.Log("[LobbyCanvas] Opening Vuforia panel...");

            // Hide GameLobbyPanel
            if (_gameLobbyPanel != null)
            {
                _gameLobbyPanel.SetActive(false);
            }

            // Hide BackgroundImage
            if (_backgroundImage != null)
            {
                _backgroundImage.SetActive(false);
            }

            // Activate Vuforia AR Camera
            if (_vuforiaARCamera != null)
            {
                _vuforiaARCamera.SetActive(true);
                Debug.Log("[LobbyCanvas] ✓ Vuforia AR Camera activated");
            }
            else
            {
                Debug.LogWarning("[LobbyCanvas] VuforiaARCamera not assigned in inspector");
            }

            // Show VuforiaPanel
            if (_vuforiaPanel != null)
            {
                _vuforiaPanel.SetActive(true);
                Debug.Log("[LobbyCanvas] ✓ Vuforia panel opened");
            }
            else
            {
                Debug.LogError("[LobbyCanvas] VuforiaPanel not assigned in inspector!");
            }
        }

        /// <summary>
        /// Called to return from VuforiaPanel back to GameLobbyPanel and deactivate AR camera.
        /// </summary>
        public void CloseVuforiaPanel()
        {
            Debug.Log("[LobbyCanvas] Closing Vuforia panel...");

            // Deactivate Vuforia AR Camera
            if (_vuforiaARCamera != null)
            {
                _vuforiaARCamera.SetActive(false);
                Debug.Log("[LobbyCanvas] ✓ Vuforia AR Camera deactivated");
            }

            // Hide VuforiaPanel
            if (_vuforiaPanel != null)
            {
                _vuforiaPanel.SetActive(false);
            }

            // Show BackgroundImage
            if (_backgroundImage != null)
            {
                _backgroundImage.SetActive(true);
            }

            // Show GameLobbyPanel
            if (_gameLobbyPanel != null)
            {
                _gameLobbyPanel.SetActive(true);
                Debug.Log("[LobbyCanvas] ✓ Returned to Game Lobby panel");
            }
        }

        private async Task LeaveLobbyAsync()
        {
            if (Networking.Services.FusionNetworkService.LocalRunner.IsServer)
            {
                CloseLobby();
            }
            await Networking.Services.FusionNetworkService.LocalRunner?.Shutdown();
        }

        public void CloseLobby()
        {
            foreach (var player in Networking.Services.FusionNetworkService.LocalRunner.ActivePlayers)
            {
                if (player != Networking.Services.FusionNetworkService.LocalRunner.LocalPlayer)
                    Networking.Services.FusionNetworkService.LocalRunner.Disconnect(player);
            }
        }

        private void ResetCanvas(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("[LobbyCanvas] Canvas reset");

            _initPanel.SetActive(true);
            _modeButtons.SetActive(true);
            var roomInputsRoot = GetRoomInputsRoot();
            if (roomInputsRoot != null)
            {
                roomInputsRoot.SetActive(false);
            }
            if (_roomActionText != null)
            {
                _roomActionText.text = string.Empty;
            }
            if (_roomActionButtonText != null)
            {
                _roomActionButtonText.text = string.Empty;
            }
            _lobbyPanel.SetActive(false);
            _gameLobbyPanel.SetActive(false);
            if (_minigameReadyPanel != null)
            {
                _minigameReadyPanel.SetActive(false);
            }
            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(false);
            }
            _victoryShown = false;
            InitializeMinigameReadyPlayerStatus();
            if (_vuforiaPanel != null)
            {
                _vuforiaPanel.SetActive(false);
            }
            if (_vuforiaARCamera != null)
            {
                _vuforiaARCamera.SetActive(false);
            }
            if (_backgroundImage != null)
            {
                _backgroundImage.SetActive(true);
            }
            _startButton.gameObject.SetActive(runner.IsServer);
        }

        public void ShowLobbyCanvas(PlayerRef player, NetworkRunner runner)
        {
            _initPanel.SetActive(false);
            _lobbyPanel.SetActive(true);
        }

        private GameObject GetRoomInputsRoot()
        {
            if (_roomInputsPanel != null)
            {
                return _roomInputsPanel;
            }

            if (_nickname != null && _nickname.transform.parent != null)
            {
                return _nickname.transform.parent.gameObject;
            }

            return null;
        }

        public void UpdateLobbyList(PlayerRef playerRef, NetworkRunner runner)
        {
            _startButton.gameObject.SetActive(runner.IsServer);
            string players = default;
            string isLocal;
            foreach (var player in runner.ActivePlayers)
            {
                isLocal = player == runner.LocalPlayer ? " (You)" : string.Empty;
                var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                if (playerData != null)
                {
                    players += playerData.Nick + isLocal + " \n";
                }
            }
            _lobbyPlayerText.text = players;
            _lobbyRoomName.text = $"Room: {runner.SessionInfo.Name}";
        }

        /// <summary>
        /// Called when the Roll Dice button is clicked.
        /// Delegates to DiceUI to handle the rolling animation and networked roll.
        /// </summary>
        private void OnRollDiceClicked()
        {
            _diceRolling = true;

            if (_rollDiceButton != null)
                _rollDiceButton.interactable = false;

            var diceUI = FindFirstObjectByType<DiceUI>();
            if (diceUI != null)
            {
                diceUI.StartDiceRoll();
            }
            else
            {
                Debug.LogError("[LobbyCanvas] DiceUI not found!");
                _diceRolling = false;
            }
        }

        /// <summary>
        /// Enable the dice roll button only if it is the local player's active turn.
        /// Called when player data is spawned and network is ready.
        /// </summary>
        public void EnableDiceButton()
        {
            if (_rollDiceButton == null) return;

            var runner = Networking.Services.FusionNetworkService.LocalRunner
                         ?? FindFirstObjectByType<NetworkRunner>();
            if (runner == null)
            {
                _rollDiceButton.interactable = false;
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            bool isMyTurn = localData != null && localData.IsActiveTurn;
            _rollDiceButton.interactable = isMyTurn;
            Debug.Log($"[LobbyCanvas] EnableDiceButton: isMyTurn={isMyTurn}");
        }

        /// <summary>
        /// Display the final dice result in the UI.
        /// Called by DiceUI after animation completes.
        /// </summary>
        public void DisplayDiceResult(int result)
        {
            if (_diceResultText != null)
            {
                _diceResultText.text = $"<b>{result}</b>";
                Debug.Log($"[LobbyCanvas] Displayed dice result: {result}");
            }
            else
            {
                Debug.LogWarning("[LobbyCanvas] Dice result text not assigned!");
            }
        }

        /// <summary>
        /// Clear the dice result from the UI.
        /// </summary>
        public void ClearDiceResult()
        {
            if (_diceResultText != null)
            {
                _diceResultText.text = string.Empty;
            }
        }

        /// <summary>
        /// Display message when room is full (6/6 players).
        /// Called by FusionNetworkService when join is denied.
        /// </summary>
        public void ShowRoomFullMessage()
        {
            Debug.LogWarning("[LobbyCanvas] Room is full! Maximum 6 players reached. New join attempts will be rejected.");

            // Optional: Show UI feedback if you have a message panel
            // Example: _lobbyStatusText.text = "Room Full (6/6)";
        }

        /// <summary>
        /// Display warning when player count is near max.
        /// Called when 5 players are in the room (1 slot remaining).
        /// </summary>
        public void ShowRoomAlmostFullMessage()
        {
            Debug.LogWarning("[LobbyCanvas] Room is almost full (5/6 players). Only 1 slot remaining!");

            // Optional: Show UI feedback
            // Example: _lobbyStatusText.text = "Room Almost Full (5/6)";
        }
    }
}
