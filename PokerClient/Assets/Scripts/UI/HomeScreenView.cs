using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

namespace HijackPoker.UI
{
    public class HomeScreenView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _subtitle;
        [SerializeField] private Button _playButton;
        [SerializeField] private Image _playButtonImage;
        [SerializeField] private Image _heroCard1;
        [SerializeField] private Image _heroCard2;
        [SerializeField] private CanvasGroup _scatteredGroup;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private CanvasGroup _nameInputGroup;
        [SerializeField] private Image _fadeOverlay;

        [Header("Avatar Selection")]
        [SerializeField] private Transform _avatarGrid;
        [SerializeField] private CanvasGroup _avatarGridGroup;
        [SerializeField] private Image _selectedAvatarPreview;
        [SerializeField] private Image _avatarBtnTemplate;

        private static readonly Color DisabledBtn = new Color(0.20f, 0.25f, 0.32f);
        private static readonly Color EnabledBtn = new Color(0.09f, 0.64f, 0.43f);

        private Sequence _entranceSeq;
        private Tween _buttonPulse;
        private Tween _buttonColorTween;
        private Tween _cardFloat;
        private bool _transitioning;
        private string _selectedAvatar = "";

        private void Start()
        {
            // Start everything invisible
            _title.alpha = 0f;
            _subtitle.alpha = 0f;
            _playButton.GetComponent<CanvasGroup>().alpha = 0f;
            _nameInputGroup.alpha = 0f;
            _heroCard1.color = new Color(1, 1, 1, 0);
            _heroCard2.color = new Color(1, 1, 1, 0);
            _scatteredGroup.alpha = 0f;
            _fadeOverlay.color = new Color(0, 0, 0, 0);

            // Fan cards out from center
            _heroCard1.rectTransform.localRotation = Quaternion.identity;
            _heroCard2.rectTransform.localRotation = Quaternion.identity;

            _entranceSeq = DOTween.Sequence();

            // 1. Scattered decorations fade in
            _entranceSeq.Append(FadeCG(_scatteredGroup, 1f, 0.6f));

            // 2. Title drops in
            _entranceSeq.Append(FadeTMP(_title, 1f, 0.4f));
            var titleStartY = _title.rectTransform.anchoredPosition.y;
            _title.rectTransform.anchoredPosition = new Vector2(
                _title.rectTransform.anchoredPosition.x, titleStartY + 30);
            _entranceSeq.Join(
                DOTween.To(() => _title.rectTransform.anchoredPosition,
                    v => _title.rectTransform.anchoredPosition = v,
                    new Vector2(_title.rectTransform.anchoredPosition.x, titleStartY), 0.4f)
                .SetEase(Ease.OutBack));

            // 3. Subtitle
            _entranceSeq.Append(FadeTMP(_subtitle, 1f, 0.3f));

            // 4. Hero cards fan out
            _entranceSeq.Append(FadeImage(_heroCard1, 1f, 0.3f));
            _entranceSeq.Join(_heroCard1.rectTransform.DOLocalRotate(new Vector3(0, 0, -8f), 0.4f)
                .SetEase(Ease.OutBack));
            _entranceSeq.Join(FadeImage(_heroCard2, 1f, 0.3f));
            _entranceSeq.Join(_heroCard2.rectTransform.DOLocalRotate(new Vector3(0, 0, 8f), 0.4f)
                .SetEase(Ease.OutBack));

            // 5. Name input
            _entranceSeq.Append(FadeCG(_nameInputGroup, 1f, 0.3f));

            // 6. Play button (starts disabled)
            _playButton.interactable = false;
            _playButtonImage.color = DisabledBtn;
            var btnCG = _playButton.GetComponent<CanvasGroup>();
            _entranceSeq.Append(FadeCG(btnCG, 1f, 0.3f));
            _entranceSeq.Join(_playButton.transform.DOScale(1f, 0.3f).From(0.7f).SetEase(Ease.OutBack));

            _entranceSeq.OnComplete(StartIdleAnimations);

            _nameInput.onValueChanged.AddListener(OnNameChanged);
            _playButton.onClick.AddListener(OnPlayClicked);

            // Populate avatar grid
            PopulateAvatarGrid();
        }

        private void PopulateAvatarGrid()
        {
            if (_avatarGrid == null || _avatarBtnTemplate == null) return;
            _avatarBtnTemplate.gameObject.SetActive(false);

            var avatars = Resources.LoadAll<Sprite>("Avatars");
            foreach (var sprite in avatars)
            {
                var go = Instantiate(_avatarBtnTemplate.gameObject, _avatarGrid);
                go.SetActive(true);
                var img = go.GetComponent<Image>();
                // Find the child avatar image
                var avatarImg = go.transform.Find("Icon")?.GetComponent<Image>();
                if (avatarImg != null)
                {
                    avatarImg.sprite = sprite;
                    avatarImg.color = Color.white;
                }
                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                string spriteName = sprite.name;
                btn.onClick.AddListener(() => SelectAvatar(spriteName, sprite));
            }

            // Pre-select first avatar
            if (avatars.Length > 0)
                SelectAvatar(avatars[0].name, avatars[0]);
        }

        private void SelectAvatar(string name, Sprite sprite)
        {
            _selectedAvatar = name;
            if (_selectedAvatarPreview != null)
            {
                _selectedAvatarPreview.sprite = sprite;
                _selectedAvatarPreview.color = Color.white;
            }
            // Highlight selected in grid
            if (_avatarGrid != null)
            {
                foreach (Transform child in _avatarGrid)
                {
                    if (!child.gameObject.activeSelf) continue;
                    var outline = child.GetComponent<Outline>();
                    var iconImg = child.Find("Icon")?.GetComponent<Image>();
                    bool isSelected = iconImg != null && iconImg.sprite != null && iconImg.sprite.name == name;
                    if (outline != null)
                        outline.effectColor = isSelected
                            ? new Color(0.39f, 0.87f, 0.86f, 1f)
                            : new Color(1, 1, 1, 0);
                }
            }
        }

        private void OnNameChanged(string value)
        {
            bool hasName = !string.IsNullOrWhiteSpace(value);
            _playButton.interactable = hasName;
            _buttonColorTween?.Kill();
            _buttonColorTween = TweenImageColor(_playButtonImage, hasName ? EnabledBtn : DisabledBtn, 0.2f);

            if (hasName && _buttonPulse == null)
                StartButtonPulse();
            else if (!hasName && _buttonPulse != null)
            {
                _buttonPulse.Kill();
                _buttonPulse = null;
                _playButtonImage.transform.localScale = Vector3.one;
            }
        }

        private void StartButtonPulse()
        {
            _buttonPulse = _playButtonImage.transform
                .DOScale(1.04f, 1.2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StartIdleAnimations()
        {
            var cardsParent = _heroCard1.transform.parent;
            _cardFloat = cardsParent
                .DOLocalMoveY(cardsParent.localPosition.y + 6f, 2.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnPlayClicked()
        {
            if (_transitioning) return;
            _transitioning = true;

            // Store the player name and avatar for the game scene
            string playerName = _nameInput.text.Trim();
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetString("PlayerAvatar", _selectedAvatar);
            PlayerPrefs.Save();

            _buttonPulse?.Kill();
            _cardFloat?.Kill();

            _playButton.transform.DOScale(0.92f, 0.1f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                _fadeOverlay.raycastTarget = true;
                FadeImage(_fadeOverlay, 1f, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    SceneManager.LoadScene("PokerTable");
                });
            });
        }

        private void OnDestroy()
        {
            _entranceSeq?.Kill();
            _buttonPulse?.Kill();
            _buttonColorTween?.Kill();
            _cardFloat?.Kill();
        }

        // DOTween helpers for types without built-in shortcuts
        private static Tween FadeCG(CanvasGroup cg, float to, float dur)
        {
            return DOTween.To(() => cg.alpha, v => cg.alpha = v, to, dur).SetEase(Ease.OutQuad);
        }

        private static Tween FadeTMP(TextMeshProUGUI tmp, float to, float dur)
        {
            return DOTween.To(() => tmp.alpha, v => tmp.alpha = v, to, dur).SetEase(Ease.OutQuad);
        }

        private static Tween FadeImage(Image img, float to, float dur)
        {
            return DOTween.To(() => img.color.a, v =>
            {
                var c = img.color;
                c.a = v;
                img.color = c;
            }, to, dur).SetEase(Ease.OutQuad);
        }

        private static Tween TweenImageColor(Image img, Color to, float dur)
        {
            return DOTween.To(() => img.color, v => img.color = v, to, dur).SetEase(Ease.OutQuad);
        }
    }
}
