using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HijackPoker.UI
{
    /// <summary>
    /// Reusable avatar circle: colored circle with initial letter inside a ring.
    /// Border color changes by state (idle/active/winner). No rotating gradient.
    /// </summary>
    public class AvatarCircleView : MonoBehaviour
    {
        private Image _avatarImage;
        private TextMeshProUGUI _initialsText;
        private AvatarBorderController _border;
        private RectTransform _ringTransform;
        private int _currentPlayerId = -1;

        public Image AvatarImage => _avatarImage;
        public TextMeshProUGUI InitialsText => _initialsText;
        public AvatarBorderController Border => _border;
        public RectTransform RingTransform => _ringTransform;

        private const int TexResolution = 88;

        public static AvatarCircleView Create(Transform parent, float size, bool includeBorder = true)
        {
            var go = new GameObject("AvatarCircle", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            var view = go.AddComponent<AvatarCircleView>();
            view.BuildUI(size, includeBorder);
            return view;
        }

        private void BuildUI(float size, bool includeBorder)
        {
            float innerSize = size - 4f;

            // Avatar ring — visible ring (subtle on dark bg)
            var ringGo = new GameObject("AvatarRing", typeof(RectTransform));
            ringGo.transform.SetParent(transform, false);
            _ringTransform = ringGo.GetComponent<RectTransform>();
            UIFactory.SetAnchor(_ringTransform, 0.5f, 0.5f);
            _ringTransform.sizeDelta = new Vector2(size, size);
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.color = new Color(1f, 1f, 1f, 0.10f);
            ringImg.sprite = TextureGenerator.GetCircle((int)size);
            ringImg.raycastTarget = false;

            // Avatar image — colored circle
            var avatarGo = new GameObject("Avatar", typeof(RectTransform));
            avatarGo.transform.SetParent(ringGo.transform, false);
            var avatarRt = avatarGo.GetComponent<RectTransform>();
            avatarRt.anchoredPosition = Vector2.zero;
            avatarRt.sizeDelta = new Vector2(innerSize, innerSize);
            _avatarImage = avatarGo.AddComponent<Image>();
            _avatarImage.color = UIFactory.AccentCyan;
            _avatarImage.sprite = TextureGenerator.GetCircle((int)innerSize);
            _avatarImage.raycastTarget = false;

            // Initials text (visible — shows first letter of player name)
            _initialsText = UIFactory.CreateText("Initials", avatarGo.transform, "",
                LayoutConfig.AvatarFontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(_initialsText.GetComponent<RectTransform>());

            // Border (optional — static color changes by state)
            if (includeBorder)
                _border = new AvatarBorderController(ringGo.transform, size);
        }

        /// <summary>
        /// Update the avatar to show a player's colored circle with their initial letter.
        /// </summary>
        public void UpdatePlayer(int playerId)
        {
            if (playerId == _currentPlayerId) return;
            _currentPlayerId = playerId;
            AvatarPatternGenerator.UpdateIfChanged(_avatarImage, _initialsText, playerId, TexResolution);
        }

        /// <summary>
        /// Show a dim placeholder circle for empty seats.
        /// </summary>
        public void SetEmpty(Color color)
        {
            _currentPlayerId = -1;
            _avatarImage.sprite = TextureGenerator.GetCircle(TexResolution);
            _avatarImage.color = color;
            if (_initialsText != null)
                _initialsText.alpha = 0f;
        }

        /// <summary>
        /// Dim the avatar for folded players.
        /// </summary>
        public void SetFolded(bool folded)
        {
            if (folded)
                _avatarImage.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        }
    }
}
