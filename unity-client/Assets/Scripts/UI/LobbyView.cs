using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Api;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Lobby view shown before connecting to a table.
    /// Deep blue gradient background, title with elastic entrance,
    /// fanned procedural cards, live table preview cards, and sparkle effects.
    /// </summary>
    public class LobbyView : MonoBehaviour
    {
        public event Action<int> OnTableSelected;

        private RectTransform _rt;
        private CanvasGroup _canvasGroup;
        private AnimationController _animController;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private TablePreviewCard[] _previews;
        private PokerApiClient _apiClient;
        private Coroutine _pollCoroutine;
        private bool _destroyed;
        private Transform _sparkleLayer;

        public static LobbyView Create(Transform parent, AnimationController anim)
        {
            var go = new GameObject("LobbyView", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.StretchFill(rt);

            var view = go.AddComponent<LobbyView>();
            view._rt = rt;
            view._animController = anim;
            view._canvasGroup = go.AddComponent<CanvasGroup>();
            view.BuildUI();
            view.PlayEntrance();
            view.StartPolling();
            return view;
        }

        private void BuildUI()
        {
            // Deep blue gradient background
            var bg = UIFactory.CreateImage("WelcomeBg", transform, Color.white);
            bg.sprite = TextureGenerator.GetVerticalGradient(
                64, 128,
                new Color(0.08f, 0.12f, 0.28f, 1f),
                new Color(0.04f, 0.06f, 0.16f, 1f),
                0);
            bg.type = Image.Type.Sliced;
            var bgRt = bg.GetComponent<RectTransform>();
            UIFactory.StretchFill(bgRt);

            // Vignette overlay to darken corners and focus the eye
            var vignette = UIFactory.CreateImage("Vignette", transform, Color.white);
            vignette.sprite = TextureGenerator.GetVignette(64, 64, 1.5f);
            var vignetteRt = vignette.GetComponent<RectTransform>();
            UIFactory.StretchFill(vignetteRt);

            // Radial center highlight for subtle spotlight effect
            var radial = UIFactory.CreateImage("RadialHighlight", transform, Color.white);
            radial.sprite = TextureGenerator.GetRadialGradient(64,
                new Color(0.15f, 0.20f, 0.35f, 0.3f),
                new Color(0f, 0f, 0f, 0f));
            var radialRt = radial.GetComponent<RectTransform>();
            radialRt.anchorMin = new Vector2(0.1f, 0.1f);
            radialRt.anchorMax = new Vector2(0.9f, 0.9f);
            radialRt.offsetMin = Vector2.zero;
            radialRt.offsetMax = Vector2.zero;

            // Sparkle layer — renders behind content so particles don't bleed over UI
            var sparkleLayerGo = new GameObject("SparkleLayer", typeof(RectTransform));
            sparkleLayerGo.transform.SetParent(transform, false);
            UIFactory.StretchFill(sparkleLayerGo.GetComponent<RectTransform>());
            _sparkleLayer = sparkleLayerGo.transform;

            // Center content container
            var center = new GameObject("CenterContent", typeof(RectTransform));
            center.transform.SetParent(transform, false);
            var centerRt = center.GetComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.05f, 0.04f);
            centerRt.anchorMax = new Vector2(0.95f, 0.96f);
            centerRt.offsetMin = Vector2.zero;
            centerRt.offsetMax = Vector2.zero;

            var vlg = center.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 10;

            // Fanned cards behind title
            var cardsRow = new GameObject("CardsRow", typeof(RectTransform));
            cardsRow.transform.SetParent(center.transform, false);
            var cardsLE = cardsRow.AddComponent<LayoutElement>();
            cardsLE.preferredHeight = 90;
            cardsLE.preferredWidth = 200;

            var card1 = CardView.Create("WelcomeCard1", cardsRow.transform);
            card1.SetFaceUp("AH");
            card1.RectTransform.anchoredPosition = new Vector2(-20, 0);
            card1.RectTransform.localEulerAngles = new Vector3(0, 0, 12f);
            card1.RectTransform.localScale = Vector3.one * 0.9f;

            var card2 = CardView.Create("WelcomeCard2", cardsRow.transform);
            card2.SetFaceDown();
            card2.RectTransform.anchoredPosition = new Vector2(20, 0);
            card2.RectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            card2.RectTransform.localScale = Vector3.one * 0.9f;

            // Title
            _titleText = UIFactory.CreateText("Title", center.transform, "HIJACK POKER",
                28f, new Color(1f, 0.84f, 0f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            var titleLE = _titleText.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 40;
            titleLE.preferredWidth = 400;
            _titleText.transform.localScale = Vector3.zero;

            // Subtitle
            _subtitleText = UIFactory.CreateText("Subtitle", center.transform,
                "Select a table to watch",
                14f, new Color(0.7f, 0.75f, 0.85f, 0.8f), TextAlignmentOptions.Center);
            var subtitleLE = _subtitleText.gameObject.AddComponent<LayoutElement>();
            subtitleLE.preferredHeight = 22;
            subtitleLE.preferredWidth = 400;
            _subtitleText.transform.localScale = Vector3.zero;

            // Table preview cards — 2x2 grid
            var previewGrid = new GameObject("PreviewGrid", typeof(RectTransform));
            previewGrid.transform.SetParent(center.transform, false);
            var gridLE = previewGrid.AddComponent<LayoutElement>();
            gridLE.preferredWidth = 380;
            gridLE.flexibleWidth = 1;
            gridLE.flexibleHeight = 1;

            var gridVlg = previewGrid.AddComponent<VerticalLayoutGroup>();
            gridVlg.spacing = 14;
            gridVlg.childAlignment = TextAnchor.MiddleCenter;
            gridVlg.childControlWidth = true;
            gridVlg.childControlHeight = true;
            gridVlg.childForceExpandWidth = true;
            gridVlg.childForceExpandHeight = true;

            _previews = new TablePreviewCard[4];
            for (int row = 0; row < 2; row++)
            {
                var rowGo = new GameObject($"PreviewRow{row}", typeof(RectTransform));
                rowGo.transform.SetParent(previewGrid.transform, false);

                var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 14;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;

                for (int col = 0; col < 2; col++)
                {
                    int tableId = row * 2 + col + 1;
                    var theme = TableTheme.ForTable(tableId);
                    var preview = TablePreviewCard.Create(tableId, rowGo.transform, _animController, theme);
                    preview.OnClicked += id => OnTableSelected?.Invoke(id);
                    var pLE = preview.gameObject.AddComponent<LayoutElement>();
                    pLE.flexibleWidth = 1;
                    _previews[tableId - 1] = preview;
                }
            }
        }

        private void StartPolling()
        {
            // Create a temporary API client for lobby REST calls
            _apiClient = gameObject.AddComponent<PokerApiClient>();
            _pollCoroutine = StartCoroutine(PollTableStates());
        }

        private IEnumerator PollTableStates()
        {
            // Fetch immediately, then poll every 3 seconds
            while (!_destroyed)
            {
                for (int i = 0; i < _previews.Length; i++)
                    FetchTableState(i + 1, _previews[i]);
                yield return new WaitForSeconds(3f);
            }
        }

        private async void FetchTableState(int tableId, TablePreviewCard preview)
        {
            if (_destroyed || _apiClient == null) return;

            try
            {
                var state = await _apiClient.GetTableStateAsync(tableId);
                if (!_destroyed && state != null)
                {
                    preview.UpdateFromState(state);
                }
                else if (!_destroyed)
                {
                    // Server returned null — use mock
                    var mock = MockStateFactory.CreateMockState();
                    mock.Game.TableId = tableId;
                    mock.Game.TableName = $"Table {tableId}";
                    preview.UpdateFromState(mock);
                }
            }
            catch
            {
                if (_destroyed) return;
                // Server unavailable — use mock fallback
                var mock = MockStateFactory.CreateMockState();
                mock.Game.TableId = tableId;
                mock.Game.TableName = $"Table {tableId}";
                preview.UpdateFromState(mock);
            }
        }

        private void PlayEntrance()
        {
            if (_animController == null) return;

            // Title elastic entrance
            _animController.Play(Tweener.TweenScale(
                _titleText.transform, Vector3.zero, Vector3.one, 0.8f, EaseType.EaseOutElastic));

            // Subtitle entrance (slightly delayed)
            StartCoroutine(DelayedSubtitleEntrance());

            // Staggered card entrance
            for (int i = 0; i < _previews.Length; i++)
                _previews[i]?.PlayEntrance(0.3f + i * 0.15f);

            // Sparkle effects on entrance (parented to sparkle layer behind content)
            SparkleEffects.SpawnSparkles(_sparkleLayer, Vector2.zero, 14,
                new Color(1f, 0.84f, 0f, 0.8f), 120f, 1.0f, _animController);

            AudioManager.Instance?.Play(SoundType.Sparkle);

            // Ambient sparkle loop to keep the lobby feeling alive
            StartCoroutine(AmbientSparkleLoop());
        }

        private IEnumerator DelayedSubtitleEntrance()
        {
            yield return new WaitForSeconds(0.2f);
            if (!_destroyed && _animController != null)
                _animController.Play(Tweener.TweenScale(
                    _subtitleText.transform, Vector3.zero, Vector3.one, 0.6f, EaseType.EaseOutElastic));
        }

        private IEnumerator AmbientSparkleLoop()
        {
            while (!_destroyed)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(6f, 10f));
                if (_destroyed || _animController == null) break;
                SparkleEffects.SpawnSparkles(_sparkleLayer,
                    new Vector2(UnityEngine.Random.Range(-150f, 150f),
                        UnityEngine.Random.Range(-80f, 80f)),
                    6, new Color(1f, 0.84f, 0f, 0.6f), 60f, 0.8f, _animController);
            }
        }

        public void FadeOutAndDestroy(Action onComplete)
        {
            _destroyed = true;
            StopPolling();

            if (_animController != null)
            {
                var tween = _animController.Play(Tweener.TweenAlpha(
                    _canvasGroup, 1f, 0f, 0.3f));
                tween.OnComplete(() =>
                {
                    onComplete?.Invoke();
                    Destroy(gameObject);
                });
            }
            else
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            }
        }

        private void StopPolling()
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }
        }

        public void SetAutoPlayIndicator(int tableId, bool active)
        {
            int idx = tableId - 1;
            if (idx >= 0 && idx < _previews.Length && _previews[idx] != null)
                _previews[idx].SetAutoPlayIndicator(active);
        }

        private void OnDestroy()
        {
            _destroyed = true;
            StopPolling();
        }
    }
}
