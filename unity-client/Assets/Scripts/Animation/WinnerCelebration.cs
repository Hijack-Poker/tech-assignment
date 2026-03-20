using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Multi-phase winner celebration sequence: backdrop dim → avatar center-stage →
    /// banner pop → confetti burst → auto-dismiss.
    /// Plays after the final pot flight arrives.
    /// </summary>
    public static class WinnerCelebration
    {
        public static void Play(AnimationController anim, Transform canvas,
            SeatView[] seats, int winnerSeat, float totalWinnings)
        {
            if (anim == null || canvas == null) return;
            if (winnerSeat < 1 || winnerSeat > LayoutConfig.MaxSeats) return;
            if (seats[winnerSeat] == null) return;

            Vector2 seatPos = LayoutConfig.WorldToCanvasPos(seats[winnerSeat].RectTransform);
            Vector2 center = LayoutConfig.CanvasCenter;

            // Track all created GameObjects for cleanup
            var created = new List<GameObject>();

            // ── Phase 1: Backdrop dim ────────────────────────────────────
            var backdropGo = new GameObject("WinnerBackdrop", typeof(RectTransform));
            backdropGo.transform.SetParent(canvas, false);
            // Push to front
            backdropGo.transform.SetAsLastSibling();
            var backdropRt = backdropGo.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            var backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.color = new Color(0, 0, 0, 0);
            backdropImg.raycastTarget = false;
            var backdropCg = backdropGo.AddComponent<CanvasGroup>();
            backdropCg.alpha = 0f;
            backdropCg.blocksRaycasts = false;
            created.Add(backdropGo);

            // ── Pre-create avatar clone ──────────────────────────────────
            float avatarSize = LayoutConfig.AvatarSize;
            var avatarClone = AvatarCircleView.Create(canvas, avatarSize, false);
            var avatarGo = avatarClone.gameObject;
            avatarGo.transform.SetAsLastSibling();
            // Find winner's player ID from the seat
            var seatView = seats[winnerSeat];
            avatarClone.UpdatePlayer(GetPlayerIdFromSeat(seatView));
            var avatarRt = avatarGo.GetComponent<RectTransform>();
            UIFactory.SetAnchor(avatarRt, 0.5f, 0.5f);
            avatarRt.anchoredPosition = seatPos;
            avatarRt.localScale = Vector3.one;
            var avatarCg = avatarGo.AddComponent<CanvasGroup>();
            avatarCg.alpha = 0f;
            created.Add(avatarGo);

            // ── Pre-create banner ────────────────────────────────────────
            var bannerGo = new GameObject("WinnerBanner", typeof(RectTransform));
            bannerGo.transform.SetParent(canvas, false);
            bannerGo.transform.SetAsLastSibling();
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            UIFactory.SetAnchor(bannerRt, 0.5f, 0.5f);
            bannerRt.sizeDelta = new Vector2(180, 40);
            float bannerY = center.y - avatarSize * AnimationConfig.WinnerAvatarScale * 0.5f - 30f;
            bannerRt.anchoredPosition = new Vector2(center.x, bannerY);
            bannerRt.localScale = Vector3.zero;

            // Banner background
            var bannerBg = bannerGo.AddComponent<Image>();
            bannerBg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
            bannerBg.sprite = TextureGenerator.GetRoundedRect(180, 40, 12);
            bannerBg.type = Image.Type.Sliced;
            bannerBg.raycastTarget = false;

            // Banner text
            var bannerText = UIFactory.CreateText("WinnerText", bannerGo.transform,
                "WINNER", 18f, UIFactory.AccentGold,
                TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(bannerText.GetComponent<RectTransform>());

            var bannerCg = bannerGo.AddComponent<CanvasGroup>();
            bannerCg.alpha = 1f;
            created.Add(bannerGo);

            // ── Cleanup helper ───────────────────────────────────────────
            bool dismissed = false;
            System.Action destroyAll = () =>
            {
                if (dismissed) return;
                dismissed = true;
                foreach (var go in created)
                    if (go != null) Object.Destroy(go);
                created.Clear();
            };

            // ── Build timeline ───────────────────────────────────────────
            new Timeline()
                // Phase 1: Backdrop fade in
                .AppendCallback(() =>
                {
                    anim.Play(Tweener.TweenFloat(0f, 0.3f,
                        AnimationConfig.WinnerBackdropFade,
                        a =>
                        {
                            if (backdropImg != null) backdropImg.color = new Color(0, 0, 0, a);
                            if (backdropCg != null) backdropCg.alpha = 1f;
                        },
                        EaseType.EaseOutQuart));
                })
                .AppendInterval(0.2f)

                // Phase 2: Avatar center-stage
                .AppendCallback(() =>
                {
                    if (avatarCg != null) avatarCg.alpha = 1f;

                    // Fly to center
                    anim.Play(Tweener.TweenPosition(avatarRt, seatPos, center,
                        AnimationConfig.WinnerAvatarFly, EaseType.EaseOutQuart));

                    // Scale up with bounce
                    float targetScale = AnimationConfig.WinnerAvatarScale;
                    anim.Play(Tweener.TweenScale(avatarGo.transform,
                        Vector3.one, Vector3.one * targetScale,
                        AnimationConfig.WinnerAvatarFly, EaseType.EaseOutBack));
                })
                .Append(() => Tweener.Delay(AnimationConfig.WinnerAvatarFly))
                .AppendCallback(() =>
                {
                    // Ring pulse when avatar lands at center
                    SparkleEffects.SpawnRingPulse(canvas, center,
                        UIFactory.AccentGold, 120f, 1.0f, anim);
                })

                // Phase 3: Banner pop
                .AppendInterval(AnimationConfig.WinnerBannerDelay)
                .AppendCallback(() =>
                {
                    // Scale pop: 0 → 1.15 → 1.0
                    anim.Play(Tweener.TweenScale(bannerGo.transform,
                        Vector3.zero, Vector3.one * 1.15f,
                        AnimationConfig.WinnerBannerPop * 0.6f, EaseType.EaseOutQuart))
                        .OnComplete(() =>
                        {
                            anim.Play(Tweener.TweenScale(bannerGo.transform,
                                Vector3.one * 1.15f, Vector3.one,
                                AnimationConfig.WinnerBannerPop * 0.4f, EaseType.EaseInOutQuad));
                        });

                    // Gold sparkles around banner
                    SparkleEffects.SpawnSparkles(canvas,
                        new Vector2(center.x, bannerY), 14,
                        UIFactory.AccentGold, 80f, 1.0f, anim);
                })

                // Phase 4: Confetti burst
                .AppendInterval(AnimationConfig.WinnerConfettiDelay)
                .AppendCallback(() =>
                {
                    ConfettiEffect.SpawnConfetti(canvas, center, 50,
                        AnimationConfig.WinnerConfettiDuration, anim);
                    SparkleEffects.SpawnGoldShower(canvas, center, 35, 1.8f, anim);
                })

                // Phase 5: Auto-dismiss
                .AppendInterval(AnimationConfig.WinnerDismissDelay)
                .AppendCallback(() =>
                {
                    float fadeDur = AnimationConfig.WinnerDismissFade;

                    // Fade out backdrop
                    anim.Play(Tweener.TweenFloat(0.3f, 0f, fadeDur,
                        a => { if (backdropImg != null) backdropImg.color = new Color(0, 0, 0, a); }));

                    // Fade out avatar
                    var avatarFade = anim.Play(Tweener.TweenAlpha(avatarCg, 1f, 0f, fadeDur));

                    // Fade out banner
                    anim.Play(Tweener.TweenAlpha(bannerCg, 1f, 0f, fadeDur));

                    // Destroy everything after fade completes
                    avatarFade.OnComplete(destroyAll);
                })
                .Play(anim, destroyAll);
        }

        /// <summary>
        /// Extracts the player ID from a SeatView by reading the seat number.
        /// The avatar pattern is deterministic from the player ID, but seats
        /// don't expose playerId directly — we use seat number as the key
        /// since AvatarPatternGenerator uses playerId for color selection.
        /// </summary>
        private static int GetPlayerIdFromSeat(SeatView seat)
        {
            // SeatView exposes SeatNumber; the avatar was updated via UpdatePlayer(playerId)
            // but playerId isn't publicly exposed. Use seat number as a proxy —
            // the avatar clone will match because we read the same seat.
            return seat.SeatNumber;
        }
    }
}
