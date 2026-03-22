using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Analytics;
using HijackPoker.Animation;
using HijackPoker.Api;
using HijackPoker.Models;
using HijackPoker.UI;
using HijackPoker.Utils;

namespace HijackPoker.Managers
{
    /// <summary>
    /// Singleton that bootstraps the entire poker client.
    /// Creates Canvas, all views, and orchestrates state updates.
    /// Manages ConnectionManager for WS/REST, auto-play, and hand history.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        private PokerApiClient _apiClient;
        private TableStateManager _stateManager;
        private AnimationController _animController;
        private ConnectionManager _connectionManager;
        private SessionTracker _sessionTracker;
        private PlayerProfiler _playerProfiler;
        private HandNarrator _handNarrator;
        private InputHandler _inputHandler;
        private int _tableId = 1;
        private bool _isProcessing;
        private bool _isTableTransitioning;
        private bool _lastIsPortrait;

        // Auto-play
        private bool _autoPlaying;
        private bool _autoPlayStopRequested;

        // Per-table state
        private readonly Dictionary<int, TableContext> _tableContexts = new();

        // Background auto-play (keeps calling POST /process while in lobby)
        private class BackgroundAutoPlay
        {
            public int TableId;
            public int SpeedIndex;
            public PokerApiClient ApiClient;
            public Coroutine Coroutine;
            public int ConsecutiveErrors;
        }
        private readonly Dictionary<int, BackgroundAutoPlay> _backgroundAutoPlays = new();

        // Loading overlay
        private LoadingOverlay _loadingOverlay;

        // Showdown
        private ShowdownOverlay _showdownOverlay;
        private SceneTransition _sceneTransition;
        private bool _showdownPaused;

        // Views
        private GameObject _canvasGo;
        private RectTransform _canvasTransform;
        private TableView _tableView;
        private SeatView[] _seats;
        private CommunityCardsView _communityCards;
        private HudView _hud;
        private ControlsView _controls;
        private ConnectionStatusView _connectionStatus;
        private HandHistoryView _handHistory;
        private CardPreviewOverlay _cardPreview;
        private PlayerStatsTooltip _statsTooltip;
        private SessionStatsPanel _sessionStatsPanel;
        private HelpPopupView _helpPopup;
        private LobbyView _lobbyView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var go = new GameObject("GameManager");
            _instance = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // Wait one frame for screen orientation to settle on mobile
            // before building UI, then continue with async initialization.
            StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            yield return null;
            UI.LayoutConfig.ResetOrientationCache();
            StartAsync();
        }

        private void StartAsync()
        {
            _stateManager = new TableStateManager();
            _stateManager.OnStateChanged += HandleStateChanged;
            _animController = new AnimationController();
            _sessionTracker = new SessionTracker();
            _playerProfiler = new PlayerProfiler();
            _handNarrator = new HandNarrator();
            AudioManager.Initialize(gameObject);

            // Show lobby first — it provides table selection
            ShowLobby();
        }

        private void ShowLobby()
        {
            // Minimal canvas for lobby
            _canvasGo = new GameObject("Canvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UI.LayoutConfig.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UI.LayoutConfig.CanvasMatch;

            _canvasGo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(transform, false);
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasRt = _canvasGo.GetComponent<RectTransform>();
            _lobbyView = LobbyView.Create(canvasRt, _animController);
            _lobbyView.OnTableSelected += OnLobbyTableSelected;

            // Show AUTO badges for any tables with background auto-play
            foreach (var kvp in _backgroundAutoPlays)
                _lobbyView.SetAutoPlayIndicator(kvp.Key, true);
        }

        private void OnLobbyTableSelected(int tableId)
        {
            _tableId = tableId;

            bool resumeAutoPlay = _backgroundAutoPlays.ContainsKey(tableId);
            StopBackgroundAutoPlay(tableId);

            if (_lobbyView != null)
            {
                _lobbyView.OnTableSelected -= OnLobbyTableSelected;

                // Scene transition: fade to black → destroy lobby → build game → fade from black
                var canvasRt = _canvasGo.GetComponent<RectTransform>();
                _sceneTransition = SceneTransition.Create(canvasRt, _animController);
                _sceneTransition.FadeToBlack(0.3f, () =>
                {
                    if (_lobbyView != null)
                    {
                        Destroy(_lobbyView.gameObject);
                        _lobbyView = null;
                    }
                    InitializeGameAfterLobby(resumeAutoPlay);
                });
            }
            else
            {
                InitializeGameAfterLobby(resumeAutoPlay);
            }
        }

        private async void InitializeGameAfterLobby(bool resumeAutoPlay = false)
        {
            // Cancel any lingering lobby animations (PulseGlow, sparkles, entrance tweens)
            _animController?.CancelAll();

            if (_canvasGo != null) Destroy(_canvasGo);

            BuildUI();
            _lastIsPortrait = LayoutConfig.IsPortrait;

            // Fade from black if scene transition is active
            if (_sceneTransition != null)
            {
                var canvasRt = _canvasGo.GetComponent<RectTransform>();
                var fadeIn = SceneTransition.Create(canvasRt, _animController);
                fadeIn.FadeFromBlack(0.3f, () => fadeIn.Cleanup());
                _sceneTransition.Cleanup();
                _sceneTransition = null;
            }

            // Restore saved context if available
            bool hasContext = _tableContexts.ContainsKey(_tableId);
            if (hasContext)
                RestoreContext(_tableId);

            _apiClient = gameObject.AddComponent<PokerApiClient>();
            _connectionManager = gameObject.AddComponent<ConnectionManager>();
            _connectionManager.Initialize(_apiClient, _tableId);
            _connectionManager.OnConnectionStateChanged += HandleConnectionStateChanged;
            _connectionManager.OnWebSocketStateReceived += HandleWebSocketStatePush;

            _controls.SetInteractable(false);
            _loadingOverlay?.Show("Connecting...");

            bool connected = await _connectionManager.ConnectAsync();
            if (connected)
            {
                _loadingOverlay?.SetMessage("Loading table...");
                await FetchInitialState();

                // Table reset: if mid-hand on first join, advance to clean state
                if (!hasContext)
                {
                    _loadingOverlay?.SetMessage("Preparing table...");
                    await ResetToCleanState();
                }

                _loadingOverlay?.Hide();

                if (resumeAutoPlay || (hasContext && _tableContexts.TryGetValue(_tableId, out var ctx) && ctx.WasAutoPlaying))
                    StartAutoPlay();
            }
            else
            {
                _loadingOverlay?.Hide();
                _stateManager.UpdateState(MockStateFactory.CreateMockState());
                _controls.SetInteractable(true);
            }
        }

        private void BuildUI()
        {
            // Canvas
            _canvasGo = new GameObject("Canvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UI.LayoutConfig.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UI.LayoutConfig.CanvasMatch;

            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem — required for button clicks
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(transform, false);
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasRt = canvasGo.GetComponent<RectTransform>();

            // Full-screen background (fills entire screen including behind notch/home indicator)
            var fullBgRt = UIFactory.CreatePanel("FullScreenBg", canvasRt, UIFactory.Background);
            UIFactory.StretchFill(fullBgRt);

            // Safe area container — constrains all interactive content within Screen.safeArea
            var safeAreaGo = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaGo.transform.SetParent(canvasRt, false);
            var safeAreaRt = safeAreaGo.GetComponent<RectTransform>();
            UIFactory.StretchFill(safeAreaRt);
            safeAreaGo.AddComponent<UI.SafeAreaPanel>();

            _canvasTransform = safeAreaRt;
            UI.LayoutConfig.SetContentRoot(safeAreaRt);

            // Table background + surface (stretch-fills behind everything)
            var tableTheme = TableTheme.ForTable(_tableId);
            _tableView = TableView.Create(safeAreaRt, _animController, tableTheme);

            // ── Main vertical grid (3 rows: HUD, Game Area, Controls) ──
            var mainGrid = new GameObject("MainGrid", typeof(RectTransform));
            mainGrid.transform.SetParent(safeAreaRt, false);
            var mainGridRt = mainGrid.GetComponent<RectTransform>();
            UIFactory.StretchFill(mainGridRt);

            var mainVlg = mainGrid.AddComponent<VerticalLayoutGroup>();
            mainVlg.childAlignment = TextAnchor.UpperCenter;
            mainVlg.childControlWidth = true;
            mainVlg.childControlHeight = true;
            mainVlg.childForceExpandWidth = true;
            mainVlg.childForceExpandHeight = false;
            mainVlg.spacing = 0;

            // ── Row 0: HUD (fixed height) ──────────────────────────────
            _hud = HudView.Create(mainGrid.transform);
            _hud.AnimController = _animController;
            var hudLE = _hud.gameObject.AddComponent<LayoutElement>();
            hudLE.preferredHeight = LayoutConfig.HudRowHeight;
            hudLE.flexibleWidth = 1;

            // ── Row 1: Game Area (flexible, fills remaining space) ──────
            var gameArea = new GameObject("GameArea", typeof(RectTransform));
            gameArea.transform.SetParent(mainGrid.transform, false);
            var gameAreaLE = gameArea.AddComponent<LayoutElement>();
            gameAreaLE.flexibleHeight = 1;
            gameAreaLE.flexibleWidth = 1;
            // No layout group — children positioned via anchors

            // ── Center content (community cards + pot) ──────────────────
            var centerContent = new GameObject("CenterContent", typeof(RectTransform));
            centerContent.transform.SetParent(gameArea.transform, false);
            var centerRt = centerContent.GetComponent<RectTransform>();
            // Anchor to center band of the game area
            centerRt.anchorMin = LayoutConfig.CenterContentMin;
            centerRt.anchorMax = LayoutConfig.CenterContentMax;
            centerRt.offsetMin = Vector2.zero;
            centerRt.offsetMax = Vector2.zero;

            var centerVlg = centerContent.AddComponent<VerticalLayoutGroup>();
            centerVlg.childAlignment = TextAnchor.MiddleCenter;
            centerVlg.childControlWidth = false;
            centerVlg.childControlHeight = false;
            centerVlg.childForceExpandWidth = false;
            centerVlg.childForceExpandHeight = false;
            centerVlg.spacing = 6;

            _communityCards = CommunityCardsView.Create(centerContent.transform, tableTheme);
            _communityCards.AnimController = _animController;

            // Wire community card clicks for preview
            if (_communityCards.Cards != null)
            {
                foreach (var card in _communityCards.Cards)
                    card.OnCardClicked += cardStr => _cardPreview?.ShowCard(cardStr);
            }

            _hud.CreatePot(centerContent.transform);

            // ── Seats container (stretch-fill, no layout group) ─────────
            var seatsContainer = new GameObject("SeatsContainer", typeof(RectTransform));
            seatsContainer.transform.SetParent(gameArea.transform, false);
            var seatsContainerRt = seatsContainer.GetComponent<RectTransform>();
            UIFactory.StretchFill(seatsContainerRt);

            // Create seats positioned via anchor points on an ellipse
            _seats = new SeatView[LayoutConfig.MaxSeats + 1]; // index 0 unused
            for (int i = 1; i <= LayoutConfig.MaxSeats; i++)
            {
                _seats[i] = SeatView.Create(i, seatsContainer.transform, tableTheme);
                var seatRt = _seats[i].RectTransform;
                var anchor = LayoutConfig.GetSeatAnchor(i);
                seatRt.anchorMin = anchor;
                seatRt.anchorMax = anchor;
                seatRt.pivot = new Vector2(0.5f, 0.5f);
                seatRt.anchoredPosition = Vector2.zero;
                seatRt.sizeDelta = LayoutConfig.SeatSize;
            }

            for (int i = 1; i <= LayoutConfig.MaxSeats; i++)
            {
                _seats[i].AnimController = _animController;
                // Wire card click events for preview
                int seatIdx = i;
                _seats[i].Card1.OnCardClicked += cardStr => _cardPreview?.ShowCard(cardStr);
                _seats[i].Card2.OnCardClicked += cardStr => _cardPreview?.ShowCard(cardStr);
                // Wire seat tap for stats tooltip
                _seats[i].OnSeatTapped += (seat, pos) => _statsTooltip?.ShowForSeat(seat, pos);
            }

            // ── Row 2: Controls (fixed height) ─────────────────────────
            _controls = ControlsView.Create(mainGrid.transform);
            _controls.SetActiveTable(_tableId);
            _controls.OnNextStep += HandleNextStep;
            _controls.OnReset += HandleReset;
            _controls.OnAutoPlayToggle += HandleAutoPlayToggle;
            _controls.OnTableConnect += HandleTableConnect;
            _controls.OnBackToLobby += HandleBackToLobby;
            _controls.OnHelpRequested += () => _helpPopup?.Show();

            // Connection status (overlaid top-left of safe area)
            _connectionStatus = ConnectionStatusView.Create(safeAreaRt);

            // Hand history (overlaid right side of safe area)
            _handHistory = HandHistoryView.Create(safeAreaRt);

            // Session stats panel (overlaid left side)
            _sessionStatsPanel = SessionStatsPanel.Create(safeAreaRt, _animController,
                _sessionTracker, _playerProfiler);

            // Card preview overlay (full-screen, above everything)
            _cardPreview = CardPreviewOverlay.Create(safeAreaRt, _animController);

            // Showdown overlay (full-screen, above card preview)
            _showdownOverlay = ShowdownOverlay.Create(safeAreaRt, _animController, tableTheme);
            _showdownOverlay.OnDismissed += HandleShowdownDismissed;

            // Help popup (full-screen, above showdown)
            _helpPopup = HelpPopupView.Create(safeAreaRt, _animController);

            // Player stats tooltip (singleton, above seats)
            _statsTooltip = PlayerStatsTooltip.Create(safeAreaRt, _animController,
                _sessionTracker, _playerProfiler);

            // Loading overlay (above everything, blocks input while loading)
            _loadingOverlay = LoadingOverlay.Create(safeAreaRt, _animController);

            // Keyboard shortcuts (desktop/WebGL) — only create once
            if (_inputHandler == null)
            {
                _inputHandler = gameObject.AddComponent<InputHandler>();
                _inputHandler.OnNextStep += HandleNextStep;
                _inputHandler.OnReset += HandleReset;
                _inputHandler.OnAutoPlayToggle += HandleAutoPlayToggle;
                _inputHandler.OnSpeedCycle += () => _controls.CycleSpeed();
                _inputHandler.OnHandHistoryToggle += () => _handHistory.TogglePanel();
            }
        }


        // ── Connection ───────────────────────────────────────────────

        private void HandleConnectionStateChanged(ConnectionState state, string message)
        {
            _connectionStatus.UpdateStatus(state, message);
            _hud.SetStatus(
                state == ConnectionState.Disconnected || state == ConnectionState.Error
                    ? message : "");

            bool canInteract = _connectionManager.IsConnected;
            _controls.SetInteractable(canInteract && !_isProcessing);

            // Stop auto-play on disconnect
            if (!canInteract && _autoPlaying)
                StopAutoPlay();
        }

        /// <summary>
        /// Handles server-pushed WebSocket state updates outside of AdvanceStepAsync.
        /// Deduplicates by gameNo+step to avoid double-processing states already
        /// handled by the AdvanceStepAsync response.
        /// </summary>
        private void HandleWebSocketStatePush(TableResponse state)
        {
            if (state?.Game == null) return;

            var current = _stateManager?.CurrentState;
            int curGameNo = current?.Game?.GameNo ?? -1;
            int curStep = current?.Game?.HandStep ?? -1;
            int newGameNo = state.Game.GameNo;
            int newStep = state.Game.HandStep;

            // Skip if we already have this exact state
            if (newGameNo == curGameNo && newStep == curStep) return;
            // Skip if the pushed state is older than what we have
            if (newGameNo < curGameNo) return;
            if (newGameNo == curGameNo && newStep < curStep) return;

            _stateManager.UpdateState(state);
        }

        // ── State ────────────────────────────────────────────────────

        private void HandleStateChanged(TableResponse oldState, TableResponse newState)
        {
            int oldStep = oldState?.Game?.HandStep ?? -1;
            int newStep = newState?.Game?.HandStep ?? -1;
            int oldGameNo = oldState?.Game?.GameNo ?? -1;
            int newGameNo = newState?.Game?.GameNo ?? -1;

            bool isHandTransition = oldState != null && newGameNo > 0 && oldGameNo != newGameNo;
            bool isDealCards = oldState != null && oldStep < 4 && newStep >= 4
                && !isHandTransition;
            bool isFindWinners = oldState != null && oldStep < 14 && newStep >= 14;
            bool isPayWinners = oldState != null && oldStep < 15 && newStep >= 15;

            // Record hand end for session tracking
            if (isHandTransition && oldState != null)
            {
                _sessionTracker?.RecordHandEnd(oldState);
                _sessionStatsPanel?.UpdateStats();
            }

            // Record player actions for profiling
            if (_playerProfiler != null && oldState?.Players != null && newState?.Players != null)
            {
                foreach (var np in newState.Players)
                {
                    if (np.Seat < 1 || string.IsNullOrEmpty(np.Action)) continue;
                    // Find previous action for this seat
                    string prevAction = null;
                    foreach (var op in oldState.Players)
                    {
                        if (op.Seat == np.Seat) { prevAction = op.Action; break; }
                    }
                    // Only record if action changed
                    if (np.Action != prevAction)
                    {
                        bool isBlindPost = newStep <= 3; // SETUP_SMALL_BLIND, SETUP_BIG_BLIND
                        _playerProfiler.RecordAction(np.Seat, np.Action, newStep, isBlindPost);
                    }
                }
            }

            // Record hand results for profiling
            if (isHandTransition && _playerProfiler != null && oldState?.Players != null)
            {
                bool showdownReached = oldStep >= 12;
                bool flopReached = oldStep >= 6;
                foreach (var player in oldState.Players)
                {
                    if (player.Seat < 1) continue;
                    bool reachedShowdown = showdownReached && !player.IsFolded;
                    bool sawFlop = flopReached && !player.IsFolded;
                    _playerProfiler.RecordHandResult(player.Seat, reachedShowdown,
                        player.IsWinner, sawFlop);
                }
            }

            if (isFindWinners)
                AudioManager.Instance?.Play(SoundType.WinnerFanfare);

            // Defer stack tweens for winners before rendering (pot will fly in first)
            if (isPayWinners && newState?.Players != null)
            {
                foreach (var player in newState.Players)
                {
                    if (player.Seat < 1 || player.Seat > LayoutConfig.MaxSeats) continue;
                    if (player.IsWinner && player.Winnings > 0)
                        _seats[player.Seat].DeferNextStackTween();
                }
            }

            RenderState(newState);

            // Update phase indicator
            if (newState?.Game != null)
                _controls.SetPhase(newState.Game.HandStep);

            // Update atmosphere
            _tableView.Atmosphere?.ApplyPhase(newState?.Game);

            // Log to hand history
            _handHistory.LogStateChange(oldState, newState);

            // Generate narration with board texture analysis
            if (newState?.Game?.CommunityCards != null && newState.Game.CommunityCards.Count > 0)
            {
                var boardTexture = BoardTextureAnalyzer.Analyze(newState.Game.CommunityCards);
                var narration = _handNarrator?.GenerateNarration(
                    newState, oldState, _playerProfiler, boardTexture);
                if (!string.IsNullOrEmpty(narration))
                    _handHistory.LogNarration(narration);
            }

            // Chip fly animation when bets are collected into pot
            if (oldState?.Players != null && newState?.Players != null)
            {
                bool isCollectBets = false;
                foreach (var op in oldState.Players)
                {
                    if (op.Bet > 0)
                    {
                        foreach (var np in newState.Players)
                        {
                            if (np.Seat == op.Seat && np.Bet < 0.01f)
                            { isCollectBets = true; break; }
                        }
                        if (isCollectBets) break;
                    }
                }
                if (isCollectBets)
                    PlayChipFly(oldState, newState);
            }

            // Play pot-to-winner fly animations after render
            if (isPayWinners)
                PlayPotDistribution(oldState, newState);

            // Shuffle animation on hand transition
            if (isHandTransition)
            {
                Vector2 shuffleCenterPos = _communityCards != null
                    ? LayoutConfig.WorldToCanvasPos(_communityCards.GetComponent<RectTransform>())
                    : Vector2.zero;
                ShuffleAnimator.PlayShuffle(_animController, _canvasTransform,
                    _seats, oldState, newGameNo, shuffleCenterPos);

                // Dismiss showdown on hand transition
                if (_showdownOverlay != null && _showdownOverlay.IsVisible)
                    _showdownOverlay.Dismiss();
                _showdownPaused = false;
            }

            // Deal animation when cards are first dealt
            if (isDealCards)
            {
                Vector2 dealDeckPos = _communityCards != null
                    ? LayoutConfig.WorldToCanvasPos(_communityCards.GetComponent<RectTransform>())
                    : Vector2.zero;
                DealAnimator.PlayDeal(_animController, _canvasTransform,
                    newState.Game.DealerSeat, _seats, newState.Players, dealDeckPos);
            }

            // Show showdown overlay at step 13 (FIND_WINNERS)
            if (isFindWinners && _showdownOverlay != null)
            {
                if (_autoPlaying)
                {
                    float delay = _controls.CurrentSpeed * 3f;
                    _showdownOverlay.ShowAutoMode(newState, delay);
                }
                else
                {
                    _showdownOverlay.Show(newState);
                }
                _showdownPaused = true;
            }
        }

        private void RenderState(TableResponse state)
        {
            if (state == null) return;

            _hud.UpdateFromState(state);
            _communityCards.UpdateFromState(state);
            UpdateSeats(state);
        }

        private void UpdateSeats(TableResponse state)
        {
            if (state?.Game == null) return;

            var playerBySeat = new Dictionary<int, PlayerState>();
            if (state.Players != null)
            {
                foreach (var player in state.Players)
                {
                    if (player.Seat < 1 || player.Seat > LayoutConfig.MaxSeats) continue;
                    playerBySeat[player.Seat] = player;
                }
            }

            for (int i = 1; i <= LayoutConfig.MaxSeats; i++)
            {
                playerBySeat.TryGetValue(i, out var player);
                _seats[i].UpdateFromState(player, state.Game, false);

                // Update session delta
                if (player != null && _sessionTracker != null)
                    _seats[i].UpdateSessionDelta(_sessionTracker.GetDelta(i));

                // Update profile badge
                if (player != null && _playerProfiler != null)
                    _seats[i].UpdateProfileBadge(_playerProfiler.GetProfile(i));
            }
        }

        // ── Data Fetching ────────────────────────────────────────────

        private async Task FetchInitialState()
        {
            try
            {
                var state = await _connectionManager.GetTableStateAsync();
                if (state != null)
                {
                    _stateManager.UpdateState(state);
                    _controls.SetInteractable(true);
                }
                else
                {
                    _hud.SetStatus("Failed to fetch table state");
                    _stateManager.UpdateState(MockStateFactory.CreateMockState());
                    _controls.SetInteractable(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching state: {ex.Message}");
                _hud.SetStatus("Server unavailable - is Docker running?");
                _stateManager.UpdateState(MockStateFactory.CreateMockState());
                _controls.SetInteractable(true);
            }
        }

        // ── Step Processing ──────────────────────────────────────────

        private async void HandleNextStep()
        {
            // If showdown overlay is visible, dismiss it and advance
            if (_showdownPaused && _showdownOverlay != null && _showdownOverlay.IsVisible)
            {
                _showdownOverlay.Dismiss();
                _showdownPaused = false;
            }

            if (_isProcessing) return;
            await ProcessStep();
        }

        private async Task ProcessStep()
        {
            if (_isProcessing) return;

            _animController.CancelAll();
            ResetSeatContinuousTweens();

            _isProcessing = true;
            if (!_autoPlaying)
                _controls.SetInteractable(false);

            try
            {
                var state = await _connectionManager.AdvanceStepAsync();
                if (state != null)
                {
                    _stateManager.UpdateState(state);
                    _hud.SetStatus("");
                }
                else
                {
                    _hud.SetStatus("Failed to process step");
                    _handHistory.LogError("Failed to process step");
                    if (_autoPlaying) StopAutoPlay();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing step: {ex.Message}");
                _hud.SetStatus($"Error: {ex.Message}");
                _handHistory.LogError(ex.Message);
                if (_autoPlaying) StopAutoPlay();
            }
            finally
            {
                _isProcessing = false;
                if (!_autoPlaying)
                {
                    _controls.SetInteractable(_connectionManager.IsConnected);
                }
            }
        }

        private async void HandleReset()
        {
            if (_isProcessing) return;

            if (_autoPlaying)
                StopAutoPlay();

            _animController.CancelAll();
            ResetSeatContinuousTweens();

            _showdownPaused = false;
            _showdownOverlay?.Dismiss();

            _isProcessing = true;
            _controls.SetInteractable(false);

            try
            {
                // Capture the starting gameNo so we know when a new hand begins
                var currentState = _stateManager?.CurrentState;
                int startGameNo = currentState?.Game?.GameNo ?? 0;
                int step = currentState?.Game?.HandStep ?? 0;

                // If already at step 0 (GAME_PREP), just refresh — we're at a clean boundary
                if (step == 0 && startGameNo > 0)
                {
                    var state = await _connectionManager.GetTableStateAsync();
                    if (state != null)
                    {
                        _stateManager.UpdateState(state);
                        _hud.SetStatus("");
                    }
                    else
                    {
                        _hud.SetStatus("Failed to fetch table state");
                    }
                }
                else
                {
                    // Advance the server through remaining steps until a new hand starts
                    int maxAttempts = 32;
                    bool reachedNewHand = false;

                    for (int i = 0; i < maxAttempts; i++)
                    {
                        var state = await _connectionManager.AdvanceStepAsync();
                        if (state == null) break;

                        _stateManager.UpdateState(state);
                        int newStep = state.Game?.HandStep ?? 0;
                        int newGameNo = state.Game?.GameNo ?? 0;

                        // A new hand has started when gameNo increments and we're back at an early step
                        if (newGameNo > startGameNo && newStep <= 4)
                        {
                            reachedNewHand = true;
                            break;
                        }
                    }

                    _hud.SetStatus(reachedNewHand ? "" : "Advanced to next available state");
                }

                _handHistory.Clear();
                _tableContexts.Remove(_tableId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resetting: {ex.Message}");
                _hud.SetStatus($"Error: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                _controls.SetInteractable(_connectionManager.IsConnected);
            }
        }

        // ── Auto-Play ────────────────────────────────────────────────

        private void HandleAutoPlayToggle()
        {
            if (_autoPlaying)
                StopAutoPlay();
            else
                StartAutoPlay();
        }

        private Coroutine _autoPlayCoroutine;

        private void StartAutoPlay()
        {
            if (_autoPlaying || _isProcessing) return;
            _autoPlaying = true;
            _autoPlayStopRequested = false;
            _controls.SetAutoPlayActive(true);
            Tweener.SpeedMultiplier = Mathf.Max(1f, 1f / _controls.CurrentSpeed);
            _autoPlayCoroutine = StartCoroutine(AutoPlayLoop());
        }

        private IEnumerator AutoPlayLoop()
        {
            while (_autoPlaying && !_autoPlayStopRequested)
            {
                // Pause auto-play during showdown
                if (_showdownPaused)
                {
                    yield return null;
                    continue;
                }

                var stepTask = ProcessStep();
                while (!stepTask.IsCompleted) yield return null;

                if (!_autoPlaying || _autoPlayStopRequested) break;

                yield return new WaitForSeconds(_controls.CurrentSpeed);
            }

            _autoPlaying = false;
            _controls.SetAutoPlayActive(false);
        }

        private void StopAutoPlay()
        {
            _autoPlayStopRequested = true;
            _autoPlaying = false;
            Tweener.SpeedMultiplier = 1f;
            if (_autoPlayCoroutine != null)
            {
                StopCoroutine(_autoPlayCoroutine);
                _autoPlayCoroutine = null;
            }
            _controls.SetAutoPlayActive(false);
        }

        // ── Background Auto-Play ─────────────────────────────────────

        private static readonly float[] BgSpeedOptions = { 0.25f, 0.5f, 1.0f, 2.0f };

        private void StartBackgroundAutoPlay(int tableId, int speedIndex)
        {
            StopBackgroundAutoPlay(tableId);

            var bg = new BackgroundAutoPlay
            {
                TableId = tableId,
                SpeedIndex = speedIndex,
                ApiClient = gameObject.AddComponent<PokerApiClient>(),
            };
            bg.Coroutine = StartCoroutine(BackgroundAutoPlayLoop(bg));
            _backgroundAutoPlays[tableId] = bg;
        }

        private void StopBackgroundAutoPlay(int tableId)
        {
            if (!_backgroundAutoPlays.TryGetValue(tableId, out var bg)) return;
            if (bg.Coroutine != null) StopCoroutine(bg.Coroutine);
            if (bg.ApiClient != null) Destroy(bg.ApiClient);
            _backgroundAutoPlays.Remove(tableId);
        }

        private void StopAllBackgroundAutoPlay()
        {
            foreach (var bg in _backgroundAutoPlays.Values)
            {
                if (bg.Coroutine != null) StopCoroutine(bg.Coroutine);
                if (bg.ApiClient != null) Destroy(bg.ApiClient);
            }
            _backgroundAutoPlays.Clear();
        }

        public bool IsBackgroundAutoPlaying(int tableId) => _backgroundAutoPlays.ContainsKey(tableId);

        private IEnumerator BackgroundAutoPlayLoop(BackgroundAutoPlay bg)
        {
            float speed = (bg.SpeedIndex >= 0 && bg.SpeedIndex < BgSpeedOptions.Length)
                ? BgSpeedOptions[bg.SpeedIndex] : 1.0f;

            while (true)
            {
                var task = bg.ApiClient.ProcessStepAsync(bg.TableId);
                while (!task.IsCompleted) yield return null;

                if (task.IsFaulted || task.Result == null)
                {
                    bg.ConsecutiveErrors++;
                    if (bg.ConsecutiveErrors >= 5)
                    {
                        Debug.LogWarning($"Background auto-play for table {bg.TableId} stopped after 5 consecutive errors.");
                        if (_tableContexts.TryGetValue(bg.TableId, out var ctx))
                            ctx.WasAutoPlaying = false;
                        // Self-remove (can't call StopBackgroundAutoPlay from inside coroutine cleanly)
                        if (bg.ApiClient != null) Destroy(bg.ApiClient);
                        _backgroundAutoPlays.Remove(bg.TableId);
                        yield break;
                    }
                    yield return new WaitForSeconds(2f);
                }
                else
                {
                    bg.ConsecutiveErrors = 0;
                    yield return new WaitForSeconds(speed);
                }
            }
        }

        private void SaveCurrentContext()
        {
            var ctx = new TableContext
            {
                TableId = _tableId,
                WasAutoPlaying = _autoPlaying,
                SpeedIndex = _controls.GetSpeedIndex(),
                SessionTracker = _sessionTracker,
                PlayerProfiler = _playerProfiler,
                HandHistoryEntries = _handHistory.ExportEntries(),
                LastGameNo = _handHistory.ExportLastGameNo(),
                StartOfHandStacks = _handHistory.ExportStartStacks(),
                PrevPlayerActions = _handHistory.ExportPrevActions(),
                LastTableState = _stateManager?.CurrentState,
            };
            _tableContexts[_tableId] = ctx;
        }

        private void RestoreContext(int tableId)
        {
            if (_tableContexts.TryGetValue(tableId, out var ctx))
            {
                _stateManager.SetStateSilently(ctx.LastTableState);
                _sessionTracker = ctx.SessionTracker;
                _playerProfiler = ctx.PlayerProfiler ?? new PlayerProfiler();
                _handHistory.ImportEntries(ctx.HandHistoryEntries, ctx.LastGameNo,
                    ctx.StartOfHandStacks, ctx.PrevPlayerActions);
                _controls.SetSpeedIndex(ctx.SpeedIndex);
            }
            else
            {
                _stateManager.SetStateSilently(null);
                _sessionTracker = new SessionTracker();
                _playerProfiler = new PlayerProfiler();
                _handHistory.Clear();
            }

            _statsTooltip?.UpdateTracker(_sessionTracker);
            _statsTooltip?.UpdateProfiler(_playerProfiler);
        }

        private async void HandleTableConnect(int tableId)
        {
            if (_isProcessing || _isTableTransitioning) return;
            if (tableId == _tableId) return;

            _isTableTransitioning = true;
            SaveCurrentContext();

            if (_autoPlaying)
                StopAutoPlay();

            _animController.CancelAll();
            ResetSeatContinuousTweens();

            // Check if the target table had auto-play running before we restore
            bool targetWasAutoPlaying = _tableContexts.TryGetValue(tableId, out var targetCtx)
                && targetCtx.WasAutoPlaying;

            int oldTableId = _tableId;
            _tableId = tableId;

            // Slide direction: higher table ID → content exits left
            bool slideLeft = tableId > oldTableId;
            float slideWidth = _canvasTransform != null
                ? Mathf.Max(_canvasTransform.rect.width, 400f)
                : Screen.width;
            float exitX = slideLeft ? -slideWidth : slideWidth;
            float entryX = slideLeft ? slideWidth : -slideWidth;

            // Phase 1: slide out old content
            if (_canvasTransform != null)
            {
                var oldWrapper = CreateSlideWrapper(_canvasTransform);
                await AwaitTween(Tweener.TweenPosition(
                    oldWrapper, Vector2.zero, new Vector2(exitX, 0),
                    0.2f, EaseType.EaseInCubic));
            }

            // Phase 2: rebuild UI with new table
            if (_canvasGo != null) Destroy(_canvasGo);
            RestoreContext(tableId);
            LayoutConfig.ResetOrientationCache();
            BuildUI();

            var currentState = _stateManager?.CurrentState;
            if (currentState != null)
                RenderState(currentState);

            // Phase 3: slide in new content
            if (_canvasTransform != null)
            {
                var newWrapper = CreateSlideWrapper(_canvasTransform);
                newWrapper.anchoredPosition = new Vector2(entryX, 0);

                await AwaitTween(Tweener.TweenPosition(
                    newWrapper, new Vector2(entryX, 0), Vector2.zero,
                    0.25f, EaseType.EaseOutQuart));

                UnwrapSlideContainer(newWrapper);
            }

            _isTableTransitioning = false;

            // Phase 4: connect to new table
            _controls.SetInteractable(false);

            try
            {
                await _connectionManager.SwitchTableAsync(tableId);
                await FetchInitialState();

                if (targetWasAutoPlaying)
                    StartAutoPlay();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error switching table: {ex.Message}");
                _hud.SetStatus($"Error: {ex.Message}");
                _controls.SetInteractable(_connectionManager.IsConnected);
            }
        }

        /// <summary>
        /// Awaits a TweenHandle, handling both normal completion and cancellation.
        /// </summary>
        private Task AwaitTween(TweenHandle tween)
        {
            var tcs = new TaskCompletionSource<bool>();
            tween.OnComplete(() => tcs.TrySetResult(true));
            var originalSnap = tween.SnapToFinal;
            tween.SnapToFinal = () => { originalSnap?.Invoke(); tcs.TrySetResult(true); };
            _animController.Play(tween);
            return tcs.Task;
        }

        /// <summary>
        /// Creates a stretch-filled wrapper inside the given parent,
        /// reparenting all existing children into it for slide animation.
        /// </summary>
        private static RectTransform CreateSlideWrapper(Transform parent)
        {
            var wrapper = new GameObject("SlideWrapper", typeof(RectTransform));
            wrapper.transform.SetParent(parent, false);
            var wrapperRt = wrapper.GetComponent<RectTransform>();
            UIFactory.StretchFill(wrapperRt);

            var children = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != wrapper.transform)
                    children.Add(child);
            }

            foreach (var child in children)
                child.SetParent(wrapper.transform, false);

            return wrapperRt;
        }

        /// <summary>
        /// Removes a slide wrapper, reparenting its children back to the wrapper's parent.
        /// </summary>
        private static void UnwrapSlideContainer(RectTransform wrapper)
        {
            if (wrapper == null) return;
            var parent = wrapper.parent;

            var children = new List<Transform>();
            for (int i = 0; i < wrapper.childCount; i++)
                children.Add(wrapper.GetChild(i));

            foreach (var child in children)
                child.SetParent(parent, false);

            UnityEngine.Object.Destroy(wrapper.gameObject);
        }

        private void HandleBackToLobby()
        {
            if (_isProcessing || _isTableTransitioning) return;

            // Save context BEFORE stopping auto-play (captures WasAutoPlaying = true)
            SaveCurrentContext();
            bool wasAutoPlaying = _autoPlaying;
            int savedSpeedIndex = _controls?.GetSpeedIndex() ?? 2;

            // Stop foreground auto-play
            if (_autoPlaying)
                StopAutoPlay();

            // Start background auto-play if it was running
            if (wasAutoPlaying)
                StartBackgroundAutoPlay(_tableId, savedSpeedIndex);

            // Also resume background auto-play for other tables that were auto-playing
            foreach (var kvp in _tableContexts)
            {
                if (kvp.Key != _tableId && kvp.Value.WasAutoPlaying && !_backgroundAutoPlays.ContainsKey(kvp.Key))
                    StartBackgroundAutoPlay(kvp.Key, kvp.Value.SpeedIndex);
            }

            // Scene transition: fade to black → tear down → show lobby → fade from black
            var canvasRt = _canvasGo?.GetComponent<RectTransform>();
            if (canvasRt != null && _animController != null)
            {
                var transition = SceneTransition.Create(canvasRt, _animController);
                transition.FadeToBlack(0.3f, () =>
                {
                    TearDownGameUI();
                    ShowLobby();

                    // Fade from black on lobby canvas
                    var lobbyCanvasRt = _canvasGo?.GetComponent<RectTransform>();
                    if (lobbyCanvasRt != null)
                    {
                        var fadeIn = SceneTransition.Create(lobbyCanvasRt, _animController);
                        fadeIn.FadeFromBlack(0.3f, () => fadeIn.Cleanup());
                    }
                    transition.Cleanup();
                });
            }
            else
            {
                TearDownGameUI();
                ShowLobby();
            }
        }

        private void TearDownGameUI()
        {
            // Cancel animations
            _animController?.CancelAll();
            ResetSeatContinuousTweens();

            // Disconnect from server
            if (_connectionManager != null)
            {
                _connectionManager.OnConnectionStateChanged -= HandleConnectionStateChanged;
                _connectionManager.OnWebSocketStateReceived -= HandleWebSocketStatePush;
                Destroy(_connectionManager);
                _connectionManager = null;
            }

            // Destroy API client
            if (_apiClient != null)
            {
                Destroy(_apiClient);
                _apiClient = null;
            }

            // Destroy game canvas
            if (_canvasGo != null)
            {
                Destroy(_canvasGo);
                _canvasGo = null;
            }

            // Destroy input handler (has closures over views)
            if (_inputHandler != null)
            {
                Destroy(_inputHandler);
                _inputHandler = null;
            }

            // Clear view references
            _seats = null;
            _tableView = null;
            _communityCards = null;
            _hud = null;
            _controls = null;
            _connectionStatus = null;
            _handHistory = null;
            _cardPreview = null;
            _statsTooltip = null;
            _sessionStatsPanel = null;
            _helpPopup = null;
            _showdownOverlay = null;
            _showdownPaused = false;
            _loadingOverlay = null;
            _canvasTransform = null;

            // Do NOT clear _tableContexts — preserve them for re-entry

            // Reset state manager
            _stateManager?.SetStateSilently(null);
            _sessionTracker = new SessionTracker();
            _playerProfiler = new PlayerProfiler();
        }

        private void Update()
        {
            // Don't detect orientation changes during lobby or table transition
            if (_seats == null || _isTableTransitioning) return;

            // Detect runtime orientation change
            LayoutConfig.ResetOrientationCache();
            bool currentPortrait = LayoutConfig.IsPortrait;
            if (currentPortrait != _lastIsPortrait)
            {
                _lastIsPortrait = currentPortrait;
                RebuildUI();
            }
        }

        private void RebuildUI()
        {
            // Stop auto-play and cancel animations
            StopAutoPlay();
            _animController?.CancelAll();

            // Destroy existing canvas
            if (_canvasGo != null)
                Destroy(_canvasGo);

            LayoutConfig.ResetOrientationCache();
            BuildUI();

            // Re-render current state
            var currentState = _stateManager?.CurrentState;
            if (currentState != null)
                RenderState(currentState);
        }

        private void OnDestroy()
        {
            StopAutoPlay();
            StopAllBackgroundAutoPlay();
            _animController?.CancelAll();
            if (_stateManager != null)
                _stateManager.OnStateChanged -= HandleStateChanged;
            if (_connectionManager != null)
            {
                _connectionManager.OnConnectionStateChanged -= HandleConnectionStateChanged;
                _connectionManager.OnWebSocketStateReceived -= HandleWebSocketStatePush;
            }
            if (_instance == this)
                _instance = null;
        }

        private void ResetSeatContinuousTweens()
        {
            if (_seats == null) return;
            for (int i = 1; i <= LayoutConfig.MaxSeats; i++)
                _seats[i].ResetContinuousTweens();
        }

        private void HandleShowdownDismissed()
        {
            _showdownPaused = false;
        }

        // ── Table Reset ──────────────────────────────────────────────

        /// <summary>
        /// On first join, if mid-hand (step > 4 and < 15), advance to a clean state.
        /// </summary>
        private async System.Threading.Tasks.Task ResetToCleanState()
        {
            var current = _stateManager?.CurrentState;
            if (current?.Game == null) return;

            int step = current.Game.HandStep;
            if (step <= 4 || step >= 15) return;

            // Loop POST /process until we reach a clean state
            int maxAttempts = 20; // Safety limit
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var state = await _connectionManager.AdvanceStepAsync();
                    if (state == null) break;

                    _stateManager.UpdateState(state);
                    step = state.Game?.HandStep ?? 0;

                    // Clean state: hand complete or early in new hand
                    if (step <= 4 || step >= 15)
                        break;
                }
                catch
                {
                    break;
                }
            }
        }

        // ── Pot Distribution (delegated to PotDistributionAnimator) ──

        private void PlayPotDistribution(TableResponse oldState, TableResponse newState)
        {
            Vector2 potPos = _hud != null && _hud.PotTransform != null
                ? LayoutConfig.WorldToCanvasPos(_hud.PotTransform)
                : Vector2.zero;
            PotDistributionAnimator.PlayPotDistribution(
                _animController, _canvasTransform, _seats, oldState, newState, potPos);
        }

        private void PlayChipFly(TableResponse oldState, TableResponse newState)
        {
            Vector2 potPos = _hud != null && _hud.PotTransform != null
                ? LayoutConfig.WorldToCanvasPos(_hud.PotTransform)
                : Vector2.zero;
            ChipFlyAnimator.PlayChipFly(
                _animController, _canvasTransform, _seats, oldState, newState, potPos);
        }
    }
}
