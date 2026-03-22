using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages table-level animations: chip fly, shuffle, card deal, and arc tweens.
    /// </summary>
    public class TableAnimationController : MonoBehaviour
    {
        private RectTransform _animLayer;
        private RectTransform _dealerSource;
        private RectTransform _potTarget;
        private Sprite _chipFlySprite;
        private Sprite _cardBackSprite;
        private SeatView[] _seatViews;
        private TableAudioController _audio;
        private Func<int, int> _seatToViewIndex;

        public void Initialize(RectTransform animLayer, RectTransform dealerSource, RectTransform potTarget,
                               Sprite chipFlySprite, Sprite cardBackSprite, SeatView[] seatViews,
                               TableAudioController audio, Func<int, int> seatToViewIndex)
        {
            _animLayer = animLayer;
            _dealerSource = dealerSource;
            _potTarget = potTarget;
            _chipFlySprite = chipFlySprite;
            _cardBackSprite = cardBackSprite;
            _seatViews = seatViews;
            _audio = audio;
            _seatToViewIndex = seatToViewIndex;
        }

        public void AnimateChipFly(SeatView seat)
        {
            if (_animLayer == null || _potTarget == null || _chipFlySprite == null) return;
            _audio?.PlayChipBetSound();

            var chipGO = new GameObject("FlyChip", typeof(RectTransform));
            chipGO.transform.SetParent(_animLayer, false);
            var img = chipGO.AddComponent<Image>();
            img.sprite = _chipFlySprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rt = chipGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(32, 32);
            chipGO.transform.position = seat.transform.position;

            var seq = DOTween.Sequence();
            seq.Append(chipGO.transform.DOMove(_potTarget.position, 0.45f).SetEase(Ease.InOutCubic));
            seq.Join(DOTween.To(() => rt.sizeDelta, v => rt.sizeDelta = v, new Vector2(20, 20), 0.45f).SetEase(Ease.InQuad));
            seq.Join(DOTween.ToAlpha(() => img.color, c => img.color = c, 0f, 0.1f).SetDelay(0.35f));
            seq.OnComplete(() => Destroy(chipGO));
        }

        public IEnumerator AnimateShuffleAtDealer()
        {
            if (_animLayer == null || _cardBackSprite == null) yield break;

            Vector3 dealerPos = _dealerSource != null
                ? _dealerSource.position
                : transform.position + new Vector3(0f, 180f, 0f);

            _audio?.PlayDealShuffleSound();

            const int cardCount = 8;
            const float spreadX = 24f;
            const float splitDuration = 0.28f;
            const float mergeDuration = 0.22f;
            var cards = new List<GameObject>();

            for (int i = 0; i < cardCount; i++)
            {
                var go = new GameObject($"ShuffleCard_{i}", typeof(RectTransform));
                go.transform.SetParent(_animLayer, false);
                var img = go.AddComponent<Image>();
                img.sprite = _cardBackSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f);

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(42, 58);
                go.transform.position = dealerPos;
                go.transform.localScale = Vector3.zero;
                rt.localRotation = Quaternion.Euler(0f, 0f, (i - cardCount / 2f) * 5f);

                cards.Add(go);
            }

            // Phase 1: Grow in
            {
                var growSeq = DOTween.Sequence();
                for (int i = 0; i < cardCount; i++)
                {
                    var go = cards[i];
                    var img = go.GetComponent<Image>();
                    float delay = i * 0.04f;
                    growSeq.Insert(delay, go.transform.DOScale(1.15f, 0.3f).SetEase(Ease.OutBack));
                    growSeq.Insert(delay, DOTween.ToAlpha(() => img.color, c => img.color = c, 1f, 0.15f));
                }
                growSeq.Append(DOTween.Sequence());
                for (int i = 0; i < cardCount; i++)
                    growSeq.Join(cards[i].transform.DOScale(1f, 0.15f).SetEase(Ease.InOutQuad));

                yield return growSeq.WaitForCompletion();
            }

            yield return new WaitForSeconds(0.25f);

            // Phase 2: Riffle shuffle — 3 rounds
            int half = cardCount / 2;
            for (int round = 0; round < 3; round++)
            {
                var seq = DOTween.Sequence();
                for (int i = 0; i < cardCount; i++)
                {
                    var rt = cards[i].GetComponent<RectTransform>();
                    float dir = i < half ? -1f : 1f;
                    Vector3 splitTarget = dealerPos + new Vector3(dir * spreadX, 0f, 0f);
                    seq.Join(cards[i].transform.DOMove(splitTarget, splitDuration).SetEase(Ease.OutQuad));
                    seq.Join(rt.DOLocalRotate(new Vector3(0f, 0f, dir * 10f), splitDuration).SetEase(Ease.OutQuad));
                }
                seq.AppendInterval(0.05f);
                for (int i = 0; i < cardCount; i++)
                {
                    var rt = cards[i].GetComponent<RectTransform>();
                    seq.Join(cards[i].transform.DOMove(dealerPos, mergeDuration).SetEase(Ease.InQuad));
                    seq.Join(rt.DOLocalRotate(new Vector3(0f, 0f, (i - cardCount / 2f) * 5f), mergeDuration).SetEase(Ease.InQuad));
                }
                for (int i = 0; i < cardCount; i++)
                    seq.Join(cards[i].transform.DOPunchScale(Vector3.one * 0.08f, 0.15f, 6, 0.5f));

                yield return seq.WaitForCompletion();

                if (round < 2)
                    yield return new WaitForSeconds(0.08f);
            }

            yield return new WaitForSeconds(0.3f);

            // Phase 3: Fade out and cleanup
            var fadeSeq = DOTween.Sequence();
            foreach (var go in cards)
            {
                var img = go.GetComponent<Image>();
                fadeSeq.Join(DOTween.ToAlpha(() => img.color, c => img.color = c, 0f, 0.25f));
                fadeSeq.Join(go.transform.DOScale(0.7f, 0.25f).SetEase(Ease.InQuad));
            }
            yield return fadeSeq.WaitForCompletion();

            foreach (var go in cards)
                Destroy(go);
        }

        public IEnumerator AnimateCardDeal(List<PlayerState> activePlayers, int dealerSeat)
        {
            if (_animLayer == null || _cardBackSprite == null || activePlayers == null || activePlayers.Count == 0)
                yield break;

            activePlayers = BuildDealOrderClockwise(activePlayers, dealerSeat);
            _audio?.PlayDealShuffleSound();

            foreach (var player in activePlayers)
            {
                int idx = _seatToViewIndex(player.Seat);
                if (idx < 0 || idx >= _seatViews.Length) continue;
                _seatViews[idx].SetCardsVisible(false);
            }

            float stagger = 0.09f;
            float travelDuration = 0.3f;
            var flyingCards = new List<GameObject>();

            for (int round = 0; round < 2; round++)
            {
                foreach (var player in activePlayers)
                {
                    int idx = _seatToViewIndex(player.Seat);
                    if (idx < 0 || idx >= _seatViews.Length) continue;
                    var seat = _seatViews[idx];

                    var cardGO = new GameObject($"DealCard_{player.Seat}_{round}", typeof(RectTransform));
                    cardGO.transform.SetParent(_animLayer, false);
                    var img = cardGO.AddComponent<Image>();
                    img.sprite = _cardBackSprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;

                    var rt = cardGO.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(44, 62);
                    cardGO.transform.SetAsLastSibling();

                    Vector3 startPos = _dealerSource != null
                        ? _dealerSource.position
                        : transform.position + new Vector3(0f, 180f, 0f);
                    cardGO.transform.position = startPos;
                    rt.localScale = Vector3.one * 0.3f;

                    Vector3 targetPos = seat.CardAreaWorldPosition + new Vector3(round == 0 ? -12f : 12f, round == 0 ? 2f : -2f, 0f);

                    float targetAngle = round == 0 ? -10f : 10f;
                    if (targetPos.x > startPos.x) targetAngle += 4f;

                    var seq = DOTween.Sequence();
                    seq.Append(CreateArcMoveTween(rt, startPos, targetPos, travelDuration));
                    seq.Join(rt.DOScale(Vector3.one, travelDuration).SetEase(Ease.OutCubic));
                    seq.Join(rt.DORotate(new Vector3(0f, 0f, targetAngle), travelDuration).SetEase(Ease.OutQuad));

                    flyingCards.Add(cardGO);

                    yield return new WaitForSeconds(stagger);
                }
            }

            yield return new WaitForSeconds(0.35f);

            foreach (var go in flyingCards)
            {
                if (go != null) Destroy(go);
            }

            foreach (var player in activePlayers)
            {
                int idx = _seatToViewIndex(player.Seat);
                if (idx < 0 || idx >= _seatViews.Length) continue;
                _seatViews[idx].SetCardsVisible(true);
            }
        }

        private List<PlayerState> BuildDealOrderClockwise(List<PlayerState> players, int dealerSeat)
        {
            if (players == null || players.Count == 0)
                return new List<PlayerState>();

            var validPlayers = players.Where(p => p.Seat > 0).ToList();

            if (validPlayers.Count <= 1 || dealerSeat <= 0)
                return validPlayers;

            var clockwiseSeats = GetClockwiseSeatOrder(validPlayers.Select(p => p.Seat));
            if (clockwiseSeats.Count == 0)
                return validPlayers;

            int startIdx = clockwiseSeats.IndexOf(dealerSeat);
            if (startIdx < 0) startIdx = 0;
            startIdx = (startIdx + 1) % clockwiseSeats.Count;

            var bySeat = validPlayers.ToDictionary(p => p.Seat, p => p);
            var ordered = new List<PlayerState>(clockwiseSeats.Count);
            for (int i = 0; i < clockwiseSeats.Count; i++)
            {
                int seat = clockwiseSeats[(startIdx + i) % clockwiseSeats.Count];
                if (bySeat.TryGetValue(seat, out var player))
                    ordered.Add(player);
            }

            return ordered;
        }

        public List<int> GetClockwiseSeatOrder(IEnumerable<int> seats)
        {
            if (seats == null) return new List<int>();

            Vector3 center = _potTarget != null ? _potTarget.position : transform.position;

            return seats
                .Distinct()
                .Where(seat =>
                {
                    int idx = _seatToViewIndex(seat);
                    return seat > 0 && idx >= 0 && idx < _seatViews.Length && _seatViews[idx] != null;
                })
                .Select(seat =>
                {
                    int idx = _seatToViewIndex(seat);
                    Vector3 p = _seatViews[idx].transform.position - center;
                    float angle = Mathf.Atan2(p.y, p.x);
                    return new { seat, angle };
                })
                .OrderByDescending(x => x.angle)
                .Select(x => x.seat)
                .ToList();
        }

        public static Tween CreateArcMoveTween(RectTransform card, Vector3 start, Vector3 end, float duration)
        {
            float height = Mathf.Clamp(Vector3.Distance(start, end) * 0.12f, 30f, 96f);
            Vector3 control = (start + end) * 0.5f + new Vector3(0f, height, 0f);

            return DOVirtual.Float(0f, 1f, duration, t =>
            {
                float u = 1f - t;
                card.position = (u * u * start) + (2f * u * t * control) + (t * t * end);
            }).SetEase(Ease.OutCubic);
        }
    }
}
