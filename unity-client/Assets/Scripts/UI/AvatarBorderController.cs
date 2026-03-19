using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Animation;

namespace HijackPoker.UI
{
    /// <summary>
    /// Simplified avatar border. Static color changes by state — no rotating conic gradient.
    /// Idle: subtle ring, Active: accent blue, Winner: gold, Folded: invisible.
    /// </summary>
    public class AvatarBorderController
    {
        public enum BorderState { Idle, Active, Winner, Folded }

        private readonly Image _ringImage;
        private readonly RectTransform _ringRt;
        private BorderState _currentState = BorderState.Idle;

        public AvatarBorderController(Transform parent, float size)
        {
            var go = new GameObject("AvatarBorder", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _ringRt = go.GetComponent<RectTransform>();
            UIFactory.SetAnchor(_ringRt, 0.5f, 0.5f);
            _ringRt.sizeDelta = new Vector2(size, size);

            _ringImage = go.AddComponent<Image>();
            _ringImage.sprite = TextureGenerator.GetRing((int)size, 2);
            _ringImage.raycastTarget = false;
            _ringImage.color = new Color(1, 1, 1, 0.15f);
        }

        public void SetState(BorderState state, AnimationController anim)
        {
            if (state == _currentState) return;
            _currentState = state;

            switch (state)
            {
                case BorderState.Idle:
                    _ringImage.color = new Color(1, 1, 1, 0.15f);
                    break;

                case BorderState.Active:
                    var blue = UIFactory.AccentCyan;
                    _ringImage.color = new Color(blue.r, blue.g, blue.b, 0.8f);
                    break;

                case BorderState.Winner:
                    var gold = UIFactory.AccentGold;
                    _ringImage.color = new Color(gold.r, gold.g, gold.b, 0.9f);
                    break;

                case BorderState.Folded:
                    _ringImage.color = new Color(1, 1, 1, 0f);
                    break;
            }
        }

        public void Destroy()
        {
            // No tweens to cancel
        }
    }
}
