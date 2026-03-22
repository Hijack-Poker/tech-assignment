using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace HijackPoker.UI
{
    public class SoundToggleView : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private Image _speakerBody;
        private Image _speakerCone;
        private Image _wave1;
        private Image _wave2;
        private Image _wave3;
        private Image _muteLine;
        private CanvasGroup _wavesGroup;

        private bool _isMuted;
        private readonly int _tweenId = "SoundToggle".GetHashCode();

        private static readonly Color SpeakerColor = new Color(0.78f, 0.88f, 1f);
        private static readonly Color WaveColor = new Color(0.42f, 0.75f, 1f);
        private static readonly Color MuteLineColor = new Color(0.92f, 0.28f, 0.28f);
        private static readonly Color BgNormal = new Color(0.06f, 0.10f, 0.16f, 0.85f);
        private static readonly Color BgMuted = new Color(0.18f, 0.06f, 0.06f, 0.85f);

        private void Awake()
        {
            _isMuted = PlayerPrefs.GetInt("SoundMuted", 0) == 1;
            BuildIcon();
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(Toggle);
            ApplyState(false);
        }

        private void Toggle()
        {
            _isMuted = !_isMuted;
            PlayerPrefs.SetInt("SoundMuted", _isMuted ? 1 : 0);
            ApplyState(true);
        }

        private void ApplyState(bool animate)
        {
            AudioListener.volume = _isMuted ? 0f : 1f;
            DOTween.Kill(_tweenId);

            float dur = animate ? 0.35f : 0f;
            var bg = GetComponent<Image>();

            if (bg != null)
            {
                Color target = _isMuted ? BgMuted : BgNormal;
                if (animate)
                    TweenColor(bg, target, dur);
                else
                    bg.color = target;
            }

            // Waves fade
            if (_wavesGroup != null)
            {
                float targetAlpha = _isMuted ? 0f : 1f;
                if (animate)
                {
                    DOVirtual.Float(_wavesGroup.alpha, targetAlpha, dur, v => { if (_wavesGroup) _wavesGroup.alpha = v; })
                        .SetId(_tweenId);
                    if (!_isMuted) AnimateWaves();
                }
                else
                    _wavesGroup.alpha = targetAlpha;
            }

            // Mute slash line
            if (_muteLine != null)
            {
                var muteRT = _muteLine.rectTransform;
                if (_isMuted)
                {
                    _muteLine.gameObject.SetActive(true);
                    if (animate)
                    {
                        muteRT.localScale = new Vector3(0f, 1f, 1f);
                        muteRT.DOScaleX(1f, dur).SetEase(Ease.OutBack).SetId(_tweenId);
                        TweenAlpha(_muteLine, 0f, 1f, dur * 0.5f);
                    }
                    else
                    {
                        muteRT.localScale = Vector3.one;
                        _muteLine.color = MuteLineColor;
                    }
                }
                else
                {
                    if (animate)
                    {
                        muteRT.DOScaleX(0f, dur * 0.6f).SetEase(Ease.InBack).SetId(_tweenId)
                            .OnComplete(() => { if (_muteLine) _muteLine.gameObject.SetActive(false); });
                    }
                    else
                        _muteLine.gameObject.SetActive(false);
                }
            }

            // Speaker body punch on toggle
            if (animate && _speakerBody != null)
            {
                _speakerBody.transform.DOKill();
                _speakerBody.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 10, 0.5f);
            }
        }

        private void AnimateWaves()
        {
            if (_wave1 == null) return;
            Image[] waves = { _wave1, _wave2, _wave3 };
            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i] == null) continue;
                var t = waves[i].transform;
                t.DOKill();
                t.localScale = new Vector3(0.5f, 0.5f, 1f);
                var c = waves[i].color;
                waves[i].color = new Color(c.r, c.g, c.b, 0f);
                float delay = i * 0.12f;
                t.DOScale(Vector3.one, 0.4f).SetDelay(delay).SetEase(Ease.OutBack);
                var img = waves[i];
                float targetA = i == 0 ? 0.9f : (i == 1 ? 0.6f : 0.35f);
                TweenAlpha(img, 0f, targetA, 0.35f, delay);
            }
        }

        private void TweenColor(Image img, Color target, float duration)
        {
            Color from = img.color;
            DOVirtual.Float(0f, 1f, duration, t => { if (img) img.color = Color.Lerp(from, target, t); })
                .SetId(_tweenId);
        }

        private void TweenAlpha(Image img, float from, float to, float duration, float delay = 0f)
        {
            var c = img.color;
            img.color = new Color(c.r, c.g, c.b, from);
            DOVirtual.Float(from, to, duration, a => { if (img) { var cc = img.color; img.color = new Color(cc.r, cc.g, cc.b, a); } })
                .SetDelay(delay).SetId(_tweenId);
        }

        private void BuildIcon()
        {
            var iconRoot = new GameObject("SpeakerIcon", typeof(RectTransform));
            iconRoot.transform.SetParent(transform, false);
            var iconRT = iconRoot.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(6, 6); iconRT.offsetMax = new Vector2(-6, -6);

            // Speaker body (small rectangle, left side)
            _speakerBody = CreateRect(iconRoot.transform, "Body",
                new Vector2(0.05f, 0.3f), new Vector2(0.3f, 0.7f),
                Vector2.zero, Vector2.zero, SpeakerColor);

            // Speaker cone (wider trapezoid-like shape)
            _speakerCone = CreateRect(iconRoot.transform, "Cone",
                new Vector2(0.22f, 0.12f), new Vector2(0.48f, 0.88f),
                Vector2.zero, Vector2.zero, SpeakerColor);

            // Waves container (right half)
            var wavesGO = new GameObject("Waves", typeof(RectTransform), typeof(CanvasGroup));
            wavesGO.transform.SetParent(iconRoot.transform, false);
            var wavesRT = wavesGO.GetComponent<RectTransform>();
            wavesRT.anchorMin = new Vector2(0.48f, 0f);
            wavesRT.anchorMax = Vector2.one;
            wavesRT.offsetMin = Vector2.zero; wavesRT.offsetMax = Vector2.zero;
            _wavesGroup = wavesGO.GetComponent<CanvasGroup>();

            // Three wave bars at increasing distance (closest=thickest, farthest=thinnest)
            _wave1 = CreateWaveBar(wavesGO.transform, "Wave1", 0.08f, 0.18f, 0.7f, WaveColor, 0.9f);
            _wave2 = CreateWaveBar(wavesGO.transform, "Wave2", 0.34f, 0.14f, 0.52f, WaveColor, 0.6f);
            _wave3 = CreateWaveBar(wavesGO.transform, "Wave3", 0.58f, 0.10f, 0.36f, WaveColor, 0.35f);

            // Mute line (red diagonal slash across the whole icon)
            var muteGO = new GameObject("MuteLine", typeof(RectTransform), typeof(Image));
            muteGO.transform.SetParent(iconRoot.transform, false);
            _muteLine = muteGO.GetComponent<Image>();
            _muteLine.color = MuteLineColor;
            _muteLine.raycastTarget = false;
            var muteRT = muteGO.GetComponent<RectTransform>();
            muteRT.anchorMin = new Vector2(0.05f, 0.5f);
            muteRT.anchorMax = new Vector2(0.95f, 0.5f);
            muteRT.sizeDelta = new Vector2(0, 3f);
            muteRT.localRotation = Quaternion.Euler(0, 0, -45f);
            _muteLine.gameObject.SetActive(false);
        }

        private static Image CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 oMin, Vector2 oMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            return img;
        }

        private static Image CreateWaveBar(Transform parent, string name,
            float xOffset, float width, float height, Color color, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, alpha);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xOffset, 0.5f - height * 0.5f);
            rt.anchorMax = new Vector2(xOffset + width, 0.5f + height * 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }

        private void OnDestroy()
        {
            DOTween.Kill(_tweenId);
            if (_speakerBody != null) _speakerBody.transform.DOKill();
            if (_wave1 != null) _wave1.transform.DOKill();
            if (_wave2 != null) _wave2.transform.DOKill();
            if (_wave3 != null) _wave3.transform.DOKill();
            if (_muteLine != null) _muteLine.transform.DOKill();
        }
    }
}
