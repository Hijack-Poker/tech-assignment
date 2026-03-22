using UnityEngine;
using HijackPoker.Animation;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Handles card flip, fold, and deal animation coordination for a single seat.
    /// </summary>
    public class SeatCardAnimator
    {
        private readonly CardView _card1;
        private readonly CardView _card2;
        private TweenHandle _flipHandle1;
        private TweenHandle _flipHandle2;
        private TweenHandle _foldTiltHandle;
        private TweenHandle _foldFadeHandle;
        private bool _prevCardsWereFaceDown;
        private bool _wasFolded;

        public SeatCardAnimator(CardView card1, CardView card2)
        {
            _card1 = card1;
            _card2 = card2;
        }

        public void UpdateCardsAnimated(PlayerState player, GameState game, bool animate,
            AnimationController animController)
        {
            if (!player.HasCards || game.HandStep < 4 || player.IsFolded)
            {
                // If fold animation is still in flight, let it finish
                if (_foldTiltHandle != null && !_foldTiltHandle.IsComplete)
                    goto TrackState;

                if (player.IsFolded && !_wasFolded && animate
                    && _card1.CurrentState != CardView.State.Empty)
                {
                    AnimateFoldCards(animController);
                }
                else
                {
                    _card1.SetEmpty();
                    _card2.SetEmpty();
                }

                TrackState:
                _wasFolded = player.IsFolded;
                _prevCardsWereFaceDown = false;
                return;
            }

            string card1Str = player.Cards.Count > 0 ? player.Cards[0] : null;
            string card2Str = player.Cards.Count > 1 ? player.Cards[1] : null;

            bool shouldShowFaceUp = ShowdownLogic.ShouldShowCards(
                game.HandStep, player.Status, player.Winnings);

            if (shouldShowFaceUp && _prevCardsWereFaceDown && animate)
            {
                if (!string.IsNullOrEmpty(card1Str))
                    _flipHandle1 = _card1.AnimateFlip(card1Str, animController);
                if (!string.IsNullOrEmpty(card2Str))
                    _flipHandle2 = _card2.AnimateFlip(card2Str, animController);
            }
            else
            {
                bool card1Flipping = _flipHandle1 != null && !_flipHandle1.IsComplete;
                bool card2Flipping = _flipHandle2 != null && !_flipHandle2.IsComplete;

                if (!card1Flipping)
                    _card1.SetFromPlayerData(card1Str, game.HandStep, player.Status, player.Winnings);
                if (!card2Flipping)
                    _card2.SetFromPlayerData(card2Str, game.HandStep, player.Status, player.Winnings);
            }

            _prevCardsWereFaceDown = !shouldShowFaceUp
                && _card1.CurrentState != CardView.State.Empty;
            _wasFolded = player.IsFolded;
        }

        private void AnimateFoldCards(AnimationController animController)
        {
            AudioManager.Instance?.Play(SoundType.FoldSwoosh);
            float duration = 0.3f;
            System.Action snapBothToFinal = () =>
            {
                _card1.SetEmpty();
                _card2.SetEmpty();
                _card1.ResetFoldVisuals();
                _card2.ResetFoldVisuals();
            };

            _foldTiltHandle = animController.Play(Tweener.TweenFloat(0f, 15f, duration,
                v =>
                {
                    _card1.RectTransform.localEulerAngles = new Vector3(0, 0, v);
                    _card2.RectTransform.localEulerAngles = new Vector3(0, 0, -v);
                }));
            _foldTiltHandle.SnapToFinal = snapBothToFinal;

            _foldFadeHandle = animController.Play(Tweener.TweenFloat(1f, 0f, duration,
                v =>
                {
                    _card1.CanvasGroup.alpha = v;
                    _card2.CanvasGroup.alpha = v;
                }));
            _foldFadeHandle.SnapToFinal = snapBothToFinal;
            _foldFadeHandle.OnComplete(snapBothToFinal);
        }

        public void Reset()
        {
            _prevCardsWereFaceDown = false;
            _wasFolded = false;
            _flipHandle1 = null;
            _flipHandle2 = null;
            _foldTiltHandle = null;
            _foldFadeHandle = null;
        }
    }
}
