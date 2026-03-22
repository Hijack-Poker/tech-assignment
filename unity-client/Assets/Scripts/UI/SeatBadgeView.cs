using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages position badge, action badge, and all-in badge for a single seat.
    /// </summary>
    public class SeatBadgeView
    {
        private readonly RectTransform _positionBadge;
        private readonly TextMeshProUGUI _positionBadgeText;
        private readonly RectTransform _actionBadge;
        private readonly TextMeshProUGUI _actionBadgeText;
        private readonly Image _actionBadgeBg;
        private readonly CanvasGroup _actionBadgeCg;
        private readonly RectTransform _allInBadge;
        private string _prevAction;

        public SeatBadgeView(
            RectTransform positionBadge, TextMeshProUGUI positionBadgeText,
            RectTransform actionBadge, TextMeshProUGUI actionBadgeText,
            Image actionBadgeBg, CanvasGroup actionBadgeCg,
            RectTransform allInBadge)
        {
            _positionBadge = positionBadge;
            _positionBadgeText = positionBadgeText;
            _actionBadge = actionBadge;
            _actionBadgeText = actionBadgeText;
            _actionBadgeBg = actionBadgeBg;
            _actionBadgeCg = actionBadgeCg;
            _allInBadge = allInBadge;
        }

        public void UpdatePositionBadge(int seat, GameState game)
        {
            string label = GetPositionLabel(seat, game);

            if (label != null)
            {
                _positionBadge.gameObject.SetActive(true);
                _positionBadgeText.text = label;

                if (label == "BTN")
                    _positionBadgeText.color = UIFactory.AccentGold;
                else if (label == "SB" || label == "BB")
                    _positionBadgeText.color = UIFactory.AccentCyan;
                else
                    _positionBadgeText.color = UIFactory.TextSecondary;
            }
            else
            {
                _positionBadge.gameObject.SetActive(false);
            }
        }

        public void UpdateActionBadgeAnimated(PlayerState player, bool animate,
            AnimationController animController)
        {
            if (string.IsNullOrEmpty(player.Action))
            {
                _actionBadgeCg.alpha = 0f;
                _prevAction = null;
                return;
            }

            string displayAction = player.Action.ToUpper();
            _actionBadgeText.text = displayAction;

            Color badgeColor;
            Color textColor;
            switch (player.Action.ToLower())
            {
                case "check": badgeColor = UIFactory.ActionCheck; textColor = UIFactory.CardBlack; break;
                case "call": badgeColor = UIFactory.ActionCall; textColor = UIFactory.CardBlack; break;
                case "bet":
                case "raise": badgeColor = UIFactory.ActionBet; textColor = UIFactory.CardBlack; break;
                case "fold": badgeColor = UIFactory.ActionFold; textColor = Color.white; break;
                case "allin": badgeColor = UIFactory.ActionAllIn; textColor = Color.white; break;
                default: badgeColor = UIFactory.TextMuted; textColor = Color.white; break;
            }
            _actionBadgeBg.color = badgeColor;
            _actionBadgeText.color = textColor;

            _actionBadgeCg.alpha = 1f;

            bool isNewAction = player.Action != _prevAction;
            if (animate && isNewAction)
            {
                animController.Play(Tweener.ScalePop(_actionBadge, 0.2f, 1.3f));
            }

            _prevAction = player.Action;
        }

        public void UpdateAllInBadge(bool isAllIn)
        {
            _allInBadge.gameObject.SetActive(isAllIn);
        }

        public void Reset()
        {
            _prevAction = null;
        }

        private static string GetPositionLabel(int seat, GameState game)
        {
            if (seat == game.DealerSeat) return "BTN";
            if (seat == game.SmallBlindSeat) return "SB";
            if (seat == game.BigBlindSeat) return "BB";

            var activeSeats = new System.Collections.Generic.List<int>();
            int bb = game.BigBlindSeat;
            for (int i = 1; i <= LayoutConfig.MaxSeats; i++)
            {
                int s = ((bb - 1 + i) % LayoutConfig.MaxSeats) + 1;
                if (s == game.DealerSeat || s == game.SmallBlindSeat || s == game.BigBlindSeat)
                    continue;
                activeSeats.Add(s);
            }

            int idx = activeSeats.IndexOf(seat);
            if (idx < 0) return null;

            int count = activeSeats.Count;
            if (count == 1) return "UTG";
            if (count == 2) return idx == 0 ? "UTG" : "CO";
            if (idx == count - 1) return "CO";
            if (idx == count - 2) return "HJ";
            return "UTG";
        }
    }
}
