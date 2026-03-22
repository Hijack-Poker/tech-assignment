using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Animation;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    /// <summary>
    /// Phase-based color grading overlay and vignette for the table felt.
    /// Maps HandStep to atmospheric buckets: Idle, Betting, Showdown, Winner.
    /// </summary>
    public class TableAtmosphereController : MonoBehaviour
    {
        private Image _gradientOverlay;
        private Image _vignetteImg;
        private AnimationController _anim;
        private TweenHandle _colorTween;
        private TweenHandle _vignetteTween;
        private TweenHandle _breathTween;
        private Image _feltGlowImg;
        private int _currentBucket = -1;

        // Phase bucket colors (set from theme)
        private Color _idleColor;
        private Color _bettingColor;
        private Color _showdownColor;
        private Color _winnerColor;

        public static TableAtmosphereController Create(RectTransform feltRt, AnimationController animController,
            Image feltGlowImg, TableTheme theme)
        {
            var go = new GameObject("AtmosphereController", typeof(RectTransform));
            go.transform.SetParent(feltRt, false);

            var controller = go.AddComponent<TableAtmosphereController>();
            controller._anim = animController;
            controller._feltGlowImg = feltGlowImg;

            var t = theme ?? TableTheme.ForTable(1);
            controller._idleColor = t.AtmoIdle;
            controller._bettingColor = t.AtmoBetting;
            controller._showdownColor = t.AtmoShowdown;
            controller._winnerColor = t.AtmoWinner;

            // Radial gradient overlay for color grading
            var overlayRt = UIFactory.CreatePanel("AtmoOverlay", feltRt);
            UIFactory.StretchFill(overlayRt);
            controller._gradientOverlay = overlayRt.gameObject.AddComponent<Image>();
            controller._gradientOverlay.sprite = TextureGenerator.GetRadialGradient(64,
                new Color(1, 1, 1, 0.5f), new Color(1, 1, 1, 0f));
            controller._gradientOverlay.color = controller._idleColor;
            controller._gradientOverlay.raycastTarget = false;

            // Vignette overlay (wider range on dark bg)
            var vigRt = UIFactory.CreatePanel("Vignette", feltRt);
            UIFactory.StretchFill(vigRt);
            controller._vignetteImg = vigRt.gameObject.AddComponent<Image>();
            controller._vignetteImg.sprite = TextureGenerator.GetVignette(128, 64, 1.8f);
            controller._vignetteImg.color = new Color(1, 1, 1, 0.25f);
            controller._vignetteImg.raycastTarget = false;

            // Start breathing glow via PulseGlow
            if (feltGlowImg != null && animController != null)
            {
                controller._breathTween = animController.Play(Tweener.PulseGlow(
                    a =>
                    {
                        if (feltGlowImg != null)
                        {
                            var c = feltGlowImg.color;
                            feltGlowImg.color = new Color(c.r, c.g, c.b, a);
                        }
                    }, 0.04f, 0.10f, 3f));
            }

            return controller;
        }

        public void ApplyPhase(GameState game)
        {
            if (game == null) return;

            int bucket = GetBucket(game.HandStep);
            if (bucket == _currentBucket) return;
            _currentBucket = bucket;

            Color targetColor;
            float vigAlpha;
            switch (bucket)
            {
                case 1: targetColor = _bettingColor; vigAlpha = 0.30f; break;
                case 2: targetColor = _showdownColor; vigAlpha = 0.55f; break;
                case 3: targetColor = _winnerColor; vigAlpha = 0.20f; break;
                default: targetColor = _idleColor; vigAlpha = 0.25f; break;
            }

            if (_anim != null)
            {
                _colorTween?.Cancel();
                Color fromColor = _gradientOverlay.color;
                _colorTween = _anim.Play(Tweener.TweenColor(fromColor, targetColor, 1.2f,
                    c => { if (_gradientOverlay != null) _gradientOverlay.color = c; }));

                _vignetteTween?.Cancel();
                float fromVig = _vignetteImg.color.a;
                _vignetteTween = _anim.Play(Tweener.TweenFloat(fromVig, vigAlpha, 1.2f,
                    a => { if (_vignetteImg != null) _vignetteImg.color = new Color(1, 1, 1, a); }));
            }
            else
            {
                _gradientOverlay.color = targetColor;
                _vignetteImg.color = new Color(1, 1, 1, vigAlpha);
            }
        }

        public void SetTheme(TableTheme theme)
        {
            _idleColor = theme.AtmoIdle;
            _bettingColor = theme.AtmoBetting;
            _showdownColor = theme.AtmoShowdown;
            _winnerColor = theme.AtmoWinner;
            _currentBucket = -1;
        }

        private static int GetBucket(int handStep)
        {
            if (handStep <= 3) return 0;   // Idle
            if (handStep == 5 || handStep == 7 || handStep == 9 || handStep == 11) return 1; // Betting
            if (handStep == 12) return 2;  // Showdown
            if (handStep >= 13) return 3;  // Winner
            return 0;                      // Deal/other = Idle
        }

        private void OnDestroy()
        {
            _colorTween?.Cancel();
            _vignetteTween?.Cancel();
            _breathTween?.Cancel();
        }
    }
}
