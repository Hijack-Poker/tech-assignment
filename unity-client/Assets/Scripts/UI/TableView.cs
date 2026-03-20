using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Animation;

namespace HijackPoker.UI
{
    /// <summary>
    /// Renders the poker table: dark background, themed felt with radial center
    /// highlight and micro-texture noise, multi-layer rail/border.
    /// Per-theme corner radius provides geometric identity.
    /// </summary>
    public class TableView : MonoBehaviour
    {
        private RectTransform _surface;

        public TableTheme Theme { get; private set; }
        private Image _railImg;
        private Image _railHighlightImg;
        private Image _feltImg;
        private Image _highlightImg;
        private Image _stitchImg;
        private Image _stitchInnerImg;
        private Image _noiseImg;

        public RectTransform Surface => _surface;
        public TableAtmosphereController Atmosphere { get; private set; }

        public static TableView Create(Transform parent, AnimationController animController = null, TableTheme theme = null)
        {
            theme = theme ?? TableTheme.ForTable(1);

            // Background — dark gradient fills entire canvas
            var bgRt = UIFactory.CreatePanel("Background", parent);
            UIFactory.StretchFill(bgRt);
            var bgImg = bgRt.gameObject.AddComponent<Image>();
            bgImg.sprite = TextureGenerator.GetVerticalGradient(
                4, 64, UIFactory.Background, UIFactory.BackgroundBottom, 0);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            // Table container
            var tableGo = new GameObject("TableView", typeof(RectTransform));
            tableGo.transform.SetParent(bgRt, false);
            var tableRt = tableGo.GetComponent<RectTransform>();
            UIFactory.StretchFill(tableRt);

            var view = tableGo.AddComponent<TableView>();
            view.Theme = theme;

            int cornerR = theme.TableCornerRadius;

            // Outer soft shadow (table sits on surface with depth)
            var shadowRt = UIFactory.CreatePanel("TableShadow", tableRt);
            shadowRt.anchorMin = LayoutConfig.TableGlowMin;
            shadowRt.anchorMax = LayoutConfig.TableGlowMax;
            shadowRt.offsetMin = Vector2.zero;
            shadowRt.offsetMax = Vector2.zero;
            var shadowImg = shadowRt.gameObject.AddComponent<Image>();
            shadowImg.sprite = TextureGenerator.GetSoftShadow(128, 64, cornerR, 8, 0.15f);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = Color.white;
            shadowImg.raycastTarget = false;

            // Felt surface — responsive to portrait/landscape
            var surfaceRt = UIFactory.CreatePanel("Surface", tableRt);
            surfaceRt.anchorMin = LayoutConfig.TableSurfaceMin;
            surfaceRt.anchorMax = LayoutConfig.TableSurfaceMax;
            surfaceRt.offsetMin = Vector2.zero;
            surfaceRt.offsetMax = Vector2.zero;

            var tableRoundedSprite = TextureGenerator.GetRoundedRect(128, 64, cornerR);

            // Rail band (outer border)
            var railImg = surfaceRt.gameObject.AddComponent<Image>();
            railImg.color = theme.RailBand;
            railImg.sprite = tableRoundedSprite;
            railImg.type = Image.Type.Sliced;
            railImg.raycastTarget = false;
            view._railImg = railImg;

            // Rail highlight (thin bright line on top edge of rail)
            var railHighlight = UIFactory.CreatePanel("RailHighlight", surfaceRt,
                theme.RailHighlight);
            var rhImg = railHighlight.GetComponent<Image>();
            if (rhImg == null) rhImg = railHighlight.gameObject.AddComponent<Image>();
            rhImg.sprite = tableRoundedSprite;
            rhImg.type = Image.Type.Sliced;
            var rhRt = railHighlight.GetComponent<RectTransform>();
            rhRt.anchorMin = Vector2.zero;
            rhRt.anchorMax = Vector2.one;
            rhRt.offsetMin = new Vector2(1, 1);
            rhRt.offsetMax = new Vector2(-1, -1);
            view._railHighlightImg = rhImg;

            // Inner felt with inset for border effect — themed gradient
            var feltRt = UIFactory.CreatePanel("Felt", surfaceRt);
            var feltImg = feltRt.gameObject.AddComponent<Image>();
            feltImg.sprite = TextureGenerator.GetVerticalGradient(
                128, 64, theme.FeltLight, theme.FeltBase, cornerR);
            feltImg.type = Image.Type.Sliced;
            feltImg.color = Color.white;
            feltRt.anchorMin = Vector2.zero;
            feltRt.anchorMax = Vector2.one;
            feltRt.offsetMin = new Vector2(4, 4);
            feltRt.offsetMax = new Vector2(-4, -4);
            view._feltImg = feltImg;

            // Felt micro-texture noise (low-contrast tileable noise for tactile depth)
            if (theme.FeltNoiseContrast > 0f)
            {
                var noiseRt = UIFactory.CreatePanel("FeltNoise", feltRt);
                noiseRt.anchorMin = Vector2.zero;
                noiseRt.anchorMax = Vector2.one;
                noiseRt.offsetMin = Vector2.zero;
                noiseRt.offsetMax = Vector2.zero;
                var noiseImg = noiseRt.gameObject.AddComponent<Image>();
                noiseImg.sprite = TextureGenerator.GetFeltTexture(256, theme.FeltNoiseContrast, theme.FeltFiberAngle);
                noiseImg.type = Image.Type.Tiled;
                noiseImg.color = new Color(1, 1, 1, 0.22f);
                noiseImg.raycastTarget = false;
                view._noiseImg = noiseImg;
            }

            // Radial center highlight — light pooling effect like casino lighting
            var innerHighlight = UIFactory.CreatePanel("InnerHighlight", feltRt);
            innerHighlight.anchorMin = new Vector2(0.15f, 0.15f);
            innerHighlight.anchorMax = new Vector2(0.85f, 0.85f);
            innerHighlight.offsetMin = Vector2.zero;
            innerHighlight.offsetMax = Vector2.zero;
            var highlightImg = innerHighlight.gameObject.AddComponent<Image>();
            highlightImg.sprite = TextureGenerator.GetRadialGradient(128,
                new Color(theme.FeltHighlight.r, theme.FeltHighlight.g, theme.FeltHighlight.b, 0.35f),
                new Color(theme.FeltHighlight.r, theme.FeltHighlight.g, theme.FeltHighlight.b, 0f));
            highlightImg.raycastTarget = false;
            view._highlightImg = highlightImg;

            // Breathing glow center (animated via atmosphere controller)
            var glowCenter = UIFactory.CreatePanel("GlowCenter", feltRt);
            glowCenter.anchorMin = new Vector2(0.25f, 0.25f);
            glowCenter.anchorMax = new Vector2(0.75f, 0.75f);
            glowCenter.offsetMin = Vector2.zero;
            glowCenter.offsetMax = Vector2.zero;
            var glowImg = glowCenter.gameObject.AddComponent<Image>();
            glowImg.sprite = TextureGenerator.GetRadialGradient(64,
                new Color(1f, 1f, 1f, 0.04f),
                new Color(1f, 1f, 1f, 0f));
            glowImg.raycastTarget = false;

            // Felt edge stitching simulation (tiny dots running inside rail)
            var stitchRt = UIFactory.CreatePanel("Stitching", feltRt,
                new Color(theme.FeltHighlightEdge.r, theme.FeltHighlightEdge.g,
                    theme.FeltHighlightEdge.b, 0.08f));
            var stitchImg = stitchRt.GetComponent<Image>();
            if (stitchImg == null) stitchImg = stitchRt.gameObject.AddComponent<Image>();
            stitchImg.sprite = tableRoundedSprite;
            stitchImg.type = Image.Type.Sliced;
            stitchRt.anchorMin = Vector2.zero;
            stitchRt.anchorMax = Vector2.one;
            stitchRt.offsetMin = new Vector2(2, 2);
            stitchRt.offsetMax = new Vector2(-2, -2);
            view._stitchImg = stitchImg;

            // Inner stitching cutout (makes the stitch a thin border)
            var stitchInner = UIFactory.CreatePanel("StitchInner", stitchRt);
            var siImg = stitchInner.gameObject.AddComponent<Image>();
            siImg.sprite = tableRoundedSprite;
            siImg.type = Image.Type.Sliced;
            siImg.color = new Color(theme.FeltBase.r, theme.FeltBase.g, theme.FeltBase.b, 1f);
            var siRt = stitchInner.GetComponent<RectTransform>();
            siRt.anchorMin = Vector2.zero;
            siRt.anchorMax = Vector2.one;
            siRt.offsetMin = new Vector2(1, 1);
            siRt.offsetMax = new Vector2(-1, -1);
            view._stitchInnerImg = siImg;

            // Atmosphere controller (handles breathing glow, phase color grading, vignette)
            if (animController != null)
                view.Atmosphere = TableAtmosphereController.Create(feltRt, animController, glowImg, theme);

            view._surface = tableRt;
            return view;
        }

        public void ApplyTheme(TableTheme theme)
        {
            Theme = theme;
            int cornerR = theme.TableCornerRadius;
            var tableRoundedSprite = TextureGenerator.GetRoundedRect(128, 64, cornerR);

            if (_railImg != null)
            {
                _railImg.color = theme.RailBand;
                _railImg.sprite = tableRoundedSprite;
            }
            if (_railHighlightImg != null)
            {
                _railHighlightImg.color = theme.RailHighlight;
                _railHighlightImg.sprite = tableRoundedSprite;
            }
            if (_feltImg != null)
                _feltImg.sprite = TextureGenerator.GetVerticalGradient(128, 64, theme.FeltLight, theme.FeltBase, cornerR);
            if (_highlightImg != null)
                _highlightImg.sprite = TextureGenerator.GetRadialGradient(128,
                    new Color(theme.FeltHighlight.r, theme.FeltHighlight.g, theme.FeltHighlight.b, 0.35f),
                    new Color(theme.FeltHighlight.r, theme.FeltHighlight.g, theme.FeltHighlight.b, 0f));
            if (_stitchImg != null)
            {
                _stitchImg.color = new Color(theme.FeltHighlightEdge.r, theme.FeltHighlightEdge.g,
                    theme.FeltHighlightEdge.b, 0.08f);
                _stitchImg.sprite = tableRoundedSprite;
            }
            if (_stitchInnerImg != null)
            {
                _stitchInnerImg.color = new Color(theme.FeltBase.r, theme.FeltBase.g, theme.FeltBase.b, 1f);
                _stitchInnerImg.sprite = tableRoundedSprite;
            }
            if (_noiseImg != null && theme.FeltNoiseContrast > 0f)
                _noiseImg.sprite = TextureGenerator.GetFeltTexture(256, theme.FeltNoiseContrast, theme.FeltFiberAngle);
            Atmosphere?.SetTheme(theme);
        }
    }
}
