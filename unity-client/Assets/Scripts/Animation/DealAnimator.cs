using System.Collections.Generic;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Animates clockwise card dealing from a center deck to player seats.
    /// Two rounds (one card per player per round), with dramatic arc trajectory,
    /// slight rotation during flight, bouncy landing, and sparkle trails.
    /// Uses Timeline for flat sequencing (replaces nested Delay→OnComplete chains).
    /// </summary>
    public static class DealAnimator
    {
        public static void PlayDeal(
            AnimationController anim,
            Transform canvas,
            int dealerSeat,
            SeatView[] seats,
            List<PlayerState> players,
            Vector2 deckPos)
        {
            if (players == null || players.Count == 0) return;

            var activeSeats = new HashSet<int>();
            foreach (var p in players)
                if (p != null && p.HasCards && !p.IsFolded)
                    activeSeats.Add(p.Seat);
            if (activeSeats.Count == 0) return;

            var dealOrder = BuildDealOrder(dealerSeat, activeSeats);

            var deckGo = CreateDeckVisual(canvas, deckPos);

            // Suppress real cards and bets during deal
            foreach (int s in dealOrder)
            {
                seats[s].Card1.RectTransform.localScale = Vector3.zero;
                seats[s].Card2.RectTransform.localScale = Vector3.zero;
                if (seats[s].BetCanvasGroup != null)
                    seats[s].BetCanvasGroup.alpha = 0f;
            }

            // Fade in bets at the start of the deal
            foreach (int s in dealOrder)
            {
                var betCg = seats[s].BetCanvasGroup;
                if (betCg == null) continue;
                var capturedCg = betCg;

                new Timeline()
                    .AppendInterval(AnimationConfig.DealInitialDelay)
                    .Append(() =>
                    {
                        var h = Tweener.TweenFloat(0f, 1f, AnimationConfig.DealBetFade,
                            a => { if (capturedCg != null) capturedCg.alpha = a; });
                        h.SnapToFinal = () => { if (capturedCg != null) capturedCg.alpha = 1f; };
                        return h;
                    })
                    .Play(anim, () => { if (capturedCg != null) capturedCg.alpha = 1f; });
            }

            int totalFlights = dealOrder.Count * 2;
            int flightIdx = 0;
            float delay = AnimationConfig.DealInitialDelay;

            // Round 1: first card to each player
            for (int i = 0; i < dealOrder.Count; i++)
            {
                int seat = dealOrder[i];
                Vector2 target = GetCardCanvasPos(seats[seat], 0);
                FlyCard(anim, canvas, deckPos, target, delay, seats[seat].Card1,
                    flightIdx == totalFlights - 1, deckGo);
                delay += AnimationConfig.DealCardFly + AnimationConfig.DealCardGap;
                flightIdx++;
            }

            // Round 2: second card to each player
            for (int i = 0; i < dealOrder.Count; i++)
            {
                int seat = dealOrder[i];
                Vector2 target = GetCardCanvasPos(seats[seat], 1);
                FlyCard(anim, canvas, deckPos, target, delay, seats[seat].Card2,
                    flightIdx == totalFlights - 1, deckGo);
                delay += AnimationConfig.DealCardFly + AnimationConfig.DealCardGap;
                flightIdx++;
            }
        }

        private static List<int> BuildDealOrder(int dealerSeat, HashSet<int> activeSeats)
        {
            var order = new List<int>();
            for (int i = 1; i <= LayoutConfig.MaxSeats; i++)
            {
                int seat = ((dealerSeat - 1 + i) % LayoutConfig.MaxSeats) + 1;
                if (activeSeats.Contains(seat))
                    order.Add(seat);
            }
            return order;
        }

        private static Vector2 GetCardCanvasPos(SeatView seat, int cardIndex)
        {
            Vector2 seatPos = LayoutConfig.WorldToCanvasPos(seat.RectTransform);
            CardView card = cardIndex == 0 ? seat.Card1 : seat.Card2;
            return seatPos + card.RectTransform.anchoredPosition;
        }

        private static void FlyCard(AnimationController anim, Transform canvas,
            Vector2 from, Vector2 to, float delay, CardView targetCard,
            bool isLastCard, GameObject deckGo)
        {
            var flyGo = CreateFlyingCardBack(canvas, from);
            flyGo.SetActive(false);
            var flyRt = flyGo.GetComponent<RectTransform>();

            CardView capturedCard = targetCard;
            GameObject capturedDeck = deckGo;
            bool capturedLast = isLastCard;
            Transform capturedCanvas = canvas;

            float fanAngle = UI.LayoutConfig.SeatCardFanAngle;
            float cardFanAngle = capturedCard.gameObject.name.Contains("Card1") ? fanAngle : -fanAngle;

            System.Action snap = () =>
            {
                if (flyGo != null) Object.Destroy(flyGo);
                if (capturedCard != null)
                {
                    capturedCard.RectTransform.localScale = Vector3.one * UI.LayoutConfig.SeatCardScale;
                    capturedCard.RectTransform.localEulerAngles = new Vector3(0, 0, cardFanAngle);
                }
                if (capturedLast && capturedDeck != null)
                    Object.Destroy(capturedDeck);
            };

            new Timeline()
                .AppendInterval(delay)
                .AppendCallback(() =>
                {
                    if (flyGo == null) return;
                    flyGo.SetActive(true);
                    AudioManager.Instance?.Play(SoundType.CardDeal);
                    SparkleEffects.SpawnShimmerTrail(capturedCanvas, from, to, 8,
                        new Color(1f, 1f, 1f, 0.7f), AnimationConfig.DealCardFly + 0.1f, anim);
                })
                .Append(() =>
                {
                    var h = Tweener.TweenFloat(0f, 1f, AnimationConfig.DealCardFly, t =>
                    {
                        if (flyRt == null) return;
                        Vector2 linear = Vector2.Lerp(from, to, t);
                        Vector2 dir = (to - from).normalized;
                        Vector2 perp = new Vector2(-dir.y, dir.x);
                        float arc = Mathf.Sin(t * Mathf.PI) * AnimationConfig.DealArcHeight;
                        flyRt.anchoredPosition = linear + perp * arc;
                        flyRt.localEulerAngles = new Vector3(0, 0,
                            Mathf.Sin(t * Mathf.PI) * AnimationConfig.DealFlightRotation);
                    }, EaseType.EaseInOutQuad);
                    h.SnapToFinal = snap;
                    return h;
                })
                .AppendCallback(() =>
                {
                    // On arrival
                    if (flyGo != null) Object.Destroy(flyGo);
                    if (capturedCard != null)
                    {
                        capturedCard.RectTransform.localScale = Vector3.one * UI.LayoutConfig.SeatCardScale;
                        capturedCard.RectTransform.localEulerAngles = new Vector3(0, 0, cardFanAngle);
                    }
                    SparkleEffects.SpawnSparkles(capturedCanvas, to, 8,
                        new Color(1f, 1f, 1f, 0.7f), 30f, 0.4f, anim);
                    AudioManager.Instance?.PlayWithDelay(SoundType.Sparkle, 0f);
                    if (capturedLast)
                        FadeDeck(anim, capturedDeck);
                })
                .Play(anim, snap);
        }

        private static void FadeDeck(AnimationController anim, GameObject deckGo)
        {
            if (deckGo == null) return;
            var cg = deckGo.GetComponent<CanvasGroup>();
            if (cg == null) { Object.Destroy(deckGo); return; }

            var go = deckGo;
            var handle = anim.Play(Tweener.TweenFloat(1f, 0f, AnimationConfig.DealDeckFade,
                a => { if (cg != null) cg.alpha = a; }));
            handle.SnapToFinal = () => { if (go != null) Object.Destroy(go); };
            handle.OnComplete(() => { if (go != null) Object.Destroy(go); });
        }

        // ── Visual element factories ──────────────────────────────────

        private static GameObject CreateDeckVisual(Transform canvas, Vector2 position)
        {
            var go = new GameObject("DealDeck", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(56, 76);
            go.AddComponent<CanvasGroup>();

            for (int i = 0; i < 3; i++)
            {
                float offset = (2 - i) * 1.5f;
                var card = UIFactory.CreateImage($"DeckCard{i}", go.transform,
                    Color.white, CardView.CardSize);
                card.sprite = TextureGenerator.GetVerticalGradient(64, 96,
                    UIFactory.CardBackOverlay, UIFactory.CardBackLight, 8);
                card.type = UnityEngine.UI.Image.Type.Sliced;
                card.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(offset, -offset);

                var edge = UIFactory.CreateImage("Edge", card.transform,
                    new Color(1f, 0.84f, 0f, 0.2f));
                var edgeRt = edge.GetComponent<RectTransform>();
                edgeRt.anchorMin = Vector2.zero;
                edgeRt.anchorMax = Vector2.one;
                edgeRt.offsetMin = new Vector2(1, 1);
                edgeRt.offsetMax = new Vector2(-1, -1);
            }

            return go;
        }

        private static GameObject CreateFlyingCardBack(Transform canvas, Vector2 position)
        {
            var go = new GameObject("FlyCard", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = CardView.CardSize;
            rt.localScale = Vector3.one * UI.LayoutConfig.SeatCardScale;

            var bg = UIFactory.CreateImage("Back", go.transform, Color.white);
            bg.sprite = TextureGenerator.GetVerticalGradient(64, 96,
                UIFactory.CardBackOverlay, UIFactory.CardBackLight, 8);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            UIFactory.StretchFill(bg.GetComponent<RectTransform>());

            var border = UIFactory.CreateImage("Border", bg.transform,
                UIFactory.CardBackBorder);
            border.sprite = TextureGenerator.GetRoundedRect(64, 96, 8);
            border.type = UnityEngine.UI.Image.Type.Sliced;
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(2, 2);
            bRt.offsetMax = new Vector2(-2, -2);

            return go;
        }
    }
}
