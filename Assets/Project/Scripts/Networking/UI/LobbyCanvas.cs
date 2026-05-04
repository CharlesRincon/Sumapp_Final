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
using UnityEngine.InputSystem;

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
        [SerializeField] private TextMeshProUGUI _tileText;
        [SerializeField] private TextMeshProUGUI _waterText;
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private Button _rollDiceButton;
        [SerializeField] private Button _openVuforiaButton;
        [SerializeField] private TextMeshProUGUI _roundStatusText;
        [SerializeField] private TextMeshProUGUI _turnStatusText;
        [SerializeField] private Image _basinHealthImage;
        [SerializeField] private Image _basinHealthRadialFill;
        [SerializeField] private Transform _rivalPlayersContainer;
        [SerializeField] private GameObject _rivalPlayerPrefab;
        [SerializeField] private Button _toggleActiveProjectsButton;
        [SerializeField] private GameObject _activeProjectsPanel;
        [SerializeField] private Transform _projectContainer;
        [SerializeField] private GameObject _projectEntryPrefab;
        [Space]
        [SerializeField] private GameObject _minigameReadyPanel;
        [SerializeField] private TextMeshProUGUI _minigameReadyText;
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
        [SerializeField] private GameObject _projectDecisionPanel;
        [SerializeField] private TextMeshProUGUI _projectDecisionTitleText;
        [SerializeField] private TextMeshProUGUI _projectDecisionBodyText;
        [SerializeField] private Button _projectBuyButton;
        [SerializeField] private Button _projectDeclineButton;
        [SerializeField] private GameObject _backgroundImage;
        [SerializeField] private GameObject _vuforiaARCamera;
        [Space]
        [SerializeField] private GameObject _modeButtons;
        [SerializeField] private GameObject _roomInputsPanel;
        [SerializeField] private TextMeshProUGUI _roomActionText;
        [SerializeField] private TextMeshProUGUI _roomActionButtonText;
        [SerializeField] private TMP_InputField _nickname;
        [SerializeField] private TMP_InputField _room;
        [Space]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private RectTransform _loadingPanelImage;
        [Space]
        [SerializeField] private GameObject _openingPanel;
        [SerializeField] private RectTransform _waveImage1;
        [SerializeField] private RectTransform _waveImage2;
        [SerializeField] private RectTransform _waveImage3;
        [SerializeField] private RectTransform _openingWaterdrop;
        [SerializeField] private GameObject _openingTitleObject;
        [SerializeField] private GameObject _tapToContinueObject;

        private Coroutine _wave1Coroutine;
        private Coroutine _wave2Coroutine;
        private Coroutine _wave3Coroutine;
        private Coroutine _tapPulseCoroutine;
        private Vector2 _openingWaterdropStartPos;
        private Vector3 _openingWaterdropStartScale;
        private float _openingWaterdropStartZ;
        private CanvasGroup _openingWaterdropCanvasGroup;
        private RectTransform _openingTitle;
        private CanvasGroup _openingTitleCanvasGroup;
        private Vector2 _openingTitleStartPos;
        private Vector3 _openingTitleStartScale;

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
        private bool _projectFlowVisible;

        private sealed class RivalPlayerCardView
        {
            public GameObject Root;
            public Image CharacterImage;
            public TextMeshProUGUI WaterText;
            public TextMeshProUGUI MoneyText;
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

            if (_toggleActiveProjectsButton != null)
            {
                _toggleActiveProjectsButton.onClick.AddListener(OnToggleActiveProjectsClicked);
            }

            if (_minigameReadyButton != null)
            {
                _minigameReadyButton.onClick.AddListener(OnMinigameReadyClicked);
            }

            if (_victoryReturnButton != null)
            {
                _victoryReturnButton.onClick.AddListener(OnVictoryReturnClicked);
            }

            if (_projectBuyButton != null)
            {
                _projectBuyButton.onClick.AddListener(OnProjectBuyClicked);
            }

            if (_projectDeclineButton != null)
            {
                _projectDeclineButton.onClick.AddListener(OnProjectDeclineClicked);
            }

            if (_minigameReadyPanel != null)
            {
                _minigameReadyPanel.SetActive(false);
            }

            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(false);
            }

            if (_projectDecisionPanel != null)
            {
                _projectDecisionPanel.SetActive(false);
            }

            if (_activeProjectsPanel != null)
            {
                _activeProjectsPanel.SetActive(false);
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

            if (_toggleActiveProjectsButton != null)
            {
                _toggleActiveProjectsButton.onClick.RemoveListener(OnToggleActiveProjectsClicked);
            }

            if (_minigameReadyButton != null)
            {
                _minigameReadyButton.onClick.RemoveListener(OnMinigameReadyClicked);
            }

            if (_victoryReturnButton != null)
            {
                _victoryReturnButton.onClick.RemoveListener(OnVictoryReturnClicked);
            }

            if (_projectBuyButton != null)
            {
                _projectBuyButton.onClick.RemoveListener(OnProjectBuyClicked);
            }

            if (_projectDeclineButton != null)
            {
                _projectDeclineButton.onClick.RemoveListener(OnProjectDeclineClicked);
            }

            StopOpeningPanelAnimations();
        }

        private void Start()
        {
            Debug.Log("[LobbyCanvas] Start() called.");
            InitializeMinigameReadyPlayerStatus();

            if (_openingPanel != null)
            {
                if (_initPanel != null) _initPanel.SetActive(false);
                _openingPanel.SetActive(true);
                StartOpeningPanelAnimations();
            }
        }

        private bool _sessionRestored;
        private bool _diceRolling;

        private void Update()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;

            RefreshMinigameReadyPlayerStatus(runner);

            if (_openingPanel != null && _openingPanel.activeSelf)
            {
                bool tapped = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                              (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
                if (tapped)
                    CloseOpeningPanel();
            }

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
            if (runner != null)
            {
                bool gameplaySurfaceVisible = (_gameLobbyPanel != null && _gameLobbyPanel.activeSelf)
                    || (_vuforiaPanel != null && _vuforiaPanel.activeSelf);

                if (gameplaySurfaceVisible)
                {
                    RefreshTurnUI(runner);
                    RefreshMinigameReadyPanel(runner);
                    RefreshProjectDecisionUI(runner);
                    RefreshVictoryPanel(runner);
                }

                if (_gameLobbyPanel != null && _gameLobbyPanel.activeSelf)
                {
                    RefreshRivalPlayersUI(runner);
                    RefreshBasinHealthImage(runner);
                    RefreshActiveProjectsUI(runner);
                }
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
            bool hasRolledThisTurn = localData != null && localData.HasRolledThisTurn;

            // Button: enabled only when it's our turn and we haven't started rolling
            if (_rollDiceButton != null)
                _rollDiceButton.interactable = isMyTurn && !hasRolledThisTurn && !_diceRolling;

            bool awaitingProjectScan = localData != null && localData.IsAwaitingProjectScan;
            bool awaitingCardScan = localData != null && localData.IsAwaitingCardScan;
            if (_openVuforiaButton != null)
                _openVuforiaButton.interactable = isMyTurn && (awaitingProjectScan || awaitingCardScan) && !_diceRolling;

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
            bool showPanel = localData != null
                && localData.IsInMinigameReadyPhase
                && !localData.IsAwaitingProjectScan
                && !localData.IsAwaitingProjectDecision
                && !localData.IsAwaitingCardScan
                && !_diceRolling;

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
                _minigameReadyText.text = $"Esperando a los demás jugadores... {readyCount}/{Mathf.Max(1, totalPlayers)} listos";
            }

            if (_tileText != null)
            {
                var tileType = gameManager.GetTileTypeAtPosition(localData.BoardPosition);
                _tileText.text = BuildMinigameTileText(localData.BoardPosition, tileType);
            }

            if (_waterText != null)
            {
                _waterText.text = BuildMinigameWaterText(localData.WaterAmount);
            }

            if (_moneyText != null)
            {
                _moneyText.text = BuildMinigameMoneyText(localData.MoneyAmount);
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

            int moneyAmount = localData != null
                ? localData.MoneyAmount
                : gameManager != null ? gameManager.StartingMoney : 0;

            var tileType = gameManager != null
                ? gameManager.GetTileTypeAtPosition(boardPosition)
                : Networking.Services.SliceTileType.Start;

            if (_tileText != null)
            {
                _tileText.text = BuildMinigameTileText(boardPosition, tileType);
            }

            if (_waterText != null)
            {
                _waterText.text = BuildMinigameWaterText(waterAmount);
            }

            if (_moneyText != null)
            {
                _moneyText.text = BuildMinigameMoneyText(moneyAmount);
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

        private void RefreshProjectDecisionUI(NetworkRunner runner)
        {
            var gameManager = Networking.Managers.GameManager.Instance;
            var localData = gameManager?.GetPlayerData(runner.LocalPlayer, runner);

            bool isAwaitingProjectScan = localData != null && localData.IsAwaitingProjectScan;
            bool isAwaitingProjectDecision = localData != null && localData.IsAwaitingProjectDecision;
            bool isAwaitingCardScan = localData != null && localData.IsAwaitingCardScan;
            bool projectFlowActive = isAwaitingProjectScan || isAwaitingProjectDecision;

            // Track flow visibility for close-button handling only.
            _projectFlowVisible = projectFlowActive || isAwaitingCardScan;

            // When the scan has been completed (host pushed a pending project) and the
            // Vuforia panel is already open, show the decision panel on top of it.
            // We do NOT auto-open Vuforia here — the player opens it manually via the scan button.
            if (isAwaitingProjectDecision && _vuforiaPanel != null && _vuforiaPanel.activeSelf
                && _projectDecisionPanel != null && !_projectDecisionPanel.activeSelf)
            {
                _projectDecisionPanel.SetActive(true);
            }

            // Hide decision panel as soon as the project flow ends.
            if (!projectFlowActive && _projectDecisionPanel != null && _projectDecisionPanel.activeSelf)
            {
                _projectDecisionPanel.SetActive(false);
                if (_vuforiaPanel != null && _vuforiaPanel.activeSelf)
                {
                    CloseVuforiaPanel();
                }
            }

            // Auto-close Vuforia when card scan is resolved (host cleared IsAwaitingCardScan).
            if (!isAwaitingCardScan && _vuforiaPanel != null && _vuforiaPanel.activeSelf && !projectFlowActive)
            {
                CloseVuforiaPanel();
            }

            if (!isAwaitingProjectDecision)
            {
                if (_projectDecisionPanel != null)
                {
                    _projectDecisionPanel.SetActive(false);
                }
                return;
            }

            if (_projectDecisionPanel != null && !_projectDecisionPanel.activeSelf)
            {
                _projectDecisionPanel.SetActive(true);
            }

            string projectName = localData.PendingProjectName.ToString();
            if (string.IsNullOrWhiteSpace(projectName))
            {
                projectName = $"Project {localData.PendingProjectId}";
            }

            if (_projectDecisionTitleText != null)
            {
                _projectDecisionTitleText.text = projectName;
            }

            if (_projectDecisionBodyText != null)
            {
                var zone = (Networking.Models.ColombiaZone)localData.PendingProjectZone;
                _projectDecisionBodyText.text =
                    $"Zone: {zone}\n" +
                    $"Price: {localData.PendingProjectPrice}\n" +
                    $"Water / round: {localData.PendingProjectWaterIncome}\n" +
                    $"Money / round: {localData.PendingProjectMoneyIncome}";
            }

            if (_projectBuyButton != null)
            {
                _projectBuyButton.interactable = localData.MoneyAmount >= localData.PendingProjectPrice;
            }

            if (_projectDeclineButton != null)
            {
                _projectDeclineButton.interactable = true;
            }
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
            if (_basinHealthImage == null && _basinHealthRadialFill == null) return;

            var gm = Networking.Managers.GameManager.Instance;
            if (gm == null) return;

            var localData = gm.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null) return;

            int basinHealth = localData.BasinHealth;
            if (localData.CurrentRound <= 0 && basinHealth <= 0)
            {
                basinHealth = gm.StartingBasinHealth;
            }

            float normalizedHealth = (float)basinHealth / Mathf.Max(1, gm.StartingBasinHealth);
            float percentage = normalizedHealth * 100f;

            Color color;
            if (percentage > 80f)
                color = Color.green;
            else if (percentage > 20f)
                color = Color.yellow;
            else
                color = Color.red;

            if (_basinHealthImage != null)
            {
                _basinHealthImage.color = color;
            }

            if (_basinHealthRadialFill != null)
            {
                _basinHealthRadialFill.fillAmount = Mathf.Clamp01(normalizedHealth);
                _basinHealthRadialFill.color = color;
            }
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

        private void OnToggleActiveProjectsClicked()
        {
            if (_activeProjectsPanel == null)
            {
                Debug.LogWarning("[LobbyCanvas] Active projects panel is not assigned.");
                return;
            }

            _activeProjectsPanel.SetActive(!_activeProjectsPanel.activeSelf);
        }

        private void RefreshActiveProjectsUI(NetworkRunner runner)
        {
            if (_projectContainer == null || _projectEntryPrefab == null)
            {
                return;
            }

            var gameManager = Networking.Managers.GameManager.Instance;
            if (gameManager == null)
            {
                return;
            }

            var localData = gameManager.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null)
            {
                return;
            }

            var projectDatabase = Networking.Managers.GameManager.Instance != null
                ? gameManager.ProjectDatabase
                : null;

            // Collect occupied slots
            var slots = new (int id, int zone)[]
            {
                (localData.OwnedProjectSlot0Id, localData.OwnedProjectSlot0Zone),
                (localData.OwnedProjectSlot1Id, localData.OwnedProjectSlot1Zone),
                (localData.OwnedProjectSlot2Id, localData.OwnedProjectSlot2Zone),
            };

            // Ensure we have exactly one child entry per occupied slot
            int slotCount = 0;
            foreach (var slot in slots)
            {
                if (slot.id > 0) slotCount++;
            }

            // Resize children: add missing, remove extras
            while (_projectContainer.childCount < slotCount)
            {
                Instantiate(_projectEntryPrefab, _projectContainer);
            }

            while (_projectContainer.childCount > slotCount)
            {
                DestroyImmediate(_projectContainer.GetChild(_projectContainer.childCount - 1).gameObject);
            }

            int entryIndex = 0;
            foreach (var slot in slots)
            {
                if (slot.id <= 0) continue;

                var entryTransform = _projectContainer.GetChild(entryIndex);
                var label = entryTransform.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label == null)
                {
                    entryIndex++;
                    continue;
                }

                string name = $"Project {slot.id}";
                int water = 0;
                int money = 0;

                if (projectDatabase != null && projectDatabase.TryGetProject(slot.id, out var projectDef) && projectDef != null)
                {
                    name = projectDef.DisplayName;
                    var (w, m) = projectDef.GetIncomeForZone((Networking.Models.ColombiaZone)slot.zone);
                    water = w;
                    money = m;
                }

                label.text = $"{name}\n+{water} water/round  +{money} money/round";
                entryIndex++;
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

        private static string BuildMinigameMoneyText(int moneyAmount)
        {
            return $"{moneyAmount}";
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

                if (cardView.MoneyText != null)
                {
                    cardView.MoneyText.text = $"{rival.Data.MoneyAmount}";
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
                MoneyText = FindRequiredComponent<TextMeshProUGUI>(instance.transform, "MoneyText"),
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

            if (_gameMode == GameMode.Host && _loadingPanel != null)
            {
                if (_initPanel != null)
                    _initPanel.SetActive(false);
                _loadingPanel.SetActive(true);
                StartLoadingImageAnimation();
            }

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

            // If the player closed the AR panel while a project scan was pending, treat it as a decline.
            if (_projectFlowVisible)
            {
                var runner = Networking.Services.FusionNetworkService.LocalRunner;
                if (runner != null)
                {
                    var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
                    if (localData != null && (localData.IsAwaitingProjectScan || localData.IsAwaitingProjectDecision))
                    {
                        localData.RPC_RequestDeclinePendingProject();
                    }
                    else if (localData != null && localData.IsAwaitingCardScan)
                    {
                        // Closing AR while awaiting a card scan: skip the card and advance the turn.
                        localData.RPC_RequestSkipCardScan();
                    }
                }
            }
        }

        private void OnProjectBuyClicked()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (runner == null)
            {
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || !localData.IsAwaitingProjectDecision)
            {
                return;
            }

            localData.RPC_RequestBuyPendingProject();
        }

        private void OnProjectDeclineClicked()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (runner == null)
            {
                return;
            }

            var localData = Networking.Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || (!localData.IsAwaitingProjectDecision && !localData.IsAwaitingProjectScan))
            {
                return;
            }

            localData.RPC_RequestDeclinePendingProject();
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
            if (_projectDecisionPanel != null)
            {
                _projectDecisionPanel.SetActive(false);
            }
            _projectFlowVisible = false;
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
            if (_loadingPanel != null && _loadingPanel.activeSelf)
            {
                LeanTween.cancel(_loadingPanel);
                if (_loadingPanelImage != null)
                    LeanTween.cancel(_loadingPanelImage.gameObject);
                _loadingPanel.SetActive(false);
            }

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

        private void StartLoadingImageAnimation()
        {
            if (_loadingPanelImage == null) return;

            Vector3 startPos = _loadingPanelImage.anchoredPosition3D;
            float floatAmount = 18f;
            float duration   = 1.6f;

            LeanTween.moveLocalY(_loadingPanelImage.gameObject, startPos.y + floatAmount, duration)
                .setEase(LeanTweenType.easeInOutSine)
                .setLoopPingPong();
        }

        private void StartOpeningPanelAnimations()
        {
            if (_waveImage1 != null)
                _wave1Coroutine = StartCoroutine(AnimateWave(_waveImage1, 14f, 1.1f, 0f));
            if (_waveImage2 != null)
                _wave2Coroutine = StartCoroutine(AnimateWave(_waveImage2, 20f, 0.85f, 1.2f));
            if (_waveImage3 != null)
                _wave3Coroutine = StartCoroutine(AnimateWave(_waveImage3, 10f, 1.4f, 2.5f));
            if (_tapToContinueObject != null)
                _tapPulseCoroutine = StartCoroutine(PulseTapToContinue(_tapToContinueObject));

            StartOpeningPanelMainTweens();
        }

        private void StartOpeningPanelMainTweens()
        {
            if (_openingWaterdrop != null)
            {
                _openingWaterdropStartPos = _openingWaterdrop.anchoredPosition;
                _openingWaterdropStartScale = _openingWaterdrop.localScale;
                _openingWaterdropStartZ = _openingWaterdrop.localEulerAngles.z;

                if (_openingWaterdropCanvasGroup == null)
                {
                    _openingWaterdropCanvasGroup = _openingWaterdrop.GetComponent<CanvasGroup>();
                    if (_openingWaterdropCanvasGroup == null)
                        _openingWaterdropCanvasGroup = _openingWaterdrop.gameObject.AddComponent<CanvasGroup>();
                }

                LeanTween.cancel(_openingWaterdrop.gameObject);
                LeanTween.cancel(_openingWaterdropCanvasGroup.gameObject);

                _openingWaterdropCanvasGroup.alpha = 0f;

                LeanTween.alphaCanvas(_openingWaterdropCanvasGroup, 1f, 0.9f)
                    .setEase(LeanTweenType.easeOutSine);

                LeanTween.moveLocalY(_openingWaterdrop.gameObject, _openingWaterdropStartPos.y + 14f, 1.8f)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setLoopPingPong();

                LeanTween.scale(_openingWaterdrop.gameObject, _openingWaterdropStartScale + new Vector3(0.04f, -0.02f, 0f), 1.6f)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setLoopPingPong();

                LeanTween.rotateZ(_openingWaterdrop.gameObject, _openingWaterdropStartZ + 3f, 2.2f)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setLoopPingPong();
            }

            if (_openingTitleObject != null && _openingTitle == null)
            {
                _openingTitle = _openingTitleObject.GetComponent<RectTransform>();
            }

            if (_openingTitle != null)
            {
                _openingTitleStartPos = _openingTitle.anchoredPosition;
                _openingTitleStartScale = _openingTitle.localScale;

                if (_openingTitleCanvasGroup == null)
                {
                    _openingTitleCanvasGroup = _openingTitle.GetComponent<CanvasGroup>();
                    if (_openingTitleCanvasGroup == null)
                        _openingTitleCanvasGroup = _openingTitle.gameObject.AddComponent<CanvasGroup>();
                }

                LeanTween.cancel(_openingTitle.gameObject);
                LeanTween.cancel(_openingTitleCanvasGroup.gameObject);

                _openingTitle.anchoredPosition = _openingTitleStartPos + new Vector2(0f, -20f);
                _openingTitle.localScale = _openingTitleStartScale;
                _openingTitleCanvasGroup.alpha = 0f;

                Vector2 introFrom = _openingTitle.anchoredPosition;
                LeanTween.value(_openingTitle.gameObject, 0f, 1f, 0.8f)
                    .setEase(LeanTweenType.easeOutCubic)
                    .setOnUpdate((float t) =>
                    {
                        if (_openingTitle != null)
                        {
                            _openingTitle.anchoredPosition = Vector2.LerpUnclamped(introFrom, _openingTitleStartPos, t);
                        }
                    });

                LeanTween.alphaCanvas(_openingTitleCanvasGroup, 1f, 0.8f)
                    .setEase(LeanTweenType.easeOutSine)
                    .setOnComplete(() =>
                    {
                        if (_openingTitle != null)
                        {
                            LeanTween.scale(_openingTitle.gameObject, _openingTitleStartScale * 1.1f, 2f)
                                .setEase(LeanTweenType.easeInOutSine)
                                .setLoopPingPong();
                        }
                    });
            }
        }

        private void StopOpeningPanelAnimations()
        {
            if (_wave1Coroutine != null) { StopCoroutine(_wave1Coroutine); _wave1Coroutine = null; }
            if (_wave2Coroutine != null) { StopCoroutine(_wave2Coroutine); _wave2Coroutine = null; }
            if (_wave3Coroutine != null) { StopCoroutine(_wave3Coroutine); _wave3Coroutine = null; }
            if (_tapPulseCoroutine != null) { StopCoroutine(_tapPulseCoroutine); _tapPulseCoroutine = null; }

            if (_openingWaterdrop != null)
            {
                LeanTween.cancel(_openingWaterdrop.gameObject);
                _openingWaterdrop.anchoredPosition = _openingWaterdropStartPos;
                _openingWaterdrop.localScale = _openingWaterdropStartScale;
                _openingWaterdrop.localRotation = Quaternion.Euler(0f, 0f, _openingWaterdropStartZ);
            }

            if (_openingWaterdropCanvasGroup != null)
            {
                LeanTween.cancel(_openingWaterdropCanvasGroup.gameObject);
                _openingWaterdropCanvasGroup.alpha = 1f;
            }

            if (_openingTitle != null)
            {
                LeanTween.cancel(_openingTitle.gameObject);
                _openingTitle.anchoredPosition = _openingTitleStartPos;
                _openingTitle.localScale = _openingTitleStartScale;
            }

            if (_openingTitleCanvasGroup != null)
            {
                LeanTween.cancel(_openingTitleCanvasGroup.gameObject);
                _openingTitleCanvasGroup.alpha = 1f;
            }
        }

        private void CloseOpeningPanel()
        {
            StopOpeningPanelAnimations();

            _openingPanel.SetActive(false);
            if (_initPanel != null) _initPanel.SetActive(true);
        }

        private IEnumerator AnimateWave(RectTransform wave, float amplitude, float speed, float phaseOffset)
        {
            Vector2 startPos = wave.anchoredPosition;
            float time = phaseOffset;
            while (true)
            {
                time += Time.deltaTime * speed;
                wave.anchoredPosition = new Vector2(startPos.x, startPos.y + Mathf.Sin(time) * amplitude);
                yield return null;
            }
        }

        private IEnumerator PulseTapToContinue(GameObject obj)
        {
            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group == null) group = obj.AddComponent<CanvasGroup>();
            while (true)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 2f;
                    group.alpha = Mathf.Lerp(0f, 1f, t);
                    yield return null;
                }
                yield return new WaitForSeconds(0.25f);
                t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.75f;
                    group.alpha = Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
                yield return new WaitForSeconds(0.15f);
            }
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
