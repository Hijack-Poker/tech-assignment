using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Compact heads-up display: phase label in frosted pill (top-center),
    /// merged hand number + blinds + active player info line.
    /// Pot is created separately via CreatePot() into the center game row.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        private TextMeshProUGUI _infoLine;
        private TextMeshProUGUI _potText;
        private RectTransform _rt;
        private RectTransform _potRt;
        private CanvasGroup _potCg;
        private KineticPhaseLabel _kineticLabel;
        private LiquidPotCounter _liquidPot;

        // Animation state
        private int _prevStep = -1;
        private float _prevPot;
        private bool _hasRenderedOnce;

        public AnimationController AnimController { get; set; }
        public RectTransform PotTransform => _potRt;

        public static HudView Create(Transform parent)
        {
            var go = new GameObject("HUD", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            var view = go.AddComponent<HudView>();
            view._rt = rt;
            view.BuildUI();
            return view;
        }

        private void BuildUI()
        {
            var phaseSize = LayoutConfig.PhaseLabelContainerSize;

            // HUD uses a vertical layout to stack elements
            var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(0, 0, 1, 0);

            // Row 1: Phase label with kinetic animations
            var phaseRow = new GameObject("PhaseRow", typeof(RectTransform));
            phaseRow.transform.SetParent(transform, false);
            var phaseRowLE = phaseRow.AddComponent<LayoutElement>();
            phaseRowLE.preferredHeight = phaseSize.y + 2;
            phaseRowLE.preferredWidth = 0;
            phaseRowLE.flexibleWidth = 1;

            _kineticLabel = KineticPhaseLabel.Create(phaseRow.transform, phaseSize);

            // Row 2: Merged info line — "Hand #N · $1/$2 · Player to act"
            _infoLine = UIFactory.CreateText("InfoLine", transform, "",
                14f, new Color(0.78f, 0.82f, 0.88f, 1f), TextAlignmentOptions.Center);
            var infoLE = _infoLine.gameObject.AddComponent<LayoutElement>();
            infoLE.preferredWidth = LayoutConfig.StatusWidth;
            infoLE.preferredHeight = 18;
        }

        /// <summary>
        /// Creates the pot display as a child of the center game row,
        /// separate from the HUD's own layout. Frosted glass pill with amber text.
        /// </summary>
        public void CreatePot(Transform centerRow)
        {
            var potSize = LayoutConfig.PotBgSize;
            var potBg = UIFactory.CreatePanel("PotBg", centerRow, null, potSize);

            // Soft shadow beneath the pill
            var shadowImg = UIFactory.CreateImage("PotShadow", potBg);
            shadowImg.sprite = TextureGenerator.GetSoftShadow((int)potSize.x, (int)potSize.y, 13, 4, 0.12f);
            shadowImg.type = Image.Type.Sliced;
            var shadowRt = shadowImg.GetComponent<RectTransform>();
            UIFactory.StretchFill(shadowRt);
            shadowRt.offsetMin = new Vector2(-4, -6);
            shadowRt.offsetMax = new Vector2(4, -2);

            // Dark glass pill background
            var potBgImg = potBg.gameObject.AddComponent<Image>();
            potBgImg.color = new Color(0f, 0.08f, 0.02f, 0.35f);
            potBgImg.sprite = TextureGenerator.GetRoundedRect((int)potSize.x, (int)potSize.y, 13);
            potBgImg.type = Image.Type.Sliced;

            // Gold top-edge highlight (1px accent line)
            var topEdge = UIFactory.CreateImage("PotTopEdge", potBg,
                new Color(UIFactory.AccentGold.r, UIFactory.AccentGold.g, UIFactory.AccentGold.b, 0.18f));
            var topEdgeRt = topEdge.GetComponent<RectTransform>();
            topEdgeRt.anchorMin = new Vector2(0.1f, 1f);
            topEdgeRt.anchorMax = new Vector2(0.9f, 1f);
            topEdgeRt.pivot = new Vector2(0.5f, 1f);
            topEdgeRt.sizeDelta = new Vector2(0, 1);

            var potLE = potBg.gameObject.AddComponent<LayoutElement>();
            potLE.preferredWidth = potSize.x;
            potLE.preferredHeight = potSize.y;

            _potRt = potBg;
            _potCg = potBg.gameObject.AddComponent<CanvasGroup>();
            _potCg.alpha = 0f;

            _potText = UIFactory.CreateText("Pot", potBg, "",
                LayoutConfig.PotFontSize, UIFactory.AccentGold, TextAlignmentOptions.Center,
                FontStyles.Bold);
            UIFactory.StretchFill(_potText.GetComponent<RectTransform>());

            _liquidPot = LiquidPotCounter.Create(potBg, _potText, LayoutConfig.PotFontSize);
            _liquidPot.SetAnimController(AnimController);
        }

        public void UpdateFromState(TableResponse state)
        {
            if (state?.Game == null)
            {
                _kineticLabel.Label.text = "Waiting for data...";
                _infoLine.text = "";
                if (_potText != null) _potText.text = "";
                return;
            }

            var game = state.Game;
            bool animate = _hasRenderedOnce && AnimController != null;

            // Phase label with kinetic entrance
            string phaseText = PhaseLabels.GetLabel(game.HandStep);
            if (animate && game.HandStep != _prevStep)
            {
                var category = KineticPhaseLabel.GetCategory(game.HandStep);
                _kineticLabel.Announce(phaseText, category, AnimController);
            }
            else if (!_hasRenderedOnce)
            {
                _kineticLabel.Label.text = phaseText;
            }
            _prevStep = game.HandStep;

            // Merged info line: "Hand #N · $1/$2 · Player to act"
            string blindsStr = $"{MoneyFormatter.Format(game.SmallBlind)}/{MoneyFormatter.Format(game.BigBlind)}";
            string activeInfo = "";
            if (game.Move > 0 && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (p.Seat == game.Move)
                    {
                        activeInfo = $"  \u00B7  {p.Username} to act";
                        break;
                    }
                }
            }
            _infoLine.text = $"Hand #{game.GameNo}  \u00B7  {blindsStr}{activeInfo}";
            _infoLine.color = new Color(0.78f, 0.82f, 0.88f, 1f);

            // Pot (with liquid counter)
            if (_potText != null)
            {
                float newPot = game.Pot;
                string sidePotStr = BuildSidePotString(game);

                if (_potCg != null)
                    _potCg.alpha = newPot > 0 ? 1f : 0f;

                if (_liquidPot != null)
                {
                    _liquidPot.SetPot(newPot, sidePotStr, animate);
                }
                else
                {
                    _potText.text = newPot > 0
                        ? $"{MoneyFormatter.Format(newPot)}{sidePotStr}"
                        : "";
                }
                _prevPot = newPot;
            }

            _hasRenderedOnce = true;
        }

        private string BuildSidePotString(GameState game)
        {
            if (game.SidePots == null || game.SidePots.Count == 0)
                return "";

            string result = "";
            for (int i = 0; i < game.SidePots.Count; i++)
            {
                result += $" + SP{i + 1}: {MoneyFormatter.Format(game.SidePots[i].Amount)}";
            }
            return result;
        }

        public void SetStatus(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                _infoLine.color = new Color(0.78f, 0.82f, 0.88f, 1f);
            }
            else
            {
                _infoLine.text = message;
                _infoLine.color = UIFactory.AccentMagenta;
            }
        }
    }
}
