using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Animation;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    /// <summary>
    /// Renders colored chip columns next to bet amounts.
    /// 4 denominations: $1 (white), $5 (red), $25 (green), $100 (black/gold).
    /// Greedy decomposition with vertical stacking and overlap.
    /// </summary>
    public class ChipStackView : MonoBehaviour
    {
        private static readonly (int value, Color face, Color edge)[] Denominations =
        {
            (100, new Color(0.15f, 0.15f, 0.18f, 1f), new Color(0.85f, 0.68f, 0.15f, 1f)),  // black/gold
            (25,  new Color(0.15f, 0.65f, 0.30f, 1f), new Color(0.10f, 0.50f, 0.22f, 1f)),   // green
            (5,   new Color(0.85f, 0.20f, 0.20f, 1f), new Color(0.70f, 0.12f, 0.12f, 1f)),   // red
            (1,   new Color(0.92f, 0.92f, 0.90f, 1f), new Color(0.78f, 0.78f, 0.76f, 1f)),   // white
        };

        private RectTransform _rt;
        private readonly List<Image> _chipImages = new();
        private readonly List<Image> _shadowImages = new();
        private float _displayedBet;

        public static ChipStackView Create(Transform parent)
        {
            var go = new GameObject("ChipStack", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(LayoutConfig.ChipDiameter * 4, LayoutConfig.ChipMaxHeight);

            var view = go.AddComponent<ChipStackView>();
            view._rt = rt;
            return view;
        }

        public void UpdateBet(float bet, AnimationController anim)
        {
            if (Mathf.Abs(bet - _displayedBet) < 0.01f) return;
            _displayedBet = bet;

            // Clear existing chips and shadows
            foreach (var img in _chipImages)
            {
                if (img != null) Destroy(img.gameObject);
            }
            _chipImages.Clear();
            foreach (var img in _shadowImages)
            {
                if (img != null) Destroy(img.gameObject);
            }
            _shadowImages.Clear();

            if (bet < 1f) return;

            var chips = DecomposeBet(bet);
            float chipDia = LayoutConfig.ChipDiameter;
            float overlap = LayoutConfig.ChipOverlap;
            float x = 0f;

            foreach (var (count, denomIdx) in chips)
            {
                var (_, face, edge) = Denominations[denomIdx];
                for (int i = 0; i < count && i < 5; i++)
                {
                    // Per-chip shadow (for stacked chips after the first)
                    if (i > 0)
                    {
                        var shadowImg = UIFactory.CreateImage($"ChipShadow_{denomIdx}_{i}", transform,
                            new Color(0, 0, 0, 0.15f), new Vector2(chipDia, chipDia));
                        shadowImg.sprite = TextureGenerator.GetCircle((int)chipDia);
                        var srt = shadowImg.GetComponent<RectTransform>();
                        srt.anchorMin = new Vector2(0, 0.5f);
                        srt.anchorMax = new Vector2(0, 0.5f);
                        srt.pivot = new Vector2(0, 0.5f);
                        srt.anchoredPosition = new Vector2(x, i * overlap - 1f);
                        shadowImg.raycastTarget = false;
                        _shadowImages.Add(shadowImg);
                    }

                    var chipImg = UIFactory.CreateImage($"Chip_{denomIdx}_{i}", transform,
                        Color.white, new Vector2(chipDia, chipDia));
                    chipImg.sprite = TextureGenerator.GetChipTexture((int)chipDia, face, edge);
                    var crt = chipImg.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(0, 0.5f);
                    crt.anchorMax = new Vector2(0, 0.5f);
                    crt.pivot = new Vector2(0, 0.5f);
                    crt.anchoredPosition = new Vector2(x, i * overlap);
                    chipImg.raycastTarget = false;
                    _chipImages.Add(chipImg);

                    if (anim != null)
                        anim.Play(Tweener.ScalePop(crt, 0.15f, 1.2f));
                }
                x += chipDia + 2f;
            }

            if (chips.Count > 0)
                AudioManager.Instance?.Play(SoundType.ChipClink);
        }

        /// <summary>
        /// Greedy decomposition of bet amount into chip denominations.
        /// Returns list of (count, denomination index).
        /// </summary>
        public static List<(int count, int denomIdx)> DecomposeBet(float bet)
        {
            var result = new List<(int, int)>();
            int remaining = Mathf.RoundToInt(bet);

            for (int i = 0; i < Denominations.Length; i++)
            {
                int count = remaining / Denominations[i].value;
                if (count > 0)
                {
                    result.Add((count, i));
                    remaining -= count * Denominations[i].value;
                }
            }

            return result;
        }

        public void Clear()
        {
            foreach (var img in _chipImages)
            {
                if (img != null) Destroy(img.gameObject);
            }
            _chipImages.Clear();
            foreach (var img in _shadowImages)
            {
                if (img != null) Destroy(img.gameObject);
            }
            _shadowImages.Clear();
            _displayedBet = 0;
        }
    }
}
