using System.Collections.Generic;
using UnityEngine;

namespace HijackPoker.UI
{
    /// <summary>
    /// Generates procedural rounded-rect, circle, gradient, shadow, and pattern
    /// sprites at runtime. SDF-based pixel fill with anti-aliased edges.
    /// Results are cached and returned as 9-slice-ready Sprites.
    /// </summary>
    public static class TextureGenerator
    {
        private static readonly Dictionary<(int w, int h, int r, int p), Sprite> _cache = new();
        private static readonly Dictionary<string, Sprite> _namedCache = new();

        /// <summary>
        /// Returns a cached Sprite with rounded corners, suitable for Image.type = Sliced.
        /// Optional padding adds transparent pixels around the shape so the visible
        /// edge sits inside the quad — this eliminates aliased edges on rotated UI elements.
        /// </summary>
        public static Sprite GetRoundedRect(int width, int height, int cornerRadius, int padding = 0)
        {
            var key = (width, height, cornerRadius, padding);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            int texW = width + padding * 2;
            int texH = height + padding * 2;
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[texW * texH];
            int r = Mathf.Min(cornerRadius, Mathf.Min(width, height) / 2);

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f - padding, y + 0.5f - padding, width, height, r);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * texW + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            int border = r + padding;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, texW, texH),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));

            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Returns a cached circular Sprite.
        /// </summary>
        public static Sprite GetCircle(int diameter)
        {
            var key = (diameter, diameter, diameter / 2, 0);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[diameter * diameter];
            float center = diameter * 0.5f;
            float radius = center;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * diameter + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Vertical gradient fill with rounded corners. Top-to-bottom color blend.
        /// </summary>
        public static Sprite GetVerticalGradient(int w, int h, Color topColor, Color bottomColor, int cornerRadius, int padding = 0)
        {
            string key = $"vg_{w}_{h}_{topColor}_{bottomColor}_{cornerRadius}_{padding}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            int texW = w + padding * 2;
            int texH = h + padding * 2;
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[texW * texH];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);

            for (int y = 0; y < texH; y++)
            {
                float innerY = Mathf.Clamp(y - padding, 0, h - 1);
                float gradientT = innerY / (h - 1);
                Color rowColor = Color.Lerp(bottomColor, topColor, gradientT);

                for (int x = 0; x < texW; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f - padding, y + 0.5f - padding, w, h, r);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    Color c = rowColor;
                    c.a *= alpha;
                    pixels[y * texW + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            int borderVal = (cornerRadius > 0 ? r : 0) + padding;
            var border = borderVal > 0 ? new Vector4(borderVal, borderVal, borderVal, borderVal) : Vector4.zero;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, texW, texH),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Radial gradient: bright center fading to edge color. Useful for felt highlight,
        /// avatar backgrounds, and light pooling effects.
        /// </summary>
        public static Sprite GetRadialGradient(int diameter, Color centerColor, Color edgeColor)
        {
            string key = $"rg_{diameter}_{centerColor}_{edgeColor}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[diameter * diameter];
            float center = diameter * 0.5f;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    dist = Mathf.Clamp01(dist);
                    Color c = Color.Lerp(centerColor, edgeColor, dist);
                    pixels[y * diameter + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Soft shadow with rounded corners. Creates a blurred edge shadow texture
        /// for elevation effects. Shadow extends outward from the shape.
        /// </summary>
        public static Sprite GetSoftShadow(int w, int h, int cornerRadius, int shadowSize, float shadowAlpha)
        {
            string key = $"ss_{w}_{h}_{cornerRadius}_{shadowSize}_{shadowAlpha:F2}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            int totalW = w + shadowSize * 2;
            int totalH = h + shadowSize * 2;
            var tex = new Texture2D(totalW, totalH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[totalW * totalH];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);

            for (int y = 0; y < totalH; y++)
            {
                for (int x = 0; x < totalW; x++)
                {
                    // SDF relative to inner rounded rect centered in padded texture
                    float px = (x + 0.5f) - shadowSize;
                    float py = (y + 0.5f) - shadowSize;
                    float dist = RoundedRectSDF(px + 0.5f, py + 0.5f, w, h, r);

                    // Soft falloff outside the shape
                    float alpha;
                    if (dist <= 0)
                        alpha = shadowAlpha;
                    else
                        alpha = shadowAlpha * Mathf.Clamp01(1f - dist / shadowSize);

                    // Smooth the falloff
                    alpha *= alpha; // quadratic falloff for softness
                    pixels[y * totalW + x] = new Color32(0, 0, 0, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            int border = r + shadowSize;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, totalW, totalH),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Diamond/crosshatch pattern for card backs. Two-tone repeating diamond grid.
        /// </summary>
        public static Sprite GetDiamondPattern(int w, int h, int cornerRadius, Color bgColor, Color patternColor)
        {
            string key = $"dp_{w}_{h}_{cornerRadius}_{bgColor}_{patternColor}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);
            int diamondSize = 8;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f, y + 0.5f, w, h, r);
                    float alpha = Mathf.Clamp01(0.5f - dist);

                    // Diamond pattern: alternating based on (x+y) mod diamondSize
                    int dx = (x + y) % (diamondSize * 2);
                    int dy = (x - y + h * diamondSize) % (diamondSize * 2);
                    bool isDiamond = dx < diamondSize && dy < diamondSize;

                    Color c = isDiamond ? patternColor : bgColor;
                    c.a *= alpha;
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var border = cornerRadius > 0 ? new Vector4(r, r, r, r) : Vector4.zero;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Ring sprite for avatar glow rings and winner effects.
        /// </summary>
        public static Sprite GetRing(int outerDiameter, int thickness)
        {
            string key = $"ring_{outerDiameter}_{thickness}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(outerDiameter, outerDiameter, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[outerDiameter * outerDiameter];
            float center = outerDiameter * 0.5f;
            float outerR = center;
            float innerR = center - thickness;

            for (int y = 0; y < outerDiameter; y++)
            {
                for (int x = 0; x < outerDiameter; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float outerAlpha = Mathf.Clamp01(outerR - dist + 0.5f);
                    float innerAlpha = Mathf.Clamp01(dist - innerR + 0.5f);
                    float alpha = outerAlpha * innerAlpha;

                    pixels[y * outerDiameter + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, outerDiameter, outerDiameter),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Right-pointing filled triangle. Rotate the Image transform to change direction.
        /// </summary>
        public static Sprite GetTriangle(int size)
        {
            string key = $"tri_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float pad = size * 0.2f;
            Vector2 v0 = new Vector2(pad, pad);
            Vector2 v1 = new Vector2(size - pad, size * 0.5f);
            Vector2 v2 = new Vector2(pad, size - pad);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = TriangleSDF(p, v0, v1, v2);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Three horizontal bars icon (hamburger/list). White pixels, tint via Image.color.
        /// </summary>
        public static Sprite GetListIcon(int size)
        {
            string key = $"list_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float pad = size * 0.22f;
            float barH = size * 0.09f;
            float halfBar = barH * 0.5f;
            float gap = (size - 2f * pad - barH) * 0.5f;
            float centerY = size * 0.5f;
            float[] barCenters = { centerY, centerY - gap, centerY + gap };
            float barR = halfBar; // rounded end radius

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float best = float.MaxValue;

                    foreach (float cy in barCenters)
                    {
                        // SDF for a horizontal rounded-end bar
                        float dx = Mathf.Max(Mathf.Abs(px - size * 0.5f) - (size * 0.5f - pad - barR), 0f);
                        float dy = Mathf.Max(Mathf.Abs(py - cy) - halfBar + barR, 0f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy) - barR;
                        best = Mathf.Min(best, d);
                    }

                    float alpha = Mathf.Clamp01(0.5f - best);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Close "X" icon. Two diagonal strokes. White pixels, tint via Image.color.
        /// </summary>
        public static Sprite GetCloseIcon(int size)
        {
            string key = $"close_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float pad = size * 0.25f;
            float thickness = size * 0.09f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // Distance to line from (pad,pad) to (size-pad, size-pad)
                    float d1 = LineSDF(px, py, pad, pad, size - pad, size - pad);
                    // Distance to line from (size-pad, pad) to (pad, size-pad)
                    float d2 = LineSDF(px, py, size - pad, pad, pad, size - pad);

                    float dist = Mathf.Min(d1, d2) - thickness;
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Mandala-style card back with default geometry: 12-petal ring + 8-pointed star + hexagonal inner ring.
        /// </summary>
        public static Sprite GetMandalaBack(int w, int h, int cornerRadius, Color outerColor, Color innerColor, Color accentColor)
        {
            return GetMandalaBack(w, h, cornerRadius, outerColor, innerColor, accentColor, 6, 4, 6);
        }

        /// <summary>
        /// Mandala-style card back with configurable geometry.
        /// petalMultiplier: half the total petal count (6 = 12 petals).
        /// starSides: polygon sides for the star (4 = 8-pointed star).
        /// innerRingSides: polygon sides for the inner ring (6 = hexagon).
        /// </summary>
        public static Sprite GetMandalaBack(int w, int h, int cornerRadius,
            Color outerColor, Color innerColor, Color accentColor,
            int petalMultiplier, int starSides, int innerRingSides)
        {
            string key = $"mandala_{w}_{h}_{cornerRadius}_{outerColor}_{innerColor}_{accentColor}_{petalMultiplier}_{starSides}_{innerRingSides}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float maxR = Mathf.Min(w, h) * 0.42f;

            float starRotOffset = Mathf.PI / starSides;
            float innerRingRotOffset = Mathf.PI / innerRingSides;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f, y + 0.5f, w, h, r);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha <= 0) { pixels[y * w + x] = new Color32(0, 0, 0, 0); continue; }

                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Base: outer color
                    Color c = outerColor;

                    // Petal ring
                    float petalD = d / maxR;
                    float petal = Mathf.Abs(Mathf.Sin(angle * petalMultiplier));
                    float petalRing = Mathf.Abs(petalD - 0.7f - petal * 0.08f);
                    if (petalRing < 0.06f)
                        c = Color.Lerp(c, accentColor, (1f - petalRing / 0.06f) * 0.7f);

                    // Star (two rotated polygons)
                    float star1 = RegularPolygonSDF(dx, dy, maxR * 0.5f, starSides, 0f);
                    float star2 = RegularPolygonSDF(dx, dy, maxR * 0.5f, starSides, starRotOffset);
                    float starD = Mathf.Min(star1, star2);
                    if (starD < 1.5f)
                        c = Color.Lerp(accentColor, c, Mathf.Clamp01(starD / 1.5f));

                    // Inner ring
                    float innerPoly = RegularPolygonSDF(dx, dy, maxR * 0.32f, innerRingSides, innerRingRotOffset);
                    float innerRing = Mathf.Abs(innerPoly);
                    if (innerRing < 3f)
                        c = Color.Lerp(c, innerColor, (1f - innerRing / 3f) * 0.6f);

                    // Center diamond
                    float diamond = RegularPolygonSDF(dx, dy, maxR * 0.12f, 4, 0f);
                    if (diamond < 0f)
                        c = Color.Lerp(accentColor, innerColor, 0.5f);

                    c.a *= alpha;
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var border = cornerRadius > 0 ? new Vector4(r, r, r, r) : Vector4.zero;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Ring-shaped glow sprite for card edge effects.
        /// SDF computes distance to card outline and renders a soft glow band.
        /// </summary>
        public static Sprite GetGlowBorder(int w, int h, int cornerRadius, float thickness, float padding)
        {
            string key = $"gb_{w}_{h}_{cornerRadius}_{thickness:F1}_{padding:F1}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            int texW = w + (int)(padding * 2);
            int texH = h + (int)(padding * 2);
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[texW * texH];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    float px = x + 0.5f - padding;
                    float py = y + 0.5f - padding;
                    float dist = RoundedRectSDF(px + 0.5f, py + 0.5f, w, h, r);
                    float absDist = Mathf.Abs(dist);
                    float glow = Mathf.Clamp01(1f - absDist / thickness);
                    glow *= glow; // Quadratic falloff for softness
                    pixels[y * texW + x] = new Color32(255, 255, 255, (byte)(glow * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            int border = r + (int)padding;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, texW, texH),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Horizontal rainbow gradient strip with soft vertical falloff, for holographic shimmer.
        /// </summary>
        public static Sprite GetPrismaticStrip(int w, int h, float hueOffset)
        {
            string key = $"prism_{w}_{h}_{hueOffset:F2}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float vy = (y + 0.5f) / h;
                float vFalloff = 1f - Mathf.Abs(vy - 0.5f) * 2f;
                vFalloff = Mathf.Clamp01(vFalloff * vFalloff);

                for (int x = 0; x < w; x++)
                {
                    float hue = ((float)x / w + hueOffset) % 1f;
                    Color c = Color.HSVToRGB(hue, 0.5f, 1f);
                    c.a = vFalloff * 0.35f;
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Vignette effect: corners dark, center transparent.
        /// </summary>
        public static Sprite GetVignette(int w, int h, float strength)
        {
            string key = $"vig_{w}_{h}_{strength:F2}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float maxDist = Mathf.Sqrt(cx * cx + cy * cy);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                    float vignette = Mathf.Clamp01(dist * dist * strength);
                    pixels[y * w + x] = new Color32(0, 0, 0, (byte)(vignette * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Conic gradient ring texture for rotating avatar borders.
        /// </summary>
        public static Sprite GetConicRingGradient(int diameter, Color[] colors, float thickness)
        {
            string key = $"conic_{diameter}_{colors.Length}_{thickness:F1}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[diameter * diameter];
            float center = diameter * 0.5f;
            float outerR = center;
            float innerR = center - thickness;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float outerAlpha = Mathf.Clamp01(outerR - dist + 0.5f);
                    float innerAlpha = Mathf.Clamp01(dist - innerR + 0.5f);
                    float ringAlpha = outerAlpha * innerAlpha;

                    if (ringAlpha <= 0)
                    {
                        pixels[y * diameter + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float angle = Mathf.Atan2(dy, dx);
                    float t = (angle + Mathf.PI) / (2f * Mathf.PI); // 0..1
                    float idx = t * colors.Length;
                    int i0 = ((int)idx) % colors.Length;
                    int i1 = (i0 + 1) % colors.Length;
                    Color c = Color.Lerp(colors[i0], colors[i1], idx - (int)idx);
                    c.a *= ringAlpha;
                    pixels[y * diameter + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Chip sprite with concentric inner ring for chip stack visualization.
        /// SDF circle with a colored face and contrasting edge ring.
        /// </summary>
        public static Sprite GetChipTexture(int diameter, Color faceColor, Color edgeColor)
        {
            string key = $"chip_{diameter}_{faceColor}_{edgeColor}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[diameter * diameter];
            float center = diameter * 0.5f;
            float outerR = center;
            float edgeWidth = diameter * 0.15f;
            float faceRadius = outerR - edgeWidth;
            float innerRingR = center * 0.55f;
            float innerRingW = diameter * 0.06f;

            // Dome shading: pre-compute lightened / darkened face colors
            Color lightenedFace = Color.Lerp(faceColor, Color.white, 0.25f);
            Color darkenedFace = Color.Lerp(faceColor, Color.black, 0.15f);

            // Specular highlight center (top-left light source)
            float hlCenterX = -0.15f * diameter;
            float hlCenterY = 0.15f * diameter;
            float hlRadius = 0.35f * diameter;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Outer circle SDF
                    float outerAlpha = Mathf.Clamp01(outerR - dist + 0.5f);
                    if (outerAlpha <= 0)
                    {
                        pixels[y * diameter + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    Color c;
                    if (dist > faceRadius)
                    {
                        // Edge band with notch pattern
                        float angle = Mathf.Atan2(dy, dx);
                        float notchPhase = angle * 4f / Mathf.PI;
                        notchPhase = notchPhase - Mathf.Floor(notchPhase); // frac
                        if (notchPhase > 0.2f && notchPhase < 0.8f)
                            c = Color.Lerp(edgeColor, Color.white, 0.5f);
                        else
                            c = edgeColor;
                    }
                    else
                    {
                        // Dome-shaded face
                        float t = dist / faceRadius;
                        t = t * t; // quadratic falloff
                        c = Color.Lerp(lightenedFace, darkenedFace, t);

                        // Inner decorative ring (blended with dome-shaded color)
                        if (Mathf.Abs(dist - innerRingR) < innerRingW)
                        {
                            float ringAlpha = 1f - Mathf.Abs(dist - innerRingR) / innerRingW;
                            c = Color.Lerp(c, edgeColor, ringAlpha * 0.6f);
                        }

                        // Specular highlight (top-left light source)
                        float hlDx = dx - hlCenterX;
                        float hlDy = dy - hlCenterY;
                        float hlDist = Mathf.Sqrt(hlDx * hlDx + hlDy * hlDy);
                        float spec = Mathf.Max(0f, 1f - hlDist / hlRadius);
                        spec = spec * spec * spec * 0.3f; // pow(3) * 0.3
                        c = Color.Lerp(c, Color.white, spec);
                    }

                    c.a *= outerAlpha;
                    pixels[y * diameter + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Destroys all cached textures and sprites. Call on cleanup or for testing.
        /// </summary>
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

            foreach (var kvp in _namedCache)
            {
                if (kvp.Value != null)
                {
                    var tex = kvp.Value.texture;
                    Object.Destroy(kvp.Value);
                    if (tex != null) Object.Destroy(tex);
                }
            }
            _namedCache.Clear();
        }

        // ── Toolbar icon sprites ──────────────────────────────────────

        /// <summary>
        /// Two vertical bars (pause icon). White pixels, tint via Image.color.
        /// </summary>
        public static Sprite GetPauseIcon(int size)
        {
            string key = $"pause_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float pad = size * 0.28f;
            float barW = size * 0.12f;
            float gap = size * 0.10f;
            float cx = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float d1 = LineSDF(px, py, cx - gap, pad, cx - gap, size - pad) - barW;
                    float d2 = LineSDF(px, py, cx + gap, pad, cx + gap, size - pad) - barW;
                    float dist = Mathf.Min(d1, d2);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Circular arrow (reset/reload icon). Ring with 60-degree gap + arrowhead.
        /// </summary>
        public static Sprite GetResetIcon(int size)
        {
            string key = $"reset_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float outerR = size * 0.38f;
            float thickness = size * 0.09f;
            float innerR = outerR - thickness;
            // Gap from -30 to +30 degrees (top of circle)
            float gapHalf = 30f * Mathf.Deg2Rad;

            // Arrowhead at gap end (clockwise tip at +30 degrees from top)
            float arrowAngle = Mathf.PI * 0.5f - gapHalf;
            float ax = center + Mathf.Cos(arrowAngle) * outerR;
            float ay = center + Mathf.Sin(arrowAngle) * outerR;
            float arrowSize = size * 0.14f;
            // Arrow points clockwise along the arc tangent
            Vector2 tipDir = new Vector2(-Mathf.Sin(arrowAngle), Mathf.Cos(arrowAngle));
            Vector2 tip = new Vector2(ax, ay) + tipDir * arrowSize * 0.5f;
            Vector2 perpDir = new Vector2(tipDir.y, -tipDir.x);
            Vector2 arrowBase1 = new Vector2(ax, ay) - tipDir * arrowSize * 0.6f + perpDir * arrowSize * 0.5f;
            Vector2 arrowBase2 = new Vector2(ax, ay) - tipDir * arrowSize * 0.6f - perpDir * arrowSize * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = px - center;
                    float dy = py - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Ring SDF
                    float ringDist = Mathf.Abs(dist - (outerR + innerR) * 0.5f) - thickness * 0.5f;

                    // Mask out the gap region (top of circle)
                    float angle = Mathf.Atan2(dy, dx);
                    float fromTop = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 90f)) * Mathf.Deg2Rad;
                    bool inGap = fromTop < gapHalf && dist < outerR + 1f;

                    float ringAlpha = inGap ? 0f : Mathf.Clamp01(0.5f - ringDist);

                    // Arrowhead triangle
                    Vector2 p = new Vector2(px, py);
                    float triDist = TriangleSDF(p, arrowBase2, tip, arrowBase1);
                    float triAlpha = Mathf.Clamp01(0.5f - triDist);

                    float alpha = Mathf.Max(ringAlpha, triAlpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Speaker icon with sound wave arcs. White pixels, tint via Image.color.
        /// </summary>
        public static Sprite GetSpeakerIcon(int size)
        {
            string key = $"speaker_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float bodyL = size * 0.18f;
            float bodyR = size * 0.38f;
            float bodyH = size * 0.22f;
            float coneR = size * 0.48f;
            float coneH = size * 0.38f;
            float waveThick = size * 0.06f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float alpha = 0f;

                    // Speaker body: rectangle
                    float bx = Mathf.Abs(px - (bodyL + bodyR) * 0.5f) - (bodyR - bodyL) * 0.5f;
                    float by = Mathf.Abs(py - cy) - bodyH;
                    float bodyDist = Mathf.Max(bx, by);
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - bodyDist));

                    // Cone: trapezoid from bodyR to coneR
                    if (px >= bodyR && px <= coneR)
                    {
                        float t = (px - bodyR) / (coneR - bodyR);
                        float halfH = Mathf.Lerp(bodyH, coneH, t);
                        float coneDist = Mathf.Abs(py - cy) - halfH;
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - coneDist));
                    }

                    // Sound wave arcs (two quarter-circle arcs on the right)
                    float wdx = px - coneR;
                    float wdy = py - cy;
                    if (wdx > 0)
                    {
                        float wDist = Mathf.Sqrt(wdx * wdx + wdy * wdy);
                        float angle = Mathf.Atan2(Mathf.Abs(wdy), wdx);
                        if (angle < Mathf.PI * 0.35f)
                        {
                            // Wave 1
                            float w1 = Mathf.Abs(wDist - size * 0.12f) - waveThick;
                            alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - w1));
                            // Wave 2
                            float w2 = Mathf.Abs(wDist - size * 0.24f) - waveThick;
                            alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - w2));
                        }
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Speaker with diagonal strikethrough (muted). White pixels, tint via Image.color.
        /// </summary>
        public static Sprite GetSpeakerMutedIcon(int size)
        {
            string key = $"speakerMuted_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float bodyL = size * 0.18f;
            float bodyR = size * 0.38f;
            float bodyH = size * 0.22f;
            float coneR = size * 0.48f;
            float coneH = size * 0.38f;
            float strikeThick = size * 0.07f;
            float pad = size * 0.18f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float alpha = 0f;

                    // Speaker body
                    float bx = Mathf.Abs(px - (bodyL + bodyR) * 0.5f) - (bodyR - bodyL) * 0.5f;
                    float by = Mathf.Abs(py - cy) - bodyH;
                    float bodyDist = Mathf.Max(bx, by);
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - bodyDist));

                    // Cone
                    if (px >= bodyR && px <= coneR)
                    {
                        float t = (px - bodyR) / (coneR - bodyR);
                        float halfH = Mathf.Lerp(bodyH, coneH, t);
                        float coneDist = Mathf.Abs(py - cy) - halfH;
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - coneDist));
                    }

                    // Diagonal strikethrough
                    float strike = LineSDF(px, py, pad, size - pad, size - pad, pad) - strikeThick;
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(0.5f - strike));

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Left-pointing chevron (back arrow). Two line segments meeting at left point.
        /// </summary>
        public static Sprite GetBackArrow(int size)
        {
            string key = $"backArrow_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float pad = size * 0.30f;
            float midX = size * 0.35f;
            float thickness = size * 0.09f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float d1 = LineSDF(px, py, midX, size * 0.5f, size - pad, pad);
                    float d2 = LineSDF(px, py, midX, size * 0.5f, size - pad, size - pad);
                    float dist = Mathf.Min(d1, d2) - thickness;
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Double right-pointing triangles (fast-forward / auto icon).
        /// </summary>
        public static Sprite GetFastForwardIcon(int size)
        {
            string key = $"ff_{size}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float pad = size * 0.18f;
            float mid = size * 0.5f;
            float gap = size * 0.04f;

            // Left triangle
            Vector2 l0 = new Vector2(pad, pad);
            Vector2 l1 = new Vector2(mid - gap, mid);
            Vector2 l2 = new Vector2(pad, size - pad);
            // Right triangle
            Vector2 r0 = new Vector2(mid + gap, pad);
            Vector2 r1 = new Vector2(size - pad, mid);
            Vector2 r2 = new Vector2(mid + gap, size - pad);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = TriangleSDF(p, l0, l1, l2);
                    float d2 = TriangleSDF(p, r0, r1, r2);
                    float dist = Mathf.Min(d1, d2);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Tileable noise texture for felt micro-texture. Hash-based dithering,
        /// no external assets. Contrast should be 0.02-0.03 for subtle tactile depth.
        /// </summary>
        public static Sprite GetNoiseTile(int size, float contrast)
        {
            string key = $"noise_{size}_{contrast:F3}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple hash-based noise (tileable)
                    uint h = (uint)(x * 374761393 + y * 668265263);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    h ^= h >> 16;
                    float n = (h & 0xFFFF) / 65535f;
                    float v = 0.5f + (n - 0.5f) * contrast;
                    byte b = (byte)(Mathf.Clamp01(v) * 255);
                    pixels[y * size + x] = new Color32(b, b, b, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Tileable felt-fiber texture for realistic fabric appearance. Multi-octave noise
        /// with directional fiber streaks — replaces flat hash noise for tactile realism.
        /// contrast: 0.02-0.03, fiberAngle: degrees (0=horizontal).
        /// </summary>
        public static Sprite GetFeltTexture(int size, float contrast, float fiberAngle)
        {
            string key = $"felt_{size}_{contrast:F3}_{fiberAngle:F1}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            float angleRad = fiberAngle * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(angleRad);
            float sinA = Mathf.Sin(angleRad);

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Multi-octave value noise (3 octaves) for organic variation
                    float noise = 0f;
                    float amp = 1f;
                    float totalAmp = 0f;
                    for (int oct = 0; oct < 3; oct++)
                    {
                        int scale = 1 << oct;
                        int sx = (x * scale) % size;
                        int sy = (y * scale) % size;
                        uint h = (uint)(sx * 374761393 + sy * 668265263 + oct * 1013904223);
                        h = (h ^ (h >> 13)) * 1274126177u;
                        h ^= h >> 16;
                        noise += ((h & 0xFFFF) / 65535f) * amp;
                        totalAmp += amp;
                        amp *= 0.5f;
                    }
                    noise /= totalAmp;

                    // Directional fiber lines — project onto fiber axis and create thin streaks
                    float fx = x - size * 0.5f;
                    float fy = y - size * 0.5f;
                    float along = (fx * cosA + fy * sinA);
                    float perp = (-fx * sinA + fy * cosA);
                    // Wrap perpendicular coordinate for tileability
                    float perpWrapped = perp % 3.7f;
                    // Create thin fiber streaks using sin-based pattern with hash jitter
                    uint fh = (uint)((int)(along * 0.5f) * 2654435761u + (int)(perpWrapped * 10f) * 340573321u);
                    fh = (fh ^ (fh >> 13)) * 1274126177u;
                    fh ^= fh >> 16;
                    float fiberJitter = (fh & 0xFFFF) / 65535f;
                    float fiber = Mathf.Abs(Mathf.Sin(perp * 2.5f + fiberJitter * 6.28f));
                    fiber = Mathf.Pow(fiber, 3f); // Sharpen to thin streaks

                    // Blend: 40% base noise, 60% directional fibers
                    float combined = noise * 0.4f + fiber * 0.6f;

                    float v = 0.5f + (combined - 0.5f) * contrast;
                    byte b = (byte)(Mathf.Clamp01(v) * 255);
                    pixels[y * size + x] = new Color32(b, b, b, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Simplified card back mark — single centered geometric shape per theme.
        /// markSides: 4=diamond, 6=hexagonal rosette, 8=octagonal rosette.
        /// outline: true for Noir-style hollow diamond, false for filled.
        /// </summary>
        public static Sprite GetCardBackMark(int w, int h, int cornerRadius,
            Color markColor, int markSides, bool outline)
        {
            string key = $"cbm_{w}_{h}_{cornerRadius}_{markColor}_{markSides}_{outline}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float maxR = Mathf.Min(w, h) * 0.32f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f, y + 0.5f, w, h, r);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha <= 0) { pixels[y * w + x] = new Color32(0, 0, 0, 0); continue; }

                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;

                    // Primary shape
                    float poly = RegularPolygonSDF(dx, dy, maxR, markSides, 0f);
                    // Second rotated copy for rosette effect (skip for 4-sided)
                    float shape;
                    if (markSides > 4)
                    {
                        float poly2 = RegularPolygonSDF(dx, dy, maxR, markSides, Mathf.PI / markSides);
                        shape = Mathf.Min(poly, poly2);
                    }
                    else
                    {
                        shape = poly;
                    }

                    float markAlpha;
                    if (outline)
                    {
                        float ring = Mathf.Abs(shape);
                        markAlpha = Mathf.Clamp01(1f - ring / 2.5f);
                    }
                    else
                    {
                        markAlpha = Mathf.Clamp01(0.5f - shape);
                    }

                    Color c = new Color(markColor.r, markColor.g, markColor.b, markColor.a * markAlpha * alpha);
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var border = cornerRadius > 0 ? new Vector4(r, r, r, r) : Vector4.zero;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Returns a cached white-alpha sprite for a card suit shape (h=heart, d=diamond, c=club, s=spade).
        /// The sprite is white pixels with alpha from the suit SDF, so it can be tinted via Image.color.
        /// </summary>
        public static Sprite GetSuitSprite(int size, char suit)
        {
            string key = $"suit_{size}_{suit}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (x + 0.5f) - center;
                    float py = (y + 0.5f) - center;

                    float dist;
                    switch (suit)
                    {
                        case 'h': dist = HeartSDF(px, py, size * 0.8f); break;
                        case 'd': dist = DiamondSDF(px, py, size * 0.8f); break;
                        case 'c': dist = ClubSDF(px, py, size * 0.8f); break;
                        case 's': dist = SpadeSDF(px, py, size * 0.8f); break;
                        default:  dist = 1f; break;
                    }

                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);

            _namedCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Returns a card face background with a subtle linen crosshatch texture inside a rounded rect.
        /// The background color is modulated by a faint sine-based pattern for visual richness.
        /// Returns a 9-slice sprite with border = cornerRadius + padding.
        /// </summary>
        public static Sprite GetCardFaceBackground(int w, int h, int cornerRadius, int padding, Color bgColor)
        {
            string key = $"cface_{w}_{h}_{cornerRadius}_{padding}_{bgColor}";
            if (_namedCache.TryGetValue(key, out var cached))
                return cached;

            int texW = w + padding * 2;
            int texH = h + padding * 2;
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[texW * texH];
            int r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2);

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f - padding, y + 0.5f - padding, w, h, r);
                    float alpha = Mathf.Clamp01(0.5f - dist);

                    // Subtle linen crosshatch texture
                    float brightness = 1.0f + 0.015f * Mathf.Sin(x * 0.5f) * Mathf.Sin(y * 0.5f);
                    Color c = new Color(
                        bgColor.r * brightness,
                        bgColor.g * brightness,
                        bgColor.b * brightness,
                        bgColor.a * alpha);
                    pixels[y * texW + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            int border = r + padding;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, texW, texH),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));

            _namedCache[key] = sprite;
            return sprite;
        }

        // ── SDF helpers ────────────────────────────────────────────────

        /// <summary>
        /// Signed distance to a rounded rectangle. Negative = inside, positive = outside.
        /// </summary>
        private static float RoundedRectSDF(float px, float py, int w, int h, int r)
        {
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;
            float dx = Mathf.Max(Mathf.Abs(px - halfW) - (halfW - r), 0f);
            float dy = Mathf.Max(Mathf.Abs(py - halfH) - (halfH - r), 0f);
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>
        /// Distance from point to a line segment (always positive).
        /// </summary>
        private static float LineSDF(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float cx = ax + t * dx - px;
            float cy = ay + t * dy - py;
            return Mathf.Sqrt(cx * cx + cy * cy);
        }

        /// <summary>
        /// Signed distance to a regular polygon centered at origin.
        /// Negative = inside, positive = outside.
        /// </summary>
        private static float RegularPolygonSDF(float px, float py, float radius, int sides, float rotation)
        {
            float angle = Mathf.Atan2(py, px) - rotation;
            float segAngle = 2f * Mathf.PI / sides;
            angle = Mathf.Abs(((angle % segAngle) + segAngle) % segAngle - segAngle * 0.5f);
            float dist = Mathf.Sqrt(px * px + py * py);
            float projected = dist * Mathf.Cos(angle) - radius * Mathf.Cos(segAngle * 0.5f);
            return projected;
        }

        /// <summary>
        /// Signed distance to a triangle (negative inside, positive outside).
        /// Vertices must be counter-clockwise.
        /// </summary>
        private static float TriangleSDF(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ab = b - a, bc = c - b, ca = a - c;
            Vector2 ap = p - a, bp = p - b, cp = p - c;
            float d0 = (ab.x * ap.y - ab.y * ap.x) / ab.magnitude;
            float d1 = (bc.x * bp.y - bc.y * bp.x) / bc.magnitude;
            float d2 = (ca.x * cp.y - ca.y * cp.x) / ca.magnitude;
            return -Mathf.Min(Mathf.Min(d0, d1), d2);
        }

        /// <summary>
        /// Signed distance to a heart shape centered at origin.
        /// Two circle lobes at top with a V-point at the bottom.
        /// </summary>
        private static float HeartSDF(float px, float py, float size)
        {
            float halfSize = size * 0.5f;
            float x = px / halfSize;
            float y = -(py - size * 0.15f) / halfSize;
            // Two circles for upper lobes
            float lobe1 = Mathf.Sqrt((x + 0.5f) * (x + 0.5f) + (y - 0.5f) * (y - 0.5f)) - 0.52f;
            float lobe2 = Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f)) - 0.52f;
            float lobes = Mathf.Min(lobe1, lobe2);
            // Bottom V-point
            float v = Mathf.Abs(x) * 0.85f + (-y) * 0.85f - 0.35f;
            return Mathf.Min(lobes, v) * halfSize;
        }

        /// <summary>
        /// Signed distance to a diamond (rotated square) centered at origin.
        /// </summary>
        private static float DiamondSDF(float px, float py, float size)
        {
            return (Mathf.Abs(px) + Mathf.Abs(py)) * 0.707f - size * 0.5f;
        }

        /// <summary>
        /// Signed distance to a club (trefoil) shape centered at origin.
        /// Three circles at 120-degree intervals plus a rectangular stem below.
        /// </summary>
        private static float ClubSDF(float px, float py, float size)
        {
            float r = size * 0.22f;
            // Top circle
            float dx0 = px;
            float dy0 = py - size * 0.2f;
            float c0 = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0) - r;
            // Left circle
            float dx1 = px + size * 0.22f;
            float dy1 = py + size * 0.08f;
            float c1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1) - r;
            // Right circle
            float dx2 = px - size * 0.22f;
            float dy2 = py + size * 0.08f;
            float c2 = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2) - r;
            // Stem: rectangular region below
            float stemHalfW = size * 0.08f;
            float stemTop = -size * 0.08f;
            float stemBot = -size * 0.45f;
            float stemDx = Mathf.Abs(px) - stemHalfW;
            float stemDy = Mathf.Max(-(py - stemTop), py - stemBot - (stemTop - stemBot));
            // Simplified box SDF for stem
            float stemDyInner = Mathf.Max(py - stemTop, stemBot - py);
            float stem = Mathf.Max(stemDx, stemDyInner);

            return Mathf.Min(Mathf.Min(Mathf.Min(c0, c1), c2), stem);
        }

        /// <summary>
        /// Signed distance to a spade shape centered at origin.
        /// Inverted heart body (flipped vertically) plus a rectangular stem below.
        /// </summary>
        private static float SpadeSDF(float px, float py, float size)
        {
            // Inverted heart body: negate py and shift to form the spade top
            float body = HeartSDF(px, -py + size * 0.05f, size * 0.85f);
            // Stem: rectangular region below
            float stemHalfW = size * 0.08f;
            float stemTop = -size * 0.08f;
            float stemBot = -size * 0.45f;
            float stemDx = Mathf.Abs(px) - stemHalfW;
            float stemDy = Mathf.Max(py - stemTop, stemBot - py);
            float stem = Mathf.Max(stemDx, stemDy);

            return Mathf.Min(body, stem);
        }
    }
}
