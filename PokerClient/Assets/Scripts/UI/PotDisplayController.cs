using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages the center pot text display and chip stacks below community cards.
    /// More chips appear as the pot grows, arranged in stacked columns.
    /// </summary>
    public class PotDisplayController : MonoBehaviour
    {
        private TextMeshProUGUI _centerPotText;
        private RectTransform _potTarget;
        private Sprite _chipFlySprite;
        private readonly List<GameObject> _potChips = new List<GameObject>();
        private float _displayedPot;
        private int _lastChipCount = -1;

        private const int MaxChips = 30;
        private const int MaxPerColumn = 6;
        private const float ChipSize = 28f;
        private const float ColumnSpacing = 18f;
        private const float ChipStepY = 3.5f;

        public void Initialize(TextMeshProUGUI centerPotText, RectTransform potTarget, Sprite chipFlySprite)
        {
            _centerPotText = centerPotText;
            _potTarget = potTarget;
            _chipFlySprite = chipFlySprite;
        }

        public void UpdateCenterPot(float pot)
        {
            if (_centerPotText == null) return;

            DOTween.Kill(_centerPotText);
            if (pot <= 0)
            {
                _centerPotText.text = "";
                _displayedPot = 0;
                UpdatePotChips(0);
                return;
            }

            DOVirtual.Float(_displayedPot, pot, 0.5f, value =>
            {
                _displayedPot = value;
                _centerPotText.text = $"POT: {MoneyFormatter.Format(value)}";
            }).SetEase(Ease.OutCubic).SetTarget(_centerPotText);

            UpdatePotChips(pot);
        }

        private void UpdatePotChips(float pot)
        {
            if (_potTarget == null || _chipFlySprite == null) return;

            // Scale chip count with pot size — more chips as pot grows
            // $2=1, $5=2, $10=3, $15=4, $20=5, $30=7, $50=10, $80=14, $120=18, $200+=24-30
            int targetCount = 0;
            if (pot >= 2) targetCount = 1;
            if (pot >= 5) targetCount = 2;
            if (pot >= 10) targetCount = 3;
            if (pot >= 15) targetCount = 4;
            if (pot >= 20) targetCount = 5;
            if (pot >= 30) targetCount = 7;
            if (pot >= 40) targetCount = 9;
            if (pot >= 50) targetCount = 11;
            if (pot >= 70) targetCount = 14;
            if (pot >= 100) targetCount = 18;
            if (pot >= 150) targetCount = 22;
            if (pot >= 200) targetCount = 26;
            if (pot >= 300) targetCount = MaxChips;
            targetCount = Mathf.Min(targetCount, MaxChips);

            if (targetCount == _lastChipCount) return;
            _lastChipCount = targetCount;

            // Remove excess
            while (_potChips.Count > targetCount)
            {
                var chip = _potChips[_potChips.Count - 1];
                _potChips.RemoveAt(_potChips.Count - 1);
                if (chip != null) Destroy(chip);
            }

            // Add new chips
            while (_potChips.Count < targetCount)
            {
                int i = _potChips.Count;
                int col = i / MaxPerColumn;
                int row = i % MaxPerColumn;
                int totalCols = Mathf.CeilToInt((float)targetCount / MaxPerColumn);

                var chipGO = new GameObject($"PotChip_{i}", typeof(RectTransform));
                chipGO.transform.SetParent(_potTarget, false);
                var img = chipGO.AddComponent<Image>();
                img.sprite = _chipFlySprite;
                img.preserveAspect = true;
                img.raycastTarget = false;

                var rt = chipGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(ChipSize, ChipSize);

                // Position in columns, centered, below community cards
                float totalWidth = (totalCols - 1) * ColumnSpacing;
                float x = -totalWidth * 0.5f + col * ColumnSpacing;
                float y = -85f + row * ChipStepY; // offset down below the cards
                rt.anchoredPosition = new Vector2(x, y);

                chipGO.transform.SetAsLastSibling();

                // Pop-in animation
                chipGO.transform.localScale = Vector3.zero;
                chipGO.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

                _potChips.Add(chipGO);
            }

            // Reposition existing chips when column count changes
            RepositionChips(targetCount);
        }

        private void RepositionChips(int totalCount)
        {
            int totalCols = Mathf.CeilToInt((float)totalCount / MaxPerColumn);
            float totalWidth = (totalCols - 1) * ColumnSpacing;

            for (int i = 0; i < _potChips.Count; i++)
            {
                if (_potChips[i] == null) continue;
                int col = i / MaxPerColumn;
                int row = i % MaxPerColumn;

                float x = -totalWidth * 0.5f + col * ColumnSpacing;
                float y = -85f + row * ChipStepY;

                var rt = _potChips[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
            }
        }

        public void Cleanup()
        {
            if (_centerPotText != null) DOTween.Kill(_centerPotText);
            foreach (var chip in _potChips)
                if (chip != null) Destroy(chip);
            _potChips.Clear();
        }
    }
}
