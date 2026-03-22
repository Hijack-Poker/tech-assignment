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
        [SerializeField] private Image _avatarBtnTemplate;

        private static readonly Color DisabledBtn = new Color(0.20f, 0.25f, 0.32f);
        private static readonly Color EnabledBtn = new Color(0.09f, 0.64f, 0.43f);
        private static readonly Color TableSelectedColor = new Color(0.09f, 0.64f, 0.43f);
        private static readonly Color TableUnselectedColor = new Color(0.15f, 0.22f, 0.30f);

        private Sequence _entranceSeq;
        private Tween _buttonPulse;
        private Tween _buttonColorTween;
        private Tween _cardFloat;
        private bool _transitioning;
        private string _selectedAvatar = "";
        private int _selectedTableId = 1;

        // Table selection UI
        private Button _table1Button;
        private Button _table2Button;
        private Image _table1Image;
        private Image _table2Image;

        private void Start()
        {
            // Start everything invisible
            _title.alpha = 0f;
            _subtitle.alpha = 0f;
            _playButton.GetComponent<CanvasGroup>().alpha = 0f;
            _nameInputGroup.alpha = 0f;
            if (_avatarGridGroup != null) _avatarGridGroup.alpha = 0f;
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

            // 6. Avatar section fades in
            if (_avatarGridGroup != null)
                _entranceSeq.Append(FadeCG(_avatarGridGroup, 1f, 0.3f));

            // 7. Play button (starts disabled)
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
            CreateTableSelector();
        }

        private void PopulateAvatarGrid()
        {
            if (_avatarGrid == null || _avatarBtnTemplate == null) return;
            _avatarBtnTemplate.gameObject.SetActive(false);

            var allAvatars = Resources.LoadAll<Sprite>("Avatars");
            if (allAvatars.Length == 0) return;

            // Shuffle and pick 4
            var list = new System.Collections.Generic.List<Sprite>(allAvatars);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            int count = Mathf.Min(4, list.Count);

            for (int i = 0; i < count; i++)
            {
                var sprite = list[i];
                var go = Instantiate(_avatarBtnTemplate.gameObject, _avatarGrid);
                go.SetActive(true);
                go.name = sprite.name;
                var avatarImg = go.transform.Find("Icon")?.GetComponent<Image>();
                if (avatarImg != null)
                {
                    avatarImg.sprite = sprite;
                    avatarImg.color = Color.white;
                }
                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                string spriteName = sprite.name;
                btn.onClick.AddListener(() => SelectAvatar(spriteName));
            }

            // Pre-select first
            SelectAvatar(list[0].name);
        }

        private void SelectAvatar(string name)
        {
            _selectedAvatar = name;
            if (_avatarGrid == null) return;

            foreach (Transform child in _avatarGrid)
            {
                if (!child.gameObject.activeSelf) continue;
                bool isSelected = child.name == name;
                var outline = child.GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = isSelected
                        ? new Color(0.39f, 0.87f, 0.86f, 1f)
                        : new Color(1, 1, 1, 0);
                // Punch the selected one
                if (isSelected)
                {
                    child.DOKill();
                    child.localScale = Vector3.one;
                    child.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.5f);
                }
                else
                {
                    child.DOKill();
                    child.localScale = Vector3.one;
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

        private void CreateTableSelector()
        {
            // Place below the avatar grid, above the play button
            var canvas = GetComponentInParent<Canvas>();
            Transform parent = _playButton.transform.parent;

            var container = new GameObject("TableSelector", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            container.transform.SetParent(parent, false);
            // Position above the play button
            var containerRt = container.GetComponent<RectTransform>();
            var playRt = _playButton.GetComponent<RectTransform>();
            containerRt.anchorMin = playRt.anchorMin;
            containerRt.anchorMax = playRt.anchorMax;
            containerRt.pivot = playRt.pivot;
            containerRt.sizeDelta = new Vector2(340f, 50f);
            containerRt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, 70f);

            var hlg = container.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGO.transform.SetParent(container.transform, false);
            var labelTxt = labelGO.GetComponent<TextMeshProUGUI>();
            labelTxt.text = "TABLE:";
            labelTxt.fontSize = 14;
            labelTxt.fontStyle = FontStyles.Bold;
            labelTxt.alignment = TextAlignmentOptions.MidlineRight;
            labelTxt.color = new Color(0.7f, 0.75f, 0.8f);
            var labelLe = labelGO.GetComponent<LayoutElement>();
            labelLe.preferredWidth = 60f;

            _table1Button = CreateTableButton(container.transform, "Starter\n$1/$2", 1);
            _table2Button = CreateTableButton(container.transform, "High Stakes\n$5/$10", 2);
            _table1Image = _table1Button.GetComponent<Image>();
            _table2Image = _table2Button.GetComponent<Image>();

            SelectTable(1);
        }

        private Button CreateTableButton(Transform parent, string label, int tableId)
        {
            var go = new GameObject($"Table{tableId}Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = TableUnselectedColor;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 120f;
            le.preferredHeight = 44f;

            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 13;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectTable(tableId));
            return btn;
        }

        private void SelectTable(int tableId)
        {
            _selectedTableId = tableId;
            if (_table1Image != null) _table1Image.color = tableId == 1 ? TableSelectedColor : TableUnselectedColor;
            if (_table2Image != null) _table2Image.color = tableId == 2 ? TableSelectedColor : TableUnselectedColor;
        }

        private void OnPlayClicked()
        {
            if (_transitioning) return;
            _transitioning = true;

            // Store the player name, avatar, and table for the game scene
            string playerName = _nameInput.text.Trim();
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetString("PlayerAvatar", _selectedAvatar);
            PlayerPrefs.SetInt("TableId", _selectedTableId);
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
