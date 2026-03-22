using UnityEngine;

namespace HijackPoker.UI
{
    /// <summary>
    /// Generates 5x5 symmetric grid identicon avatars from player IDs.
    /// Each player gets a unique, deterministic pattern rendered inside a circle.
    /// </summary>
    public static class AvatarPatternGenerator
    {
        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _cache = new();

        /// <summary>
        /// Deterministic 32-bit hash using Knuth multiplicative mixing.
        /// </summary>
        public static int HashPlayerId(int playerId)
        {
            unchecked
            {
                return (int)((uint)playerId * 2654435761u) ^ (playerId * 40503);
            }
        }

        /// <summary>
        /// Derives a secondary color by shifting the hue of the primary color
        /// by +5-20% based on hash bits.
        /// </summary>
        public static Color DeriveSecondaryColor(Color primary, int hash)
        {
            Color.RGBToHSV(primary, out float h, out float s, out float v);

            // Use bits 20-24 of hash to get a shift in range [5%, 20%]
            int bits = (hash >> 20) & 0xF; // 0-15
            float shift = 0.05f + (bits / 15f) * 0.15f; // 0.05 to 0.20

            h = (h + shift) % 1f;
            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>
        /// Derives a darkened background color at ~30% luminance from the primary.
        /// </summary>
        public static Color DeriveBackgroundColor(Color primary)
        {
            Color.RGBToHSV(primary, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Min(s * 1.1f, 1f), 0.3f);
        }

        public static Sprite Generate(int playerId, int size, Color fgColor, Color bgColor)
        {
            if (_cache.TryGetValue(playerId, out var cached))
                return cached;

            int hash = HashPlayerId(playerId);
            Color secondaryColor = DeriveSecondaryColor(fgColor, hash);
            Color backgroundColor = DeriveBackgroundColor(fgColor);

            // Build 5x5 symmetric grid from 15 hash bits.
            // We only need columns 0-2 (3 columns x 5 rows = 15 cells).
            // Columns 3 and 4 mirror columns 1 and 0 respectively.
            bool[,] grid = new bool[5, 5];
            int bitIndex = 0;
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    bool filled = ((hash >> bitIndex) & 1) == 1;
                    grid[row, col] = filled;
                    // Mirror: col 0 -> col 4, col 1 -> col 3, col 2 stays center
                    grid[row, 4 - col] = filled;
                    bitIndex++;
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = center;

            // Grid layout: the 5x5 identicon is inscribed within the circle.
            // We use about 80% of the diameter for the grid area, centered.
            float gridExtent = size * 0.38f; // half-width of grid area
            float gridLeft = center - gridExtent;
            float gridTop = center - gridExtent;
            float cellSize = (gridExtent * 2f) / 5f;

            // Use bits 15-16 to decide if secondary color is used for some cells
            bool useSecondary = ((hash >> 16) & 1) == 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // Circle SDF with anti-aliasing
                    float dx = px - center;
                    float dy = py - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist);

                    if (alpha <= 0f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // Determine grid cell for this pixel
                    float gx = px - gridLeft;
                    float gy = py - gridTop;
                    int col = Mathf.FloorToInt(gx / cellSize);
                    int row = Mathf.FloorToInt(gy / cellSize);

                    bool isFilled = false;
                    if (col >= 0 && col < 5 && row >= 0 && row < 5)
                    {
                        isFilled = grid[row, col];
                    }

                    // Choose pixel color
                    Color cellColor;
                    if (isFilled)
                    {
                        // Alternate some filled cells with secondary color for visual interest
                        if (useSecondary && ((row + col) % 2 == 1))
                            cellColor = secondaryColor;
                        else
                            cellColor = fgColor;
                    }
                    else
                    {
                        cellColor = backgroundColor;
                    }

                    // Subtle radial gradient overlay for depth (10% opacity, darker at edges)
                    float normalizedDist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float gradientDarken = normalizedDist * 0.1f;
                    cellColor.r = Mathf.Max(0f, cellColor.r - gradientDarken);
                    cellColor.g = Mathf.Max(0f, cellColor.g - gradientDarken);
                    cellColor.b = Mathf.Max(0f, cellColor.b - gradientDarken);

                    cellColor.a *= alpha;
                    pixels[y * size + x] = cellColor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _cache[playerId] = sprite;
            return sprite;
        }

        /// <summary>
        /// Update avatar to show identicon circle with initial letter overlay.
        /// </summary>
        public static void UpdateIfChanged(UnityEngine.UI.Image avatarBg,
            TMPro.TextMeshProUGUI avatarText, int playerId, int size)
        {
            var fgColor = UIFactory.GetAvatarColor(playerId);

            avatarBg.sprite = Generate(playerId, size, fgColor, Color.clear);
            avatarBg.color = Color.white;

            if (avatarText != null)
            {
                avatarText.alpha = 1f;
                avatarText.color = Color.white;
                // Show first letter based on player ID (seat-based for spatial consistency)
                char letter = (char)('A' + (Mathf.Abs(playerId) % 26));
                avatarText.text = letter.ToString();
            }
        }

        public static void ClearCache()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value != null)
                {
                    var tex = kvp.Value.texture;
                    Object.Destroy(kvp.Value);
                    if (tex != null) Object.Destroy(tex);
                }
            }
            _cache.Clear();
        }
    }
}
