using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    /// <summary>
    /// Small dark rounded-rect popup showing player session stats.
    /// Singleton — only one visible at a time. Auto-dismisses after 3s.
    /// </summary>
    public class PlayerStatsTooltip : MonoBehaviour
    {
        private RectTransform _rt;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _statsText;
        private AnimationController _animController;
        private SessionTracker _sessionTracker;
        private TweenHandle _showTween;
        private TweenHandle _dismissTween;
        private float _showTime;
        private bool _isVisible;

        private const float AutoDismissSeconds = 3f;

        public static PlayerStatsTooltip Create(Transform parent, AnimationController anim,
            SessionTracker tracker)
        {
            var go = new GameObject("PlayerStatsTooltip", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 80);

            var view = go.AddComponent<PlayerStatsTooltip>();
            view._rt = rt;
            view._animController = anim;
            view._sessionTracker = tracker;
            view._canvasGroup = go.AddComponent<CanvasGroup>();
            view.BuildUI();
            view.Hide(false);
            return view;
        }

        private void BuildUI()
        {
            // Dark rounded background
            var bg = UIFactory.CreateImage("TooltipBg", transform,
                new Color(0.08f, 0.10f, 0.16f, 0.92f));
            bg.sprite = TextureGenerator.GetRoundedRect(160, 80, 10);
            bg.type = Image.Type.Sliced;
            var bgRt = bg.GetComponent<RectTransform>();
            UIFactory.StretchFill(bgRt);

            // Stats text
            _statsText = UIFactory.CreateText("Stats", transform, "",
                11f, Color.white, TextAlignmentOptions.TopLeft);
            var textRt = _statsText.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 6);
            textRt.offsetMax = new Vector2(-10, -6);
            _statsText.enableAutoSizing = true;
            _statsText.fontSizeMin = 8f;
            _statsText.fontSizeMax = 11f;
        }

        public void UpdateTracker(SessionTracker tracker)
        {
            _sessionTracker = tracker;
        }

        public void ShowForSeat(int seat, Vector2 position)
        {
            if (_sessionTracker == null) return;
            var session = _sessionTracker.GetSession(seat);
            if (session == null || session.HandsPlayed == 0) return;

            // Cancel any pending dismiss
            _showTween?.Cancel();
            _dismissTween?.Cancel();

            _isVisible = true;
            _showTime = Time.time;
            gameObject.SetActive(true);

            // Position above the seat
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.anchoredPosition = position + new Vector2(0, 60);

            // Format stats
            float delta = _sessionTracker.GetDelta(seat);
            float winPct = session.HandsPlayed > 0
                ? (float)session.HandsWon / session.HandsPlayed * 100f
                : 0f;
            string deltaStr = delta >= 0
                ? $"<color=#10B981>+${delta:F0}</color>"
                : $"<color=#EF4444>-${-delta:F0}</color>";

            _statsText.text = $"Hands: {session.HandsPlayed}\n" +
                              $"Won: {winPct:F0}%\n" +
                              $"Biggest: ${session.BiggestPot:F0}\n" +
                              $"P/L: {deltaStr}";

            // Animated entrance
            _canvasGroup.alpha = 0f;
            _rt.localScale = Vector3.one * 0.8f;

            if (_animController != null)
            {
                _showTween = _animController.Play(Tweener.TweenAlpha(
                    _canvasGroup, 0f, 1f, 0.15f));
                _animController.Play(Tweener.TweenScale(
                    _rt, Vector3.one * 0.8f, Vector3.one, 0.2f, EaseType.EaseOutBack));
            }
            else
            {
                _canvasGroup.alpha = 1f;
                _rt.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (_isVisible && Time.time - _showTime > AutoDismissSeconds)
                Hide(true);
        }

        private void Hide(bool animate)
        {
            _isVisible = false;
            _showTween?.Cancel();

            if (animate && _animController != null)
            {
                _dismissTween = _animController.Play(Tweener.TweenAlpha(
                    _canvasGroup, _canvasGroup.alpha, 0f, 0.15f));
                _dismissTween.OnComplete(() =>
                {
                    if (gameObject != null) gameObject.SetActive(false);
                });
            }
            else
            {
                _canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
            }
        }
    }
}
