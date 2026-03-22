using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages the center pot text display and animated chip stacks.
    /// </summary>
    public class PotDisplayController : MonoBehaviour
    {
        private TextMeshProUGUI _centerPotText;
        private RectTransform _potTarget;
        private Sprite _chipFlySprite;
        private readonly List<GameObject> _potChips = new List<GameObject>();
        private float _displayedPot;

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

            int targetCount = 0;
            if (pot >= 5) targetCount = 1;
            if (pot >= 15) targetCount = 2;
            if (pot >= 30) targetCount = 3;
            if (pot >= 50) targetCount = 4;
            if (pot >= 80) targetCount = 5;
            if (pot >= 120) targetCount = 6;

            while (_potChips.Count > targetCount)
            {
                var chip = _potChips[_potChips.Count - 1];
                _potChips.RemoveAt(_potChips.Count - 1);
                if (chip != null) Destroy(chip);
            }

            while (_potChips.Count < targetCount)
            {
                int i = _potChips.Count;
                var chipGO = new GameObject($"PotChip_{i}", typeof(RectTransform));
                chipGO.transform.SetParent(_potTarget, false);
                var img = chipGO.AddComponent<Image>();
                img.sprite = _chipFlySprite;
                img.preserveAspect = true;
                var rt = chipGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(32, 32);

                float x = (i % 2 == 0 ? -1 : 1) * (i / 2) * 8f;
                float y = i * 4f;
                rt.anchoredPosition = new Vector2(x, y);

                chipGO.transform.SetAsLastSibling();
                chipGO.transform.localScale = Vector3.zero;
                chipGO.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

                _potChips.Add(chipGO);
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
