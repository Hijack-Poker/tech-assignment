using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    public static class UIFactory
    {
        // ── Color palette — Dark Confidence ─────────────────────────
        // Neutral scale (zinc-based dark)
        public static readonly Color Background = HexColor("#111114");
        public static readonly Color BackgroundBottom = HexColor("#0E0E11");
        public static readonly Color TableFelt = HexColor("#1B6B3A");
        public static readonly Color TableFeltLight = HexColor("#247A48");
        public static readonly Color TableBorder = HexColor("#0F4D2A", 0.8f);

        // Semantic accents
        public static readonly Color AccentCyan = HexColor("#3B82F6");
        public static readonly Color AccentMagenta = HexColor("#EF4444");
        public static readonly Color AccentGold = HexColor("#F5A623");
        public static readonly Color AccentGreen = HexColor("#10B981");

        // Card colors
        public static readonly Color CardFace = HexColor("#FFFCF7");
        public static readonly Color CardBack = HexColor("#1E40AF");
        public static readonly Color CardBackLight = HexColor("#3B82F6");
        public static readonly Color CardRed = HexColor("#DC2626");

        // Text hierarchy
        public static readonly Color TextPrimary = HexColor("#E4E4E8");
        public static readonly Color TextSecondary = HexColor("#8B8B96");
        public static readonly Color TextMuted = HexColor("#52525B");
        public static readonly Color TextBright = HexColor("#FAFAFA");

        // Actions
        public static readonly Color ActionCheck = HexColor("#10B981");
        public static readonly Color ActionCall = HexColor("#3B82F6");
        public static readonly Color ActionBet = HexColor("#F5A623");
        public static readonly Color ActionFold = HexColor("#6B7280");
        public static readonly Color ActionAllIn = HexColor("#EF4444");
        public static readonly Color Transparent = new Color(0, 0, 0, 0);

        // ── Extended palette (dark surfaces) ─────────────────────────
        public static readonly Color PanelDark = HexColor("#1A1A1E", 0.92f);
        public static readonly Color PanelDarkSolid = HexColor("#1A1A1E");
        public static readonly Color ButtonDefault = HexColor("#252529");
        public static readonly Color ButtonDefaultBottom = HexColor("#1F1F23");
        public static readonly Color ControlsBarBg = HexColor("#1A1A1E", 0.92f);
        public static readonly Color StatusBarBg = HexColor("#1A1A1E", 0.70f);
        public static readonly Color SubtleBorder = HexColor("#2A2A30");
        public static readonly Color TableGlowColor = new Color(0f, 0f, 0f, 0.10f);
        public static readonly Color TableBorderColor = HexColor("#0F4D2A", 0.8f);
        public static readonly Color FeltColor = HexColor("#1B6B3A");
        public static readonly Color FeltHighlight = HexColor("#2E9A56");
        public static readonly Color FeltHighlightEdge = HexColor("#3DAF65");
        public static readonly Color GradientHint = HexColor("#1A1A1E");
        public static readonly Color CardBgFaceUp = HexColor("#FFFCF7");
        public static readonly Color CardInnerBg = HexColor("#FFF9F0");
        public static readonly Color CardEdgeHighlight = new Color(1, 1, 1, 0.04f);
        public static readonly Color CardBlack = HexColor("#0F0F14");
        public static readonly Color CardPlaceholderOutline = new Color(1, 1, 1, 0.20f);
        public static readonly Color CardPlaceholderFill = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color CardBackOverlay = HexColor("#1E40AF");
        public static readonly Color CardBackInner = HexColor("#3B82F6");
        public static readonly Color CardBackCenter = HexColor("#1E3A8A");
        public static readonly Color CardBackBorder = new Color(1f, 0.84f, 0f, 0.50f);
        public static readonly Color AvatarRing = new Color(1, 1, 1, 0.20f);
        public static readonly Color ActiveGlowCyan = HexColor("#3B82F6");
        public static readonly Color PositionBadgeBg = HexColor("#252529", 0.95f);
        public static readonly Color AutoPlayActiveBg = HexColor("#0D3320");
        public static readonly Color InputFieldBg = HexColor("#252529");
        public static readonly Color HeaderBarBg = HexColor("#1A1A1E", 0.98f);
        public static readonly Color ToggleBg = HexColor("#252529", 0.9f);
        public static readonly Color ShimmerColor = new Color(0.23f, 0.51f, 0.96f, 0.12f);
        public static readonly Color SeparatorColor = new Color(1, 1, 1, 0.08f);
        public static readonly Color GroupPillBg = HexColor("#252529", 0.5f);
        public static readonly Color FoldedTextColor = new Color(0.4f, 0.42f, 0.46f, 1f);
        public static readonly Color WinnerGlowGold = HexColor("#F5A623");
        public static readonly Color ShadowColor = new Color(0, 0, 0, 0.20f);
        public static readonly Color ShadowDeep = new Color(0, 0, 0, 0.30f);

        // ── Toolbar colors ────────────────────────────────────────────
        public static readonly Color ToolbarBg = HexColor("#1A1A1E", 0.95f);
        public static readonly Color ToolbarDivider = new Color(1, 1, 1, 0.10f);

        // ── Atmosphere colors (boosted ~2x for dark bg visibility) ───
        public static readonly Color AtmoIdle = new Color(1f, 0.85f, 0.5f, 0.08f);
        public static readonly Color AtmoBetting = new Color(1f, 0.88f, 0.55f, 0.12f);
        public static readonly Color AtmoShowdown = new Color(0.6f, 0.7f, 0.85f, 0.16f);
        public static readonly Color AtmoWinner = new Color(1f, 0.84f, 0.3f, 0.20f);

        // ── Table rail colors ──────────────────────────────────────
        public static readonly Color RailBand = HexColor("#0F4D2A", 0.9f);
        public static readonly Color RailHighlight = HexColor("#3DAF65", 0.4f);

        // ── Four-color suit mode ──────────────────────────────────
        public static bool FourColorSuits { get; set; } = false;
        public static readonly Color SuitSpades = HexColor("#0F0F14");
        public static readonly Color SuitHearts = HexColor("#DC2626");
        public static readonly Color SuitDiamonds = HexColor("#E87C0B");
        public static readonly Color SuitClubs = HexColor("#2563EB");

        // ── Avatar colors (vibrant, pastel-meets-saturated) ─────────
        private static readonly Color[] AvatarColors =
        {
            HexColor("#F87171"), HexColor("#60A5FA"), HexColor("#34D399"),
            HexColor("#FBBF24"), HexColor("#A78BFA"), HexColor("#2DD4BF"),
            HexColor("#FB923C"), HexColor("#38BDF8"), HexColor("#4ADE80"),
            HexColor("#F472B6")
        };

        public static Color GetAvatarColor(int playerId)
        {
            return AvatarColors[Mathf.Abs(playerId) % AvatarColors.Length];
        }

        // ── Factory methods ──────────────────────────────────────────

        public static RectTransform CreatePanel(string name, Transform parent,
            Color? color = null, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            if (color.HasValue)
            {
                var img = go.AddComponent<Image>();
                img.color = color.Value;
            }

            if (size.HasValue)
                rt.sizeDelta = size.Value;

            return rt;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent,
            string text = "", float fontSize = 16f, Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color ?? TextPrimary;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            var font = FontManager.Regular;
            if (font != null) tmp.font = font;

            return tmp;
        }

        public static Button CreateButton(string name, Transform parent,
            string label, float fontSize = 18f, Color? bgColor = null,
            Color? textColor = null, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size ?? new Vector2(160, 44);

            var img = go.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.90f, 0.91f, 0.93f, 1f);
            img.sprite = TextureGenerator.GetRoundedRect(64, 44, 10);
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            btn.onClick.AddListener(() => AudioManager.Instance?.Play(SoundType.ButtonClick));

            var tmp = CreateText("Label", go.transform, label, fontSize,
                textColor ?? TextPrimary);
            var tmpRt = tmp.GetComponent<RectTransform>();
            tmpRt.anchorMin = Vector2.zero;
            tmpRt.anchorMax = Vector2.one;
            tmpRt.sizeDelta = Vector2.zero;
            tmp.raycastTarget = false;

            return btn;
        }

        public static RectTransform CreatePillGroup(string name, Transform parent,
            float height, float spacing = 2f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = GroupPillBg;
            img.sprite = TextureGenerator.GetRoundedRect(64, 44, 14);
            img.type = Image.Type.Sliced;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = new RectOffset(2, 2, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            return rt;
        }

        public static Image CreateImage(string name, Transform parent,
            Color? color = null, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (size.HasValue)
                rt.sizeDelta = size.Value;

            var img = go.AddComponent<Image>();
            img.color = color ?? Color.white;
            img.raycastTarget = false;

            return img;
        }

        /// <summary>
        /// Sets RectTransform anchors to a single point and positions via anchoredPosition.
        /// </summary>
        public static void SetAnchor(RectTransform rt, float anchorX, float anchorY,
            Vector2? pivot = null)
        {
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Stretch RectTransform to fill parent with optional padding.
        /// </summary>
        public static void StretchFill(RectTransform rt, float padding = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        public static string GetInitials(string username)
        {
            if (string.IsNullOrEmpty(username)) return "?";
            var parts = username.Split(' ');
            if (parts.Length >= 2)
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            return username.Length >= 2
                ? $"{char.ToUpper(username[0])}{char.ToUpper(username[1])}"
                : $"{char.ToUpper(username[0])}";
        }

        // ── Toolbar factory helpers ────────────────────────────────────

        /// <summary>
        /// Icon button with optional text label. HLG layout: [icon] [label?].
        /// Returns the Button; the icon Image is the first child.
        /// </summary>
        public static Button CreateIconButton(string name, Transform parent,
            Sprite icon, Color iconColor, int iconSize,
            string label = null, float labelFontSize = 12f,
            Color? bgColor = null, Color? labelColor = null, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (size.HasValue) rt.sizeDelta = size.Value;

            var bgImg = go.AddComponent<Image>();
            bgImg.color = bgColor ?? Transparent;
            bgImg.sprite = TextureGenerator.GetRoundedRect(64, 34, 8);
            bgImg.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;
            btn.onClick.AddListener(() => AudioManager.Instance?.Play(SoundType.ButtonClick));

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(6, 6, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Icon
            var iconImg = CreateImage("Icon", go.transform, iconColor, new Vector2(iconSize, iconSize));
            iconImg.sprite = icon;
            iconImg.raycastTarget = false;

            // Optional label
            if (!string.IsNullOrEmpty(label))
            {
                var tmp = CreateText("Label", go.transform, label, labelFontSize,
                    labelColor ?? TextPrimary);
                tmp.raycastTarget = false;
                var tmpRt = tmp.GetComponent<RectTransform>();
                tmpRt.sizeDelta = new Vector2(tmp.preferredWidth + 4, iconSize);
            }

            return btn;
        }

        /// <summary>
        /// 1px vertical divider line between toolbar segments.
        /// </summary>
        public static Image CreateVerticalDivider(string name, Transform parent,
            float height, Color? color = null)
        {
            var img = CreateImage(name, parent, color ?? ToolbarDivider,
                new Vector2(1, height));
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 1;
            le.preferredHeight = height;
            le.flexibleWidth = 0;
            return img;
        }

        // ── Helpers ──────────────────────────────────────────────────

        public static string ColorToHex(Color c) => $"#{ColorUtility.ToHtmlStringRGB(c)}";

        public static Color GetActionColor(string action)
        {
            return (action?.ToLower()) switch
            {
                "check" => ActionCheck,
                "call" => ActionCall,
                "bet" or "raise" => ActionBet,
                "fold" => ActionFold,
                "allin" => ActionAllIn,
                _ => TextSecondary
            };
        }

        public static Color HexColor(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Returns the color for a suit character, respecting 4-color mode.
        /// </summary>
        public static Color GetSuitColor(char suit)
        {
            if (FourColorSuits)
            {
                return suit switch
                {
                    '♠' => SuitSpades,
                    '♥' => SuitHearts,
                    '♦' => SuitDiamonds,
                    '♣' => SuitClubs,
                    _ => CardBlack
                };
            }
            bool isRed = suit == '♥' || suit == '♦';
            return isRed ? CardRed : CardBlack;
        }
    }
}
