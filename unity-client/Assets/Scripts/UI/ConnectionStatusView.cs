using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    /// <summary>
    /// Frosted white pill with colored dot + status text showing connection state.
    /// Green = WebSocket, Blue = REST, Yellow = Connecting, Red = Disconnected/Error.
    /// </summary>
    public class ConnectionStatusView : MonoBehaviour
    {
        private Image _dot;
        private TextMeshProUGUI _label;

        private static readonly Color Green = UIFactory.HexColor("#00c853");
        private static readonly Color Blue = UIFactory.HexColor("#2196f3");
        private static readonly Color Yellow = UIFactory.HexColor("#ffeb3b");
        private static readonly Color Red = UIFactory.HexColor("#f44336");

        public static ConnectionStatusView Create(Transform parent)
        {
            var go = new GameObject("ConnectionStatus", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(8, -8);
            rt.sizeDelta = new Vector2(220, 26);

            // Frosted white pill with soft shadow
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.75f);
            bg.sprite = TextureGenerator.GetRoundedRect(220, 26, 13);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            var view = go.AddComponent<ConnectionStatusView>();
            view.BuildUI();
            return view;
        }

        private void BuildUI()
        {
            var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(8, 4, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            _dot = UIFactory.CreateImage("Dot", transform, Yellow, new Vector2(10, 10));
            var dotLE = _dot.gameObject.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 10;
            dotLE.preferredHeight = 10;

            _label = UIFactory.CreateText("Label", transform, "Connecting...",
                11f, UIFactory.TextMuted, TextAlignmentOptions.MidlineLeft);
            var labelLE = _label.gameObject.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;
        }

        public void UpdateStatus(ConnectionState state, string message)
        {
            Color dotColor = state switch
            {
                ConnectionState.ConnectedWebSocket => Green,
                ConnectionState.ConnectedRest => Blue,
                ConnectionState.Connecting => Yellow,
                _ => Red
            };

            _dot.color = dotColor;
            _label.text = message;
        }
    }
}
