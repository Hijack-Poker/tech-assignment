using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Object pool for sparkle/shimmer particle elements.
    /// Pre-allocates reusable GameObjects to avoid creating and destroying
    /// 120+ transient objects per win sequence.
    /// </summary>
    public class ParticlePool : MonoBehaviour
    {
        private static ParticlePool _instance;

        public static ParticlePool Instance
        {
            get
            {
                if (_instance == null) Initialize();
                return _instance;
            }
        }

        private readonly Stack<GameObject> _available = new();
        private const int WarmUpCount = 130;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; }

        private static void Initialize()
        {
            var go = new GameObject("[ParticlePool]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            _instance = go.AddComponent<ParticlePool>();
            _instance.WarmUp();
        }

        private void WarmUp()
        {
            for (int i = 0; i < WarmUpCount; i++)
                _available.Push(CreateElement());
        }

        private GameObject CreateElement()
        {
            var go = new GameObject("PooledSparkle", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.AddComponent<Image>();
            go.AddComponent<CanvasGroup>();
            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Get a sparkle element from the pool, configured and activated.
        /// </summary>
        public GameObject Rent(Transform parent, Vector2 position, float size, Color color)
        {
            var go = _available.Count > 0 ? _available.Pop() : CreateElement();

            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(size, size);
            rt.localEulerAngles = new Vector3(0, 0, 45);
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = null;
            img.raycastTarget = false;

            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 1f;

            go.SetActive(true);
            return go;
        }

        /// <summary>
        /// Return a sparkle element to the pool for reuse.
        /// </summary>
        public void Return(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _available.Push(go);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
