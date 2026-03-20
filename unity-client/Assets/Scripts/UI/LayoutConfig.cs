using UnityEngine;

namespace HijackPoker.UI
{
    /// <summary>
    /// Centralized responsive layout values. Detects portrait/landscape at startup
    /// and provides sizing and spacing for all UI elements.
    /// Uses layout groups for positioning — no absolute anchor coordinates.
    /// </summary>
    public static class LayoutConfig
    {
        public const int MaxSeats = 6;

        private static bool? _isPortrait;
        private static RectTransform _contentRoot;

        /// <summary>
        /// Detects portrait vs landscape. On iOS, Screen.orientation and
        /// Input.deviceOrientation are checked first since Screen dimensions
        /// may still reflect portrait during early frames before autorotation.
        /// </summary>
        public static bool IsPortrait
        {
            get
            {
                if (_isPortrait.HasValue) return _isPortrait.Value;

                var o = Screen.orientation;
                if (o == ScreenOrientation.LandscapeLeft || o == ScreenOrientation.LandscapeRight)
                    _isPortrait = false;
                else if (o == ScreenOrientation.Portrait || o == ScreenOrientation.PortraitUpsideDown)
                    _isPortrait = true;
                else
                {
                    // AutoRotation — check physical device orientation
                    var d = Input.deviceOrientation;
                    if (d == DeviceOrientation.LandscapeLeft || d == DeviceOrientation.LandscapeRight)
                        _isPortrait = false;
                    else if (d == DeviceOrientation.Portrait || d == DeviceOrientation.PortraitUpsideDown)
                        _isPortrait = true;
                    else
                        _isPortrait = Screen.height > Screen.width;
                }

                return _isPortrait.Value;
            }
        }

        public static void ResetOrientationCache()
        {
            _isPortrait = null;
        }

        public static void SetContentRoot(RectTransform rt)
        {
            _contentRoot = rt;
        }

        // ── Canvas ────────────────────────────────────────────────────

        public static Vector2 ReferenceResolution => IsPortrait
            ? new Vector2(1080, 1920)
            : new Vector2(1920, 1080);

        public static float CanvasMatch => IsPortrait ? 0f : 0.5f;

        // ── Compact screen scaling ────────────────────────────────────
        // Scales key elements down on phones smaller than reference resolution.

        public static float CompactScale
        {
            get
            {
                float refShort = IsPortrait ? ReferenceResolution.x : ReferenceResolution.y;
                float actualShort = Mathf.Min(Screen.width, Screen.height);
                float scale = actualShort / refShort;
                return Mathf.Clamp(scale, 0.82f, 1.0f);
            }
        }

        // ── Main grid row heights ───────────────────────────────────
        // The main layout is a 3-row vertical grid:
        //   Row 0: HUD (fixed height)
        //   Row 1: Game area (flexible, fills remaining space)
        //   Row 2: Controls (fixed height)

        public static float HudRowHeight => IsPortrait ? 46f : 34f;

        // ── Game area ─────────────────────────────────────────────────
        // The game area uses anchor-based elliptical seat positioning.
        // Center content (community cards + pot) is anchored to the middle.

        // ── Table surface (anchor-range, already relative) ──────────
        // Bigger felt — covers more screen, seats overlap edges

        public static Vector2 TableSurfaceMin => IsPortrait
            ? new Vector2(0.03f, 0.18f) : new Vector2(0.15f, 0.16f);
        public static Vector2 TableSurfaceMax => IsPortrait
            ? new Vector2(0.97f, 0.80f) : new Vector2(0.85f, 0.84f);

        public static Vector2 TableGlowMin => IsPortrait
            ? new Vector2(0.01f, 0.16f) : new Vector2(0.13f, 0.14f);
        public static Vector2 TableGlowMax => IsPortrait
            ? new Vector2(0.99f, 0.82f) : new Vector2(0.87f, 0.86f);

        public static Vector2 GradientMin => IsPortrait
            ? new Vector2(0.01f, 0.12f) : new Vector2(0.11f, 0.12f);
        public static Vector2 GradientMax => IsPortrait
            ? new Vector2(0.99f, 0.88f) : new Vector2(0.89f, 0.88f);

        // ── Card size ─────────────────────────────────────────────────

        public static Vector2 CardSize
        {
            get
            {
                float s = CompactScale;
                return IsPortrait
                    ? new Vector2(78 * s, 110 * s)
                    : new Vector2(56 * s, 78 * s);
            }
        }

        // ── Controls (kept for BettingView / HandHistoryView compat) ─

        public static float ControlsBarHeight => IsPortrait ? 100f : 80f;
        public static float ControlsBarPadding => IsPortrait ? 12f : 40f;
        public static float ControlsButtonHeight => IsPortrait ? 42f : 36f;
        public static float ControlsSecondaryHeight => IsPortrait ? 36f : 36f;

        // ── Toolbar (compact floating control strip) ────────────────

        public static float ToolbarMaxWidth => IsPortrait ? 340f : 480f;
        public static float ToolbarHeight => 48f;
        public static int ToolbarCornerRadius => IsPortrait ? 18 : 16;
        public static int ToolbarIconSize => IsPortrait ? 18 : 16;
        public static float ToolbarTableBadgeSize => IsPortrait ? 18f : 16f;
        public static float ToolbarPrimaryBtnH => IsPortrait ? 36f : 32f;
        public static float ToolbarPrimaryBtnW => IsPortrait ? 120f : 110f;
        public static float ToolbarSecondaryBtnH => IsPortrait ? 30f : 28f;
        public static float ToolbarInnerPadding => IsPortrait ? 8f : 6f;

        // ── Lobby pill (top-left navigation) ─────────────────────────

        public static float LobbyPillHeight => 32f;
        public static int LobbyPillCornerRadius => 14;

        // ── Phase indicator (above dock) ─────────────────────────────

        public static float PhaseIndicatorHeight => 24f;
        public static float PhaseIndicatorWidth => IsPortrait ? 280f : 320f;
        public static float PhaseIndicatorFontSize => 11f;

        // ── Speed pill (standalone) ──────────────────────────────────

        public static float SpeedPillWidth => 44f;

        // ── Seat elements ─────────────────────────────────────────────

        public static Vector2 SeatSize
        {
            get
            {
                float s = CompactScale;
                return IsPortrait
                    ? new Vector2(168 * s, 276 * s)
                    : new Vector2(144 * s, 242 * s);
            }
        }

        public static float SeatCardSpacing => IsPortrait ? 14f : 11f;
        public static float SeatCardScale => IsPortrait ? 0.65f : 0.68f;

        public static float SeatElementSpacing => IsPortrait ? 6f : 4f;

        // Card fan
        public static float SeatCardRotationPadding => IsPortrait ? 36f : 30f;
        public static float SeatCardFanAngle => IsPortrait ? 8f : 6f;
        public static float SeatCardFanYOffset => IsPortrait ? 4f : 3f;

        // Avatar
        public static float AvatarSize
        {
            get
            {
                float s = CompactScale;
                return IsPortrait ? 52f * s : 44f * s;
            }
        }
        public static float AvatarRingSize => AvatarSize + 4f;
        public static float AvatarFontSize => IsPortrait ? 15f : 14f;

        // Info panel (name + stack backdrop)
        public static Vector2 SeatInfoSize
        {
            get
            {
                float s = CompactScale;
                return IsPortrait
                    ? new Vector2(146 * s, 50 * s)
                    : new Vector2(124 * s, 46 * s);
            }
        }
        public static float SeatInfoCornerRadius => IsPortrait ? 14f : 14f;
        public static float SeatNameFontSize => IsPortrait ? 14f : 13f;
        public static float SeatNameWidth => IsPortrait ? 146f : 120f;
        public static float SeatStackFontSize => IsPortrait ? 12f : 12f;
        public static RectOffset SeatPadding => IsPortrait
            ? new RectOffset(4, 4, 4, 2) : new RectOffset(3, 3, 3, 2);

        // Badges
        public static Vector2 SeatPositionBadgeSize => IsPortrait
            ? new Vector2(32, 20) : new Vector2(32, 18);
        public static Vector2 SeatActionBadgeSize => IsPortrait
            ? new Vector2(70, 22) : new Vector2(68, 22);
        public static Vector2 SeatAllInBadgeSize => IsPortrait
            ? new Vector2(62, 18) : new Vector2(60, 18);
        public static float SeatBetWidth => IsPortrait ? 100f : 90f;

        // ── Chip stack ───────────────────────────────────────────────

        public static float ChipDiameter => (IsPortrait ? 24f : 20f) * CompactScale;
        public static float ChipMaxHeight => (IsPortrait ? 44f : 36f) * CompactScale;
        public static float ChipOverlap => IsPortrait ? 5f : 4f;

        // ── Turn timer ──────────────────────────────────────────────

        public static float TimerDiameter => AvatarRingSize + 8f;
        public static float TimerDuration => 20f;

        // ── Session delta ───────────────────────────────────────────

        public static float SessionDeltaFontSize => IsPortrait ? 14f : 12f;
        public static float SessionDeltaRowHeight => IsPortrait ? 26f : 22f;

        // ── HUD elements ────────────────────────────────────────────

        public static Vector2 PhaseLabelContainerSize => IsPortrait
            ? new Vector2(360, 32) : new Vector2(400, 36);
        public static float PhaseFontSize => IsPortrait ? 22f : 24f;
        public static Vector2 PotBgSize => IsPortrait
            ? new Vector2(140, 26) : new Vector2(160, 28);
        public static float PotFontSize => IsPortrait ? 16f : 18f;
        public static float HandNumberWidth => IsPortrait ? 260f : 300f;
        public static float BlindsWidth => IsPortrait ? 200f : 220f;
        public static float StatusWidth => IsPortrait ? 420f : 500f;

        // ── Community cards ─────────────────────────────────────────

        public static float CommunityCardGap => IsPortrait ? 6f : 6f;
        public static float CommunityCardScale => IsPortrait ? 1.0f : 1.0f;

        // ── Showdown overlay ──────────────────────────────────────────

        public static float ShowdownRowHeight => IsPortrait ? 96f : 86f;
        public static float ShowdownCardScale => IsPortrait ? 0.75f : 0.75f;
        public static float ShowdownCardFanAngle => IsPortrait ? 8f : 6f;
        public static float ShowdownCardFanYOffset => IsPortrait ? 4f : 3f;
        public static float ShowdownCardSpacing => IsPortrait ? 12f : 10f;
        public static float ShowdownCardRotationPadding => IsPortrait ? 10f : 8f;
        public static float ShowdownCommunityCardScale => 0.55f;
        public static float ShowdownCommunityCardGap => 4f;
        public static float ShowdownCommunityRowHeight => IsPortrait ? 70f : 60f;

        // ── Betting UI ───────────────────────────────────────────────

        public static float BettingBarHeight => IsPortrait ? 80f : 60f;
        public static float BettingButtonSize => IsPortrait ? 38f : 34f;

        // ── Center content anchors ──────────────────────────────────

        public static Vector2 CenterContentMin => IsPortrait
            ? new Vector2(0.12f, 0.38f) : new Vector2(0.28f, 0.32f);
        public static Vector2 CenterContentMax => IsPortrait
            ? new Vector2(0.88f, 0.62f) : new Vector2(0.72f, 0.68f);

        // ── Seat anchor positions (elliptical around table) ────────
        // Each seat is placed at a point anchor on an ellipse centered
        // at (0.5, 0.5) within the game area overlay.

        // Ellipse: center (0.5,0.5), seats at 270°,210°,150°,90°,30°,330°
        // Landscape: rx=0.38, ry=0.36
        private static readonly Vector2[] _seatAnchorsLandscape =
        {
            Vector2.zero,                   // index 0 unused
            new Vector2(0.50f, 0.14f),      // Seat 1 — 270° bottom center
            new Vector2(0.17f, 0.32f),      // Seat 2 — 210° bottom-left
            new Vector2(0.17f, 0.68f),      // Seat 3 — 150° top-left
            new Vector2(0.50f, 0.86f),      // Seat 4 —  90° top center
            new Vector2(0.83f, 0.68f),      // Seat 5 —  30° top-right
            new Vector2(0.83f, 0.32f),      // Seat 6 — 330° bottom-right
        };

        // Portrait: rx=0.38, ry=0.30
        private static readonly Vector2[] _seatAnchorsPortrait =
        {
            Vector2.zero,                   // index 0 unused
            new Vector2(0.50f, 0.22f),      // Seat 1 — 270° bottom center
            new Vector2(0.20f, 0.36f),      // Seat 2 — 210° bottom-left
            new Vector2(0.20f, 0.64f),      // Seat 3 — 150° top-left
            new Vector2(0.50f, 0.78f),      // Seat 4 —  90° top center
            new Vector2(0.80f, 0.64f),      // Seat 5 —  30° top-right
            new Vector2(0.80f, 0.36f),      // Seat 6 — 330° bottom-right
        };

        public static Vector2 GetSeatAnchor(int seatNumber)
        {
            var anchors = IsPortrait ? _seatAnchorsPortrait : _seatAnchorsLandscape;
            if (seatNumber < 1 || seatNumber >= anchors.Length)
                return new Vector2(0.5f, 0.5f);
            return anchors[seatNumber];
        }

        // ── Runtime position helpers ────────────────────────────────
        // Animations need world-space positions of seats/pot/deck at runtime.
        // These are computed from the actual RectTransform positions rather
        // than precomputed anchor coordinates.

        /// <summary>
        /// Converts a RectTransform's world position to a local anchoredPosition
        /// relative to the content root (safe area). Used by animations to get
        /// the canvas-space position of layout-driven elements.
        /// </summary>
        public static Vector2 WorldToCanvasPos(RectTransform element)
        {
            if (_contentRoot == null || element == null) return Vector2.zero;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, element.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _contentRoot, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        /// <summary>
        /// Gets the canvas-space center position of the content root.
        /// Used as the "center of table" for deck/pot animations.
        /// </summary>
        public static Vector2 CanvasCenter => Vector2.zero;
    }
}
