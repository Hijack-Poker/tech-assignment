using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HijackPoker.UI
{
    public class ChipStackView : MonoBehaviour
    {
        [SerializeField] private Sprite[] _chipSprites;
        [SerializeField] private Vector2 _chipSize = new Vector2(24f, 24f);
        [SerializeField] private float _columnSpacing = 14f;
        [SerializeField] private float _chipStepY = 2.4f;
        [SerializeField] private int _maxPerColumn = 10;
        [SerializeField] private int _maxVisualChips = 52;

        private readonly List<Image> _chipPool = new List<Image>();
        private int _lastWholeAmount = -1;

        private static readonly int[] Denominations = { 100, 25, 5, 1 };
        private static readonly int[] SpriteIndexByDenomination = { 3, 1, 0, 2 };

        private struct ColumnSpec
        {
            public int Count;
            public Sprite Sprite;
        }

        public void Render(float stackAmount)
        {
            int wholeAmount = Mathf.Max(0, Mathf.FloorToInt(stackAmount));
            if (wholeAmount == _lastWholeAmount) return;
            _lastWholeAmount = wholeAmount;

            if (_chipSprites == null || _chipSprites.Length == 0 || wholeAmount <= 0)
            {
                Clear();
                return;
            }

            int[] counts = BuildDenominationCounts(wholeAmount);
            int[] visualCounts = BuildVisualCounts(counts);
            var columns = BuildColumns(visualCounts);

            if (columns.Count == 0)
            {
                Clear();
                return;
            }

            int needed = 0;
            for (int i = 0; i < columns.Count; i++) needed += columns[i].Count;
            EnsurePoolSize(needed);

            float startX = -(columns.Count - 1) * _columnSpacing * 0.5f;
            float baselineY = -16f;
            int poolIndex = 0;

            for (int col = 0; col < columns.Count; col++)
            {
                var spec = columns[col];
                float x = startX + col * _columnSpacing;

                for (int i = 0; i < spec.Count; i++)
                {
                    var chip = _chipPool[poolIndex++];
                    chip.sprite = spec.Sprite;
                    chip.enabled = spec.Sprite != null;
                    chip.gameObject.SetActive(true);

                    var rt = chip.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = _chipSize;
                    rt.anchoredPosition = new Vector2(x, baselineY + i * _chipStepY);
                }
            }

            for (int i = poolIndex; i < _chipPool.Count; i++)
                _chipPool[i].gameObject.SetActive(false);
        }

        public void Clear()
        {
            _lastWholeAmount = -1;
            for (int i = 0; i < _chipPool.Count; i++)
                _chipPool[i].gameObject.SetActive(false);
        }

        private int[] BuildDenominationCounts(int amount)
        {
            var counts = new int[Denominations.Length];
            int rem = amount;
            for (int i = 0; i < Denominations.Length; i++)
            {
                counts[i] = rem / Denominations[i];
                rem %= Denominations[i];
            }
            return counts;
        }

        private int[] BuildVisualCounts(int[] counts)
        {
            int total = 0;
            for (int i = 0; i < counts.Length; i++) total += counts[i];
            if (total <= _maxVisualChips) return counts;

            var visual = new int[counts.Length];
            var remainders = new float[counts.Length];
            float ratio = (float)_maxVisualChips / total;
            int sum = 0;

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] <= 0) continue;
                float raw = counts[i] * ratio;
                int v = Mathf.Max(1, Mathf.FloorToInt(raw));
                v = Mathf.Min(v, counts[i]);
                visual[i] = v;
                remainders[i] = raw - Mathf.Floor(raw);
                sum += v;
            }

            while (sum < _maxVisualChips)
            {
                int pick = -1;
                float best = -1f;
                for (int i = 0; i < counts.Length; i++)
                {
                    if (visual[i] >= counts[i]) continue;
                    if (remainders[i] > best)
                    {
                        best = remainders[i];
                        pick = i;
                    }
                }
                if (pick == -1) break;
                visual[pick]++;
                sum++;
            }

            while (sum > _maxVisualChips)
            {
                int pick = -1;
                int best = 0;
                for (int i = 0; i < visual.Length; i++)
                {
                    if (visual[i] > 1 && visual[i] > best)
                    {
                        best = visual[i];
                        pick = i;
                    }
                }
                if (pick == -1) break;
                visual[pick]--;
                sum--;
            }

            return visual;
        }

        private List<ColumnSpec> BuildColumns(int[] visualCounts)
        {
            var columns = new List<ColumnSpec>();

            for (int i = 0; i < visualCounts.Length; i++)
            {
                int remaining = visualCounts[i];
                if (remaining <= 0) continue;

                Sprite sprite = ResolveSprite(i);
                while (remaining > 0)
                {
                    int inColumn = Mathf.Min(_maxPerColumn, remaining);
                    columns.Add(new ColumnSpec
                    {
                        Count = inColumn,
                        Sprite = sprite
                    });
                    remaining -= inColumn;
                }
            }

            return columns;
        }

        private Sprite ResolveSprite(int denominationIndex)
        {
            int spriteIdx = SpriteIndexByDenomination[denominationIndex];
            if (spriteIdx >= 0 && spriteIdx < _chipSprites.Length)
                return _chipSprites[spriteIdx];
            return _chipSprites.Length > 0 ? _chipSprites[0] : null;
        }

        private void EnsurePoolSize(int needed)
        {
            while (_chipPool.Count < needed)
            {
                var go = new GameObject($"Chip_{_chipPool.Count}", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                _chipPool.Add(image);
            }
        }
    }
}
