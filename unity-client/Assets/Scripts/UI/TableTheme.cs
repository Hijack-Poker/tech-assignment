using UnityEngine;

namespace HijackPoker.UI
{
    /// <summary>
    /// Per-table visual theme — felt colors, atmosphere tints, card back colors,
    /// and geometric identity. Each table (1–4) has a distinct identity.
    /// Reduced from 23+ fields to a focused set; derived values computed from base colors.
    /// </summary>
    public class TableTheme
    {
        public string Name;

        // Table surface
        public Color FeltBase;
        public Color FeltLight;

        // Border
        public Color RailBand;
        public Color RailHighlight;

        // Lobby card border glow
        public Color GlowBorder;

        // Atmosphere phase tints (boosted ~2x for dark bg visibility)
        public Color AtmoIdle;
        public Color AtmoBetting;
        public Color AtmoShowdown;
        public Color AtmoWinner;

        // Thematic accent
        public Color Accent;

        // Card back (3 fields replacing 8)
        public Color CardBackPrimary;
        public Color CardBackSecondary;
        public Color CardBackAccent;

        // Face-up tint
        public Color CardFaceTint;

        // Geometric identity
        public int TableCornerRadius;
        public float FeltNoiseContrast;
        public float FeltFiberAngle;

        // ── Derived properties (computed from base colors) ─────────

        public Color FeltHighlight => Color.Lerp(FeltBase, Color.white, 0.2f);
        public Color FeltHighlightEdge => Color.Lerp(FeltBase, Color.white, 0.3f);
        public Color PreviewFeltBase => new Color(FeltBase.r * 0.7f, FeltBase.g * 0.7f, FeltBase.b * 0.7f, 1f);
        public Color PreviewFeltHighlight => new Color(FeltLight.r, FeltLight.g, FeltLight.b, 0.5f);

        /// <summary>
        /// True when the card face tint is dark enough to need light text on cards.
        /// </summary>
        public bool IsDarkCardFace
        {
            get
            {
                float lum = CardFaceTint.r * 0.299f + CardFaceTint.g * 0.587f + CardFaceTint.b * 0.114f;
                return lum < 0.5f;
            }
        }

        public static TableTheme ForTable(int tableId)
        {
            switch (tableId)
            {
                case 2: return Sapphire();
                case 3: return Velvet();
                case 4: return Noir();
                default: return Classic();
            }
        }

        // T1 — The Classic: Emerald green (rounded, traditional)
        private static TableTheme Classic() => new TableTheme
        {
            Name = "The Classic",
            FeltBase = UIFactory.FeltColor,
            FeltLight = UIFactory.TableFeltLight,
            RailBand = UIFactory.RailBand,
            RailHighlight = UIFactory.RailHighlight,
            GlowBorder = new Color(0.23f, 0.51f, 0.96f, 0.12f),
            AtmoIdle = new Color(1f, 0.85f, 0.5f, 0.08f),
            AtmoBetting = new Color(1f, 0.88f, 0.55f, 0.12f),
            AtmoShowdown = new Color(0.6f, 0.7f, 0.85f, 0.16f),
            AtmoWinner = new Color(1f, 0.84f, 0.3f, 0.20f),
            Accent = UIFactory.AccentCyan,
            CardBackPrimary = UIFactory.HexColor("#1E40AF"),
            CardBackSecondary = UIFactory.HexColor("#3B82F6"),
            CardBackAccent = new Color(1f, 0.84f, 0f, 0.50f),
            CardFaceTint = Color.white,
            TableCornerRadius = 24,
            FeltNoiseContrast = 0.025f,
            FeltFiberAngle = 0f,
        };

        // T2 — The Sapphire: Deep navy / royal blue (slightly tighter)
        private static TableTheme Sapphire() => new TableTheme
        {
            Name = "The Sapphire",
            FeltBase = UIFactory.HexColor("#1A3A6B"),
            FeltLight = UIFactory.HexColor("#244A7A"),
            RailBand = UIFactory.HexColor("#0F2A4D", 0.9f),
            RailHighlight = UIFactory.HexColor("#3D85AF", 0.4f),
            GlowBorder = new Color(0.23f, 0.51f, 0.96f, 0.12f),
            AtmoIdle = new Color(0.5f, 0.7f, 1.0f, 0.08f),
            AtmoBetting = new Color(0.55f, 0.75f, 1.0f, 0.12f),
            AtmoShowdown = new Color(0.4f, 0.5f, 0.9f, 0.16f),
            AtmoWinner = new Color(0.3f, 0.7f, 1.0f, 0.20f),
            Accent = UIFactory.HexColor("#60A5FA"),
            CardBackPrimary = UIFactory.HexColor("#1A3A6B"),
            CardBackSecondary = UIFactory.HexColor("#4A7AB8"),
            CardBackAccent = new Color(0.85f, 0.9f, 1.0f, 0.50f),
            CardFaceTint = new Color(0.97f, 0.98f, 1.0f, 1.0f),
            TableCornerRadius = 16,
            FeltNoiseContrast = 0.02f,
            FeltFiberAngle = 15f,
        };

        // T3 — The Velvet: Rich burgundy / wine (rounder, softer)
        private static TableTheme Velvet() => new TableTheme
        {
            Name = "The Velvet",
            FeltBase = UIFactory.HexColor("#6B1A2A"),
            FeltLight = UIFactory.HexColor("#7A2438"),
            RailBand = UIFactory.HexColor("#4D0F1A", 0.9f),
            RailHighlight = UIFactory.HexColor("#AF3D5D", 0.4f),
            GlowBorder = new Color(0.96f, 0.23f, 0.40f, 0.12f),
            AtmoIdle = new Color(1.0f, 0.5f, 0.6f, 0.08f),
            AtmoBetting = new Color(1.0f, 0.55f, 0.65f, 0.12f),
            AtmoShowdown = new Color(0.85f, 0.5f, 0.6f, 0.16f),
            AtmoWinner = new Color(1.0f, 0.4f, 0.5f, 0.20f),
            Accent = UIFactory.HexColor("#EF4444"),
            CardBackPrimary = UIFactory.HexColor("#6B1A2A"),
            CardBackSecondary = UIFactory.HexColor("#B8424A"),
            CardBackAccent = new Color(1f, 0.84f, 0f, 0.50f),
            CardFaceTint = new Color(1.0f, 0.99f, 0.97f, 1.0f),
            TableCornerRadius = 28,
            FeltNoiseContrast = 0.03f,
            FeltFiberAngle = -10f,
        };

        // T4 — The Noir: Dark charcoal / slate with gold (near-square, angular)
        private static TableTheme Noir() => new TableTheme
        {
            Name = "The Noir",
            FeltBase = UIFactory.HexColor("#2A2A30"),
            FeltLight = UIFactory.HexColor("#38383F"),
            RailBand = UIFactory.HexColor("#1A1A20", 0.9f),
            RailHighlight = UIFactory.HexColor("#8B7D2A", 0.4f),
            GlowBorder = new Color(0.96f, 0.84f, 0.23f, 0.12f),
            AtmoIdle = new Color(1.0f, 0.9f, 0.5f, 0.08f),
            AtmoBetting = new Color(1.0f, 0.92f, 0.55f, 0.12f),
            AtmoShowdown = new Color(0.8f, 0.75f, 0.5f, 0.16f),
            AtmoWinner = new Color(1.0f, 0.84f, 0.3f, 0.20f),
            Accent = UIFactory.AccentGold,
            CardBackPrimary = UIFactory.HexColor("#1A1A20"),
            CardBackSecondary = UIFactory.HexColor("#38383F"),
            CardBackAccent = new Color(0.96f, 0.84f, 0.23f, 0.50f),
            CardFaceTint = UIFactory.HexColor("#1E1E2A"),
            TableCornerRadius = 8,
            FeltNoiseContrast = 0.02f,
            FeltFiberAngle = 90f,
        };
    }
}
