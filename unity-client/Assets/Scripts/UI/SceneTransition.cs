using System;
using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Animation;

namespace HijackPoker.UI
{
    /// <summary>
    /// Full-screen overlay for fade-to-black / fade-from-black transitions.
    /// Static Create() factory spawns a self-contained overlay with CanvasGroup.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private AnimationController _animController;

        public static SceneTransition Create(Transform parent, AnimationController anim)
        {
            var go = new GameObject("SceneTransition", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.StretchFill(rt);

            // Black overlay
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            var view = go.AddComponent<SceneTransition>();
            view._canvasGroup = cg;
            view._animController = anim;
            return view;
        }

        public void FadeToBlack(float duration, Action onComplete)
        {
            _canvasGroup.blocksRaycasts = true;

            if (_animController != null)
            {
                var tween = _animController.Play(
                    Tweener.TweenAlpha(_canvasGroup, 0f, 1f, duration));
                tween.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                _canvasGroup.alpha = 1f;
                onComplete?.Invoke();
            }
        }

        public void FadeFromBlack(float duration, Action onComplete)
        {
            _canvasGroup.alpha = 1f;

            if (_animController != null)
            {
                var tween = _animController.Play(
                    Tweener.TweenAlpha(_canvasGroup, 1f, 0f, duration));
                tween.OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                onComplete?.Invoke();
            }
        }

        public void Cleanup()
        {
            if (gameObject != null)
                Destroy(gameObject);
        }
    }
}
