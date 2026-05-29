using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
<<<<<<< HEAD
=======
using UnityEngine.SceneManagement;
>>>>>>> projects-logic
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;
<<<<<<< HEAD
=======
using System.Linq;
using UnityEngine.InputSystem;
>>>>>>> projects-logic

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
<<<<<<< HEAD
=======
        public FusionEvent OnCharacterSelectionCompleteEvent;
        public FusionEvent OnGameLoadEvent;
>>>>>>> projects-logic

        [Space]
        [SerializeField] private GameObject _initPanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private TextMeshProUGUI _lobbyPlayerText;
        [SerializeField] private TextMeshProUGUI _lobbyRoomName;
        [SerializeField] private Button _startButton;
        [Space]
<<<<<<< HEAD
        [SerializeField] private GameObject _modeButtons;
        [SerializeField] private TMP_InputField _nickname;
        [SerializeField] private TMP_InputField _room;
=======
        [SerializeField] private GameObject _gameLobbyPanel;
        [SerializeField] private Image _gameLobbyCharacterImage;
        [SerializeField] private TextMeshProUGUI _diceResultText;
        [SerializeField] private TextMeshProUGUI _tileText;
        [SerializeField] private GameObject _turnNotificationPanel;
        [SerializeField] private TextMeshProUGUI _turnNotificationText;
        private bool _shownTurnNotificationThisTurn = false;
        [SerializeField] private TextMeshProUGUI _waterText;
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private ResourceChangeUI _resourceChangeUI;
        [SerializeField] private Button _rollDiceButton;
[SerializeField] private Button _openVuforiaButton;
        [SerializeField] private Button _openModelButton;
        [SerializeField] private TextMeshProUGUI _roundStatusText;
        [SerializeField] private TextMeshProUGUI _turnStatusText;
        [SerializeField] private Image _basinHealthImage;
        [SerializeField] private Image _basinHealthRadialFill;
        [Header("Basin Health Sprites")]
        [SerializeField] private Sprite _basinHealthySprite;
        [SerializeField] private Sprite _basinMediumSprite;
        [SerializeField] private Sprite _basinCriticalSprite;
        [SerializeField] private Transform _rivalPlayersContainer;
        [SerializeField] private GameObject _rivalPlayerPrefab;
        [Space]
        [Space]
        [SerializeField] private TurnOrderPanel _turnOrderPanel;
        [Space]
        [SerializeField] private GameObject _vuforiaPanel;
        [SerializeField] private GameObject _modelPanel;
        [SerializeField] private GameObject _backgroundImage;
        [SerializeField] private GameObject _vuforiaARCamera;
            [SerializeField] private Button _closeVuforiaButton;
        [SerializeField] private ModelViewerController _modelViewerController;
        [Space]
        [SerializeField] private GameObject _modeButtons;
        [SerializeField] private GameObject _roomInputsPanel;
        [SerializeField] private TextMeshProUGUI _roomActionText;
        [SerializeField] private TextMeshProUGUI _roomActionButtonText;
        [SerializeField] private TMP_InputField _nickname;
        [SerializeField] private TMP_InputField _room;
        [Space]
        [Header("Another Player Turn UI")]
        [SerializeField] private GameObject _anotherPlayerTurnPanel;
        [SerializeField] private TextMeshProUGUI _anotherPlayerTurnText;
        [SerializeField] private Image _anotherPlayerTurnImage;
        private PlayerRef _lastActivePlayerRef = PlayerRef.None;

        [SerializeField] private AnimationsLogic _animationsLogic;
        [SerializeField] private VictoryUIController _victoryUIController;
        [SerializeField] private MinigameReadyUIController _minigameReadyUIController;
        [SerializeField] private ProjectFlowUIController _projectFlowUIController;
        [SerializeField] private TriviaUIController _triviaUIController;

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
        private static bool _openingPanelShownThisAppSession;
        private bool _isTransitioningToRoomInputs;
        private bool _initPanelVisualsVisible;
        private bool _roomInputsVisualsVisible;
        private bool _isTransitioningFromRoomInputs;
        private bool _vuforiaScanCompletedInSession;

        private sealed class RivalPlayerCardView
        {
            public GameObject Root;
            public Image CharacterImage;
            public TextMeshProUGUI WaterText;
            public TextMeshProUGUI MoneyText;
            public TextMeshProUGUI RivalNameText;
        }
>>>>>>> projects-logic

        private void OnEnable()
        {
            OnPlayerJoinedEvent.RegisterResponse(ShowLobbyCanvas);
            OnShutdownEvent.RegisterResponse(ResetCanvas);
            OnPlayerLeftEvent.RegisterResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RegisterResponse(UpdateLobbyList);
<<<<<<< HEAD
=======
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

            if (_openModelButton != null)
            {
                _openModelButton.onClick.AddListener(OpenModelPanel);
            }

            // Wire roll dice button if assigned
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
                _rollDiceButton.interactable = false;  // Disabled until game is ready
            }

            EnsureMinigameReadyUIController();
            _minigameReadyUIController?.HidePanel();
            EnsureVictoryUIController();
            _victoryUIController?.HidePanel();
            EnsureProjectFlowUIController();
            _projectFlowUIController?.InitializePanels();
            EnsureTriviaUIController();
            _triviaUIController?.HidePanel();

            EnsureMinigameReadyUIController();
            _minigameReadyUIController?.InitializeStatus();
            InitializeGameLobbyStatus();

            // Ensure turn notifications are hidden at start
            EnsureAnimationsLogic();
            _animationsLogic?.HideAnotherPlayerTurnNotification(_anotherPlayerTurnPanel);
>>>>>>> projects-logic
        }

        private void OnDisable()
        {
            OnPlayerJoinedEvent.RemoveResponse(ShowLobbyCanvas);
            OnShutdownEvent.RemoveResponse(ResetCanvas);
            OnPlayerLeftEvent.RemoveResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RemoveResponse(UpdateLobbyList);
<<<<<<< HEAD
=======
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

            if (_openModelButton != null)
            {
                _openModelButton.onClick.RemoveListener(OpenModelPanel);
            }

            // Wire roll dice button if assigned
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.RemoveListener(OnRollDiceClicked);
            }

            StopOpeningPanelAnimations();
        }

        private void Start()
        {
            Debug.Log("[LobbyCanvas] Start() called.");

            // Limit input fields to 10 characters
            if (_nickname != null) _nickname.characterLimit = 10;
            if (_room != null) _room.characterLimit = 10;

            EnsureMinigameReadyUIController();
            _minigameReadyUIController?.InitializeStatus();

            EnsureAnimationsLogic();
            if (_animationsLogic?.OpeningPanel != null)
            {
                bool shouldShowOpening = !_openingPanelShownThisAppSession && !ShouldSkipOpeningPanel();
                if (shouldShowOpening)
                {
                    if (_initPanel != null) _initPanel.SetActive(false);
                    _animationsLogic.OpeningPanel.SetActive(true);
                    StartOpeningPanelAnimations();
                }
                else
                {
                    HideOpeningPanelImmediate();
                    ShowInitPanel();
                }
            }

            _openingPanelShownThisAppSession = true;
        }

        private bool _sessionRestored;
        private bool _diceRolling;
        private Queue<string> _notificationQueue = new Queue<string>();
        private bool _isProcessingNotification;

        public bool IsTurnNotificationPanelActive => _turnNotificationPanel != null && _turnNotificationPanel.activeSelf;
        public bool IsProcessingNotification => _isProcessingNotification || _notificationQueue.Count > 0;

        /// <summary>
        /// Returns true if it is currently the local player's turn.
        /// </summary>
        public bool IsLocalPlayerTurn
        {
            get
            {
                var runner = Networking.Services.FusionNetworkService.LocalRunner;
                if (runner == null) return false;
                var gm = Networking.Managers.GameManager.Instance;
                if (gm == null) return false;
                var localData = gm.GetPlayerData(runner.LocalPlayer, runner);
                return localData != null && localData.IsActiveTurn;
            }
        }

        /// <summary>
        /// Returns true if a turn start notification is pending to be shown for the local player.
        /// Useful for other UI panels to wait until the "It's your turn" animation triggers or finishes.
        /// </summary>
        public bool IsWaitingForTurnNotification => IsLocalPlayerTurn && !_shownTurnNotificationThisTurn;

        private void Update()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            
            if (ShouldReplayInitPanelFade())
            {
                ShowInitPanel();
            }

            if (_animationsLogic?.OpeningPanel != null && _animationsLogic.OpeningPanel.activeSelf)
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
                    HideOpeningPanelImmediate();

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
                        HideOpeningPanelImmediate();
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
                bool gameLobbyActive = _gameLobbyPanel != null && _gameLobbyPanel.activeSelf;
                bool vuforiaActive = _vuforiaPanel != null && _vuforiaPanel.activeSelf;
                bool modelActive = _modelPanel != null && _modelPanel.activeSelf;
                bool gameplaySurfaceVisible = gameLobbyActive || vuforiaActive || modelActive;

                if (gameplaySurfaceVisible)
                {
                    RefreshTurnUI(runner);
                    EnsureMinigameReadyUIController();
                    _minigameReadyUIController?.Refresh(
                        runner,
                        _diceRolling,
                        _turnNotificationPanel,
                        _vuforiaPanel != null && _vuforiaPanel.activeSelf);
                    RefreshGameLobbyStatus(runner);
                    RefreshProjectDecisionUI(runner);
                    RefreshVuforiaBackButtonState(runner);
                    RefreshVictoryPanel(runner);
                    EnsureTriviaUIController();
                    _triviaUIController?.Refresh(runner, _turnNotificationPanel);
                }

                if (gameLobbyActive)
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

            // Find current active player
            PlayerRef currentActivePlayer = PlayerRef.None;
            foreach (var player in runner.ActivePlayers)
            {
                var data = gm.GetPlayerData(player, runner);
                if (data != null && data.IsActiveTurn)
                {
                    currentActivePlayer = player;
                    break;
                }
            }

            // Handle AnotherPlayerTurnPanel
            if (currentActivePlayer != _lastActivePlayerRef)
            {
                _lastActivePlayerRef = currentActivePlayer;
                
                if (currentActivePlayer != PlayerRef.None && currentActivePlayer != runner.LocalPlayer)
                {
                    var activeData = gm.GetPlayerData(currentActivePlayer, runner);
                    if (activeData != null)
                    {
                        string playerName = activeData.Nick.ToString();
                        _animationsLogic?.ShowAnotherPlayerTurnNotification(_anotherPlayerTurnPanel, _anotherPlayerTurnText, _anotherPlayerTurnImage, playerName);
                    }
                }
                else
                {
                    _animationsLogic?.HideAnotherPlayerTurnNotification(_anotherPlayerTurnPanel);
                }
            }

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

            // Turn notification sub-panel — show "your turn" once per turn entry
            if (_turnNotificationPanel != null)
            {
                if (isMyTurn && !_shownTurnNotificationThisTurn)
                {
                    _shownTurnNotificationThisTurn = true;
                    SetTurnNotificationCharacterAccent(runner);
                    ShowTurnNotification("\u00a1Es tu turno!");
                }
                else if (!isMyTurn)
                {
                    // Allow notifications (like landing on a tile) to finish naturally
                    // instead of cutting them off abruptly when the networked turn state changes.
                    _shownTurnNotificationThisTurn = false;
                }
            }
        }

        private void RefreshVictoryPanel(NetworkRunner runner)
        {
            EnsureVictoryUIController();
            _victoryUIController?.RefreshVictoryPanel(runner);
        }

        private void InitializeGameLobbyStatus()
        {
            RefreshGameLobbyStatus(null);
        }

        private void RefreshGameLobbyStatus(NetworkRunner runner)
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
                _tileText.text = BuildGameLobbyTileText(boardPosition, tileType);
            }

            // Only trigger the dynamic resource animation when not busy with character jumps or notifications.
            // This ensures the +/-, green/red animation isn't hidden by tile arrival animations.
            bool isBusyAnimating = _diceRolling || _isProcessingNotification;

            if (_resourceChangeUI != null)
            {
                if (!isBusyAnimating)
                {
                    _resourceChangeUI.OnResourcesChanged(waterAmount, moneyAmount);
                }
            }
            else
            {
                if (_waterText != null)
                {
                    _waterText.text = $"{waterAmount}";
                }

                if (_moneyText != null)
                {
                    _moneyText.text = $"{moneyAmount}";
                }
            }

            RefreshBasinHealthImage(runner);
        }

        private void RefreshProjectDecisionUI(NetworkRunner runner)
        {
            EnsureProjectFlowUIController();
            bool vuforiaActive = _vuforiaPanel != null && _vuforiaPanel.activeSelf;
            Debug.Log($"[LobbyCanvas] RefreshProjectDecisionUI. VuforiaActive={vuforiaActive}");
            _projectFlowUIController?.RefreshProjectDecisionUI(
                runner,
                vuforiaActive,
                CloseVuforiaPanel);
        }

        private void RefreshBasinHealthImage(NetworkRunner runner)
        {
            if (_basinHealthImage == null && _basinHealthRadialFill == null && _modelViewerController == null) return;

            var gm = Networking.Managers.GameManager.Instance;
            if (gm == null) return;

            int basinHealth;
            int startingBasinHealth = gm.StartingBasinHealth;

            var localData = runner != null ? gm.GetPlayerData(runner.LocalPlayer, runner) : null;
            if (localData != null)
            {
                basinHealth = localData.BasinHealth;
                if (localData.CurrentRound <= 0 && basinHealth <= 0)
                {
                    basinHealth = startingBasinHealth;
                }
            }
            else
            {
                basinHealth = startingBasinHealth;
            }

            float normalizedHealth = (float)basinHealth / Mathf.Max(1, startingBasinHealth);
            float percentage = normalizedHealth * 100f;

            if (_modelViewerController != null)
            {
                _modelViewerController.RefreshBasinModels(percentage);
            }

            Color color;
            Sprite statusSprite = null;

            if (percentage > 80f)
            {
                color = Color.green;
                statusSprite = _basinHealthySprite;
            }
            else if (percentage > 20f)
            {
                color = Color.yellow;
                statusSprite = _basinMediumSprite;
            }
            else
            {
                color = Color.red;
                statusSprite = _basinCriticalSprite;
            }

            if (_basinHealthImage != null)
            {
                _basinHealthImage.color = Color.white;
                if (statusSprite != null)
                {
                    _basinHealthImage.sprite = statusSprite;
                }
            }

            if (_basinHealthRadialFill != null)
            {
                _basinHealthRadialFill.fillAmount = Mathf.Clamp01(normalizedHealth);
                _basinHealthRadialFill.color = color;
            }
        }

        public void NotifyDiceRollCompleted(int diceValue)
        {
            // Animate character jump first, then show turn notification
            StartCoroutine(AnimateCharacterJumpThenShowNotification(diceValue));
        }

        private IEnumerator AnimateCharacterJumpThenShowNotification(int jumps)
        {
            // Get character image rect and animate jump
            if (_gameLobbyCharacterImage != null)
            {
                RectTransform rectTransform = _gameLobbyCharacterImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 originalPosition = rectTransform.anchoredPosition;
                    float jumpHeight = 80f;  // How high to jump
                    float jumpUpDuration = 0.2f;  // Reduced duration for repeated jumps
                    float jumpDownDuration = 0.2f;

                    for (int i = 0; i < jumps; i++)
                    {
                        // Jump up
                        float elapsedUp = 0f;
                        while (elapsedUp < jumpUpDuration)
                        {
                            elapsedUp += Time.deltaTime;
                            float progress = Mathf.Clamp01(elapsedUp / jumpUpDuration);
                            float easeProgress = progress < 0.5f 
                                ? 2f * progress * progress 
                                : -1f + (4f - 2f * progress) * progress; // easeOutQuad
                            
                            Vector2 newPos = originalPosition;
                            newPos.y += jumpHeight * easeProgress;
                            rectTransform.anchoredPosition = newPos;
                            yield return null;
                        }

                        // Jump down back to original position
                        float elapsedDown = 0f;
                        while (elapsedDown < jumpDownDuration)
                        {
                            elapsedDown += Time.deltaTime;
                            float progress = Mathf.Clamp01(elapsedDown / jumpDownDuration);
                            float easeProgress = progress * progress; // easeInQuad
                            
                            Vector2 newPos = originalPosition;
                            newPos.y += jumpHeight * (1f - easeProgress);
                            rectTransform.anchoredPosition = newPos;
                            yield return null;
                        }

                        // Ensure we're exactly at original position before next jump
                        rectTransform.anchoredPosition = originalPosition;
                        
                        // Brief pause between jumps
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                else
                {
                    Debug.LogWarning("[LobbyCanvas] CharacterImage RectTransform not found!");
                }
            }

            _diceRolling = false;

            // Now show tile info after animation completes
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (runner != null)
            {
                var gm = Networking.Managers.GameManager.Instance;
                var localData = gm?.GetPlayerData(runner.LocalPlayer, runner);
                if (localData != null)
                {
                    var tileType = gm.GetTileTypeAtPosition(localData.BoardPosition);
                    SetTurnNotificationTileAccent(localData.BoardPosition, gm);
                    ShowTurnNotification(BuildTileNotificationText(tileType));
                }
                RefreshTurnUI(runner);
            }
        }

        private void ShowTurnNotification(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _notificationQueue.Enqueue(message);
            if (!_isProcessingNotification)
            {
                StartCoroutine(ProcessNotificationQueue());
            }
        }

        private IEnumerator ProcessNotificationQueue()
        {
            _isProcessingNotification = true;

            while (_notificationQueue.Count > 0)
            {
                string message = _notificationQueue.Dequeue();
                
                EnsureAnimationsLogic();
                _animationsLogic?.ShowTurnNotification(_turnNotificationPanel, _turnNotificationText, message);

                // Wait for the notification duration + exit time buffer (approx 3.5s)
                yield return new WaitForSeconds(3.5f);
            }

            _isProcessingNotification = false;
        }

        private void HideTurnNotification()
        {
            EnsureAnimationsLogic();
            _animationsLogic?.HideTurnNotification(_turnNotificationPanel);
            _notificationQueue.Clear();
            _isProcessingNotification = false;
        }

        private void RefreshActiveProjectsUI(NetworkRunner runner)
        {
            EnsureProjectFlowUIController();
            _projectFlowUIController?.RefreshActiveProjectsUI(runner);
        }

        private static string BuildRoundStatusText(Networking.Managers.GameManager gameManager)
        {
            int currentRound = Mathf.Max(1, gameManager.CurrentRound);
            return $"Ronda {currentRound}/{gameManager.MaxRoundsToWin}";
        }

        private static string BuildTurnStatusText(Networking.Managers.GameManager gameManager, NetworkRunner runner, bool isMyTurn)
        {
            string weatherName = gameManager.ActiveWeatherCardName;
            
            if (!string.IsNullOrEmpty(weatherName))
            {
                // Show only the weather name in a stylized format
                return $"<b>Clima: {weatherName}</b>";
            }

            // Return nothing if there is no active weather
            return string.Empty;
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
>>>>>>> projects-logic
        }

        //Called from button
        public void SetGameMode(int gameMode)
        {
<<<<<<< HEAD
            Networking.Managers.GameManager.Instance.SetGameState(Networking.Managers.GameManager.GameState.Lobby);
            _gameMode = (GameMode)gameMode;
            _modeButtons.SetActive(false);
            _nickname.transform.parent.gameObject.SetActive(true);
=======
            if (_isTransitioningToRoomInputs)
            {
                return;
            }

            if (Networking.Managers.GameManager.Instance != null)
            {
                Networking.Managers.GameManager.Instance.SetGameState(Networking.Managers.GameManager.GameState.Lobby);
            }

            _gameMode = (GameMode)gameMode;
            _isTransitioningToRoomInputs = true;
            _initPanelVisualsVisible = false;

            EnsureAnimationsLogic();
            _animationsLogic?.FadeOutInitPanelObjects(() =>
            {
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
                    ShowRoomInputsPanel(roomInputsRoot);
                }
                else
                {
                    Debug.LogWarning("[LobbyCanvas] Room inputs panel is not assigned and nickname parent could not be resolved.");
                }

                _isTransitioningToRoomInputs = false;
            });

            if (_animationsLogic == null)
            {
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
                    ShowRoomInputsPanel(roomInputsRoot);
                }
                else
                {
                    Debug.LogWarning("[LobbyCanvas] Room inputs panel is not assigned and nickname parent could not be resolved.");
                }

                _isTransitioningToRoomInputs = false;
            }
>>>>>>> projects-logic
        }

        //Called from button
        public void StartLauncher()
        {
<<<<<<< HEAD
            Launcher = FindFirstObjectByType<Networking.Managers.GameLauncher>();
            Nickname = _nickname.text;
            PlayerPrefs.SetString("Nick", Nickname);
            Launcher.Launch(_gameMode, _room.text);
            _nickname.transform.parent.gameObject.SetActive(false);
=======
            if (_isTransitioningFromRoomInputs)
            {
                return;
            }

            if (_nickname == null || string.IsNullOrWhiteSpace(_nickname.text))
            {
                Debug.LogWarning("[LobbyCanvas] Nickname cannot be empty.");
                return;
            }

            if (_room == null || string.IsNullOrWhiteSpace(_room.text))
            {
                Debug.LogWarning("[LobbyCanvas] Room name cannot be empty.");
                return;
            }

            Launcher = FindFirstObjectByType<Networking.Managers.GameLauncher>();
            Nickname = _nickname.text;
            PlayerPrefs.SetString("Nick", Nickname);

            var roomInputsRoot = GetRoomInputsRoot();
            if (roomInputsRoot == null)
            {
                BeginLauncherFlow();
                return;
            }

            _isTransitioningFromRoomInputs = true;
            _roomInputsVisualsVisible = false;

            EnsureAnimationsLogic();
            _animationsLogic?.FadeOutRoomInputsObjects(() =>
            {
                roomInputsRoot.SetActive(false);
                _isTransitioningFromRoomInputs = false;
                BeginLauncherFlow();
            });

            if (_animationsLogic == null)
            {
                roomInputsRoot.SetActive(false);
                _isTransitioningFromRoomInputs = false;
                BeginLauncherFlow();
            }
>>>>>>> projects-logic
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
<<<<<<< HEAD
            Networking.Services.FusionNetworkService.LocalRunner.SessionInfo.IsOpen = false;
            Networking.Services.FusionNetworkService.LocalRunner.SessionInfo.IsVisible = false;
            // TODO: Add scene loading when needed
=======
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
                _turnOrderPanel = FindAnyObjectByType<TurnOrderPanel>(FindObjectsInactive.Include);
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
            Sprite selectedTurnSprite = null;
            Color selectedCharacterColor = Color.white;

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
                            selectedTurnSprite = charConfig.TurnImage != null ? charConfig.TurnImage : charConfig.CharacterSprite;
                            selectedCharacterColor = charConfig.CharacterColor;
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

            EnsureAnimationsLogic();
            _animationsLogic?.SetTurnNotificationSecondaryImageSprite(selectedTurnSprite);
            _animationsLogic?.SetTurnNotificationAccentColor(selectedCharacterColor);

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
                    playerData.RPC_LoadMinigameScene("Minigame");
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

            _vuforiaScanCompletedInSession = false;
            if (_closeVuforiaButton != null)
            {
                _closeVuforiaButton.interactable = false;
            }

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

        public void OpenModelPanel()
        {
            Debug.Log("[LobbyCanvas] Opening model panel...");

            if (_gameLobbyPanel != null)
            {
                _gameLobbyPanel.SetActive(false);
            }

            if (_modelPanel != null)
            {
                _modelPanel.SetActive(true);
            }

            if (_modelViewerController != null)
            {
                _modelViewerController.Show();
                Debug.Log("[LobbyCanvas] ✓ Model panel opened");
            }
            else if (_modelPanel == null)
            {
                Debug.LogError("[LobbyCanvas] ModelPanel not assigned in inspector!");
            }
            else
            {
                Debug.LogWarning("[LobbyCanvas] ModelViewerController not assigned in inspector. Showing panel only.");
            }
        }

        /// <summary>
        /// Returns from whichever overlay panel is active back to GameLobbyPanel.
        /// Handles both the Vuforia panel and the model panel.
        /// </summary>
        public void CloseVuforiaPanel()
        {
            Debug.Log("[LobbyCanvas] Closing overlay panel...");

            _vuforiaScanCompletedInSession = false;
            if (_closeVuforiaButton != null)
            {
                _closeVuforiaButton.interactable = true;
            }

            bool wasVuforiaOpen = _vuforiaPanel != null && _vuforiaPanel.activeSelf;
            bool wasModelOpen = _modelPanel != null && _modelPanel.activeSelf;

            if (wasVuforiaOpen && _vuforiaARCamera != null)
            {
                _vuforiaARCamera.SetActive(false);
                Debug.Log("[LobbyCanvas] ✓ Vuforia AR Camera deactivated");
            }

            if (wasVuforiaOpen)
            {
                _vuforiaPanel.SetActive(false);
            }

            if (wasModelOpen)
            {
                if (_modelViewerController != null)
                {
                    _modelViewerController.Hide();
                }
                else if (_modelPanel != null)
                {
                    _modelPanel.SetActive(false);
                }
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.SetActive(true);
            }

            if (_gameLobbyPanel != null)
            {
                _gameLobbyPanel.SetActive(true);
                Debug.Log("[LobbyCanvas] ✓ Returned to Game Lobby panel");
            }

            if (wasVuforiaOpen)
            {
                var runner = Networking.Services.FusionNetworkService.LocalRunner;
                var gm = Networking.Managers.GameManager.Instance;
                var localData = runner != null && gm != null ? gm.GetPlayerData(runner.LocalPlayer, runner) : null;

                bool justResolvedTeleport = false;
                if (localData != null && localData.IsPendingTeleportTileResolution)
                {
                    var tileType = gm.GetTileTypeAtPosition(localData.BoardPosition);
                    SetTurnNotificationTileAccent(localData.BoardPosition, gm);
                    ShowTurnNotification(BuildTileNotificationText(tileType));
                    
                    localData.RPC_ResolveTeleportLanding();
                    justResolvedTeleport = true;
                }

                // If the player closed the AR panel while a project scan was pending, treat it as a decline.
                // Skip this if we just resolved a teleport landing, to give the player a chance to scan the target tile.
                EnsureProjectFlowUIController();
                if (!justResolvedTeleport && _projectFlowUIController != null && _projectFlowUIController.IsProjectFlowVisible)
                {
                    if (runner != null && localData != null)
                    {
                        if (localData.IsAwaitingProjectScan || localData.IsAwaitingProjectDecision)
                        {
                            localData.RPC_RequestDeclinePendingProject();
                        }
                        else if (localData.IsAwaitingCardScan)
                        {
                            // Closing AR while awaiting a card scan: skip the card and advance the turn.
                            localData.RPC_RequestSkipCardScan();
                        }
                    }
                }
            }
        }

        public void CloseModelPanel()
        {
            CloseVuforiaPanel();
>>>>>>> projects-logic
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
<<<<<<< HEAD
            foreach(var player in Networking.Services.FusionNetworkService.LocalRunner.ActivePlayers)
=======
            foreach (var player in Networking.Services.FusionNetworkService.LocalRunner.ActivePlayers)
>>>>>>> projects-logic
            {
                if (player != Networking.Services.FusionNetworkService.LocalRunner.LocalPlayer)
                    Networking.Services.FusionNetworkService.LocalRunner.Disconnect(player);
            }
        }

        private void ResetCanvas(PlayerRef player, NetworkRunner runner)
        {
<<<<<<< HEAD
            _initPanel.SetActive(true);
            _modeButtons.SetActive(true);
            _lobbyPanel.SetActive(false);
            _startButton.gameObject.SetActive(runner.IsServer);
=======
            Debug.Log("[LobbyCanvas] Canvas reset");
            HideOpeningPanelImmediate();

            EnsureAnimationsLogic();
            if (_animationsLogic?.LoadingPanel != null && _animationsLogic.LoadingPanel.activeSelf)
            {
                _animationsLogic.CancelLoadingPanelAnimation();
                _animationsLogic.LoadingPanel.SetActive(false);
            }

            if (_initPanel != null)
            {
                _initPanel.SetActive(true);
            }
            _modeButtons.SetActive(true);
            var roomInputsRoot = GetRoomInputsRoot();
            if (roomInputsRoot != null)
            {
                roomInputsRoot.SetActive(false);
            }
            _roomInputsVisualsVisible = false;
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
            EnsureMinigameReadyUIController();
            _minigameReadyUIController?.HidePanel();
            EnsureVictoryUIController();
            _victoryUIController?.HidePanel();
            EnsureProjectFlowUIController();
            _projectFlowUIController?.InitializePanels();
            EnsureTriviaUIController();
            _triviaUIController?.HidePanel();
            EnsureVictoryUIController();
            _victoryUIController?.ResetVictoryState();
            _shownTurnNotificationThisTurn = false;
            HideTurnNotification();
            _animationsLogic?.HideAnotherPlayerTurnNotification(_anotherPlayerTurnPanel);
            _lastActivePlayerRef = PlayerRef.None;
            _minigameReadyUIController?.InitializeStatus();
            InitializeGameLobbyStatus();
            if (_vuforiaPanel != null)
            {
                _vuforiaPanel.SetActive(false);
            }
            if (_modelViewerController != null)
            {
                _modelViewerController.Hide();
            }
            else if (_modelPanel != null)
            {
                _modelPanel.SetActive(false);
            }
            if (_vuforiaARCamera != null)
            {
                _vuforiaARCamera.SetActive(false);
            }
            if (_backgroundImage != null)
            {
                _backgroundImage.SetActive(true);
            }
            if (runner != null && _startButton != null)
            {
                _startButton.gameObject.SetActive(runner.IsServer);
            }
            ShowInitPanel();
>>>>>>> projects-logic
        }

        public void ShowLobbyCanvas(PlayerRef player, NetworkRunner runner)
        {
<<<<<<< HEAD
=======
            EnsureAnimationsLogic();
            if (_animationsLogic?.LoadingPanel != null && _animationsLogic.LoadingPanel.activeSelf)
            {
                _animationsLogic.CancelLoadingPanelAnimation();
                _animationsLogic.LoadingPanel.SetActive(false);
            }

>>>>>>> projects-logic
            _initPanel.SetActive(false);
            _lobbyPanel.SetActive(true);
        }

<<<<<<< HEAD
=======
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
            EnsureAnimationsLogic();
            _animationsLogic?.StartLoadingImageAnimation();
        }

        private void StartOpeningPanelAnimations()
        {
            EnsureAnimationsLogic();
            _animationsLogic?.StartOpeningPanelAnimations();
        }

        private void StopOpeningPanelAnimations()
        {
            EnsureAnimationsLogic();
            _animationsLogic?.StopOpeningPanelAnimations();
        }

        private void CloseOpeningPanel()
        {
            EnsureAnimationsLogic();
            _animationsLogic?.CloseOpeningPanel(_initPanel);
        }

        private void ShowInitPanel()
        {
            EnsureAnimationsLogic();
            _animationsLogic?.ShowInitPanel(_initPanel);
            _initPanelVisualsVisible = true;
        }

        private void ShowRoomInputsPanel(GameObject roomInputsRoot)
        {
            EnsureAnimationsLogic();
            _animationsLogic?.ShowRoomInputsPanel(roomInputsRoot);
            _roomInputsVisualsVisible = true;
        }

        private void BeginLauncherFlow()
        {
            EnsureAnimationsLogic();
            // Show loading panel for both Host and Client modes
            if ((_gameMode == GameMode.Host || _gameMode == GameMode.Client) && _animationsLogic?.LoadingPanel != null)
            {
                if (_initPanel != null)
                    _initPanel.SetActive(false);
                _animationsLogic.LoadingPanel.SetActive(true);
                StartLoadingImageAnimation();
            }

            Launcher.Launch(_gameMode, _room.text);
        }

        private bool ShouldReplayInitPanelFade()
        {
            if (_initPanelVisualsVisible || _isTransitioningToRoomInputs)
            {
                return false;
            }

            if (_initPanel == null || !_initPanel.activeSelf)
            {
                return false;
            }

            if ((_animationsLogic?.OpeningPanel != null && _animationsLogic.OpeningPanel.activeSelf)
                || (_animationsLogic?.LoadingPanel != null && _animationsLogic.LoadingPanel.activeSelf))
            {
                return false;
            }

            if ((_lobbyPanel != null && _lobbyPanel.activeSelf)
                || (_gameLobbyPanel != null && _gameLobbyPanel.activeSelf)
                || (_vuforiaPanel != null && _vuforiaPanel.activeSelf)
                || (_modelPanel != null && _modelPanel.activeSelf))
            {
                return false;
            }

            var roomInputsRoot = GetRoomInputsRoot();
            bool roomInputsVisible = roomInputsRoot != null && roomInputsRoot.activeSelf;
            bool modeButtonsVisible = _modeButtons != null && _modeButtons.activeSelf;
            return modeButtonsVisible && !roomInputsVisible;
        }

        private void HideOpeningPanelImmediate()
        {
            EnsureAnimationsLogic();
            _animationsLogic?.HideOpeningPanelImmediate();
        }

        private bool ShouldSkipOpeningPanel()
        {
            var runner = Networking.Services.FusionNetworkService.LocalRunner;
            if (runner == null) return false;

            if (FindFirstObjectByType<Networking.Managers.CharacterSelectionManager>() != null)
                return false;

            var gameManager = Networking.Managers.GameManager.Instance;
            if (gameManager == null) return false;

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = gameManager.GetPlayerData(player, runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                    return true;
            }

            return false;
        }

        private void EnsureAnimationsLogic()
        {
            if (_animationsLogic != null)
            {
                return;
            }

            _animationsLogic = GetComponent<AnimationsLogic>();
            if (_animationsLogic == null)
            {
                _animationsLogic = gameObject.AddComponent<AnimationsLogic>();
                Debug.Log("[LobbyCanvas] AnimationsLogic component was missing and has been auto-added.");
            }
        }

        private void EnsureVictoryUIController()
        {
            if (_victoryUIController == null)
            {
                _victoryUIController = GetComponent<VictoryUIController>();
                if (_victoryUIController == null)
                {
                    _victoryUIController = gameObject.AddComponent<VictoryUIController>();
                    Debug.Log("[LobbyCanvas] VictoryUIController component was missing and has been auto-added.");
                }
            }

            EnsureMinigameReadyUIController();
            _victoryUIController.Configure(
                _minigameReadyUIController?.Panel,
                _rollDiceButton,
                _openVuforiaButton);
        }

        private void EnsureMinigameReadyUIController()
        {
            if (_minigameReadyUIController != null) return;

            _minigameReadyUIController = GetComponent<MinigameReadyUIController>();
            if (_minigameReadyUIController == null)
            {
                _minigameReadyUIController = gameObject.AddComponent<MinigameReadyUIController>();
                Debug.Log("[LobbyCanvas] MinigameReadyUIController was missing and has been auto-added.");
            }
        }

        private void EnsureProjectFlowUIController()
        {
            if (_projectFlowUIController != null) return;

            _projectFlowUIController = GetComponent<ProjectFlowUIController>();
            if (_projectFlowUIController == null)
            {
                _projectFlowUIController = gameObject.AddComponent<ProjectFlowUIController>();
                Debug.Log("[LobbyCanvas] ProjectFlowUIController was missing and has been auto-added.");
            }
        }

        private void EnsureTriviaUIController()
        {
            if (_triviaUIController != null) return;

            _triviaUIController = GetComponent<TriviaUIController>();
            if (_triviaUIController == null)
            {
                _triviaUIController = gameObject.AddComponent<TriviaUIController>();
                Debug.Log("[LobbyCanvas] TriviaUIController was missing and has been auto-added.");
            }
        }

        private static string BuildGameLobbyTileText(int boardPosition, Networking.Services.SliceTileType tileType)
        {
            return BuildTileNotificationText(tileType);
        }

        private static string BuildTileNotificationText(Networking.Services.SliceTileType tileType)
        {
            return tileType switch
            {
                Networking.Services.SliceTileType.Start => "Casilla de inicio",
                Networking.Services.SliceTileType.Hydric => "Casilla hídrica",
                Networking.Services.SliceTileType.Catastrophic => "Casilla de catástrofe",
                Networking.Services.SliceTileType.Project => "Casilla de proyecto",
                Networking.Services.SliceTileType.DrawCard => "Casilla de evento",
                Networking.Services.SliceTileType.Trivia => "Casilla de trivia",
                _ => "Casilla"
            };
        }

        private void SetTurnNotificationCharacterAccent(NetworkRunner runner)
        {
            EnsureAnimationsLogic();
            _animationsLogic?.SetTurnNotificationAccentColor(GetLocalCharacterAccentColor(runner));
        }

        private void RefreshVuforiaBackButtonState(NetworkRunner runner)
        {
            if (_closeVuforiaButton == null || _vuforiaPanel == null || !_vuforiaPanel.activeSelf)
            {
                return;
            }

            if (!_vuforiaScanCompletedInSession)
            {
                var gameManager = Networking.Managers.GameManager.Instance;
                var localData = gameManager?.GetPlayerData(runner.LocalPlayer, runner);
                bool hasEventCardInfo = localData != null && !string.IsNullOrWhiteSpace(localData.PendingCardTitle.ToString());
                bool hasProjectScanResult = localData != null && (localData.IsAwaitingProjectDecision || localData.PendingProjectId > 0);
                _vuforiaScanCompletedInSession = hasEventCardInfo || hasProjectScanResult;
            }

            _closeVuforiaButton.interactable = _vuforiaScanCompletedInSession;
        }

        private void SetTurnNotificationTileAccent(int boardPosition, Networking.Managers.GameManager gameManager)
        {
            EnsureAnimationsLogic();
            Color tileColor = gameManager != null ? gameManager.GetTileColorAtPosition(boardPosition) : Color.white;
            _animationsLogic?.SetTurnNotificationAccentColor(tileColor);
        }

        private Color GetLocalCharacterAccentColor(NetworkRunner runner)
        {
            if (runner == null)
            {
                return Color.white;
            }

            var gameManager = Networking.Managers.GameManager.Instance;
            var localPlayerData = gameManager?.GetPlayerData(runner.LocalPlayer, runner);
            if (localPlayerData == null || localPlayerData.SelectedCharacterId <= 0)
            {
                return Color.white;
            }

            var charConfig = Networking.Managers.CharacterDatabase.Instance?.GetCharacterById(localPlayerData.SelectedCharacterId);
            return charConfig != null ? charConfig.CharacterColor : Color.white;
        }

>>>>>>> projects-logic
        public void UpdateLobbyList(PlayerRef playerRef, NetworkRunner runner)
        {
            _startButton.gameObject.SetActive(runner.IsServer);
            string players = default;
            string isLocal;
<<<<<<< HEAD
            foreach(var player in runner.ActivePlayers)
=======
            foreach (var player in runner.ActivePlayers)
>>>>>>> projects-logic
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
<<<<<<< HEAD
=======

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

        public void DisplayWeatherRollResult(int roll, int waterDelta, int moneyDelta)
        {
            if (_diceResultText != null)
            {
                string waterText = waterDelta >= 0 ? $"+{waterDelta} AGUA" : $"{waterDelta} AGUA";
                string moneyText = moneyDelta >= 0 ? $"+{moneyDelta} DINERO" : $"{moneyDelta} DINERO";
                _diceResultText.text = $"Tirada clima: {roll} → {waterText}, {moneyText}";
                Debug.Log($"[LobbyCanvas] Displayed weather roll: {roll}, {waterText}, {moneyText}");
            }
            else
            {
                Debug.LogWarning("[LobbyCanvas] Dice result text not assigned for weather roll!");
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
>>>>>>> projects-logic
    }
}
