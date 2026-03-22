using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.UI;
using HijackPoker.Utils;

namespace HijackPoker.AI
{
    public enum NarrativeSituation
    {
        BluffSteal, Cooler, AllInClash, DominantWin,
        BigLaydown, CheckRaise, Standard
    }

    public class HandNarrator : MonoBehaviour
    {
        private HandDataCollector _collector;
        private GameObject _banner;
        private TextMeshProUGUI _bannerText;
        private CanvasGroup _bannerCG;

        private void Awake() => _collector = FindObjectOfType<HandDataCollector>();

        private void OnEnable()
        {
            if (_collector != null) _collector.OnHandCompleted += OnHandCompleted;
        }

        private void OnDisable()
        {
            if (_collector != null) _collector.OnHandCompleted -= OnHandCompleted;
        }

        private void OnHandCompleted(HandSnapshot hand)
        {
            var winner = hand.PlayerRecords.FirstOrDefault(p => p.IsWinner);
            if (winner == null) return;

            var board = BoardAnalyzer.Analyze(hand.CommunityCards);
            var situation = Classify(hand, board);
            string narration = BuildNarration(situation, hand, winner, board);
            ShowBanner(narration);
        }

        // ── Situation Classification ──────────────────────────

        private static NarrativeSituation Classify(HandSnapshot hand, BoardTexture board)
        {
            var winner = hand.PlayerRecords.FirstOrDefault(p => p.IsWinner);
            if (winner == null) return NarrativeSituation.Standard;

            // Bluff/steal: won without showdown with significant pot
            if (hand.Outcome == HandOutcome.FoldWin && hand.Pot > hand.BigBlind * 4)
                return NarrativeSituation.BluffSteal;

            // All-in clash
            int allInCount = hand.PlayerRecords.Count(p => p.FinalStatus == "allin");
            if (allInCount >= 2) return NarrativeSituation.AllInClash;

            // Showdown analysis
            var showdown = hand.PlayerRecords
                .Where(p => p.FinalStatus != "folded" && !string.IsNullOrEmpty(p.HandRank))
                .ToList();

            if (showdown.Count >= 2)
            {
                int winTier = BoardAnalyzer.HandRankTier(winner.HandRank);
                int bestLoserTier = showdown.Where(p => !p.IsWinner)
                    .Select(p => BoardAnalyzer.HandRankTier(p.HandRank)).DefaultIfEmpty(0).Max();

                // Cooler: both have strong hands
                if (winTier >= 4 && bestLoserTier >= 4)
                    return NarrativeSituation.Cooler;
                if (winTier >= 5 && bestLoserTier >= 3)
                    return NarrativeSituation.Cooler;
            }

            // Dominant win: monster hand
            if (!string.IsNullOrEmpty(winner.HandRank) && BoardAnalyzer.HandRankTier(winner.HandRank) >= 7)
                return NarrativeSituation.DominantWin;

            // Check-raise detection
            foreach (var rec in hand.PlayerRecords)
            {
                for (int i = 0; i < rec.Actions.Count - 1; i++)
                {
                    if (rec.Actions[i].Action == "check" &&
                        (rec.Actions[i + 1].Action == "raise" || rec.Actions[i + 1].Action == "bet") &&
                        rec.Actions[i].BettingRound == rec.Actions[i + 1].BettingRound)
                        return NarrativeSituation.CheckRaise;
                }
            }

            // Big laydown
            foreach (var rec in hand.PlayerRecords)
            {
                if (rec.FinalStatus == "folded" && rec.TotalInvested > hand.BigBlind * 5)
                    return NarrativeSituation.BigLaydown;
            }

            return NarrativeSituation.Standard;
        }

        // ── Narration Builder ─────────────────────────────────

        private string BuildNarration(NarrativeSituation sit, HandSnapshot hand,
            PlayerHandRecord winner, BoardTexture board)
        {
            string wName = ColoredName(winner);
            string pot = $"<color=#FFD700>{MoneyFormatter.Format(hand.Pot)}</color>";
            string rank = !string.IsNullOrEmpty(winner.HandRank) ? $"<b>{winner.HandRank}</b>" : "a strong hand";
            int seed = hand.GameNo;

            // Find best loser at showdown
            var loser = hand.PlayerRecords
                .Where(p => !p.IsWinner && p.FinalStatus != "folded" && !string.IsNullOrEmpty(p.HandRank))
                .OrderByDescending(p => BoardAnalyzer.HandRankTier(p.HandRank))
                .FirstOrDefault();
            string lName = loser != null ? ColoredName(loser) : "";
            string lRank = loser != null ? $"<b>{loser.HandRank}</b>" : "";

            string main = sit switch
            {
                NarrativeSituation.BluffSteal => Pick(seed, _bluffTemplates)
                    .Replace("{w}", wName).Replace("{pot}", pot),

                NarrativeSituation.Cooler => Pick(seed, _coolerTemplates)
                    .Replace("{w}", wName).Replace("{l}", lName)
                    .Replace("{wr}", rank).Replace("{lr}", lRank).Replace("{pot}", pot),

                NarrativeSituation.AllInClash => Pick(seed, _allInTemplates)
                    .Replace("{w}", wName).Replace("{rank}", rank).Replace("{pot}", pot),

                NarrativeSituation.DominantWin => Pick(seed, _dominantTemplates)
                    .Replace("{w}", wName).Replace("{rank}", rank).Replace("{pot}", pot),

                NarrativeSituation.CheckRaise => Pick(seed, _checkRaiseTemplates)
                    .Replace("{w}", wName).Replace("{rank}", rank).Replace("{pot}", pot),

                NarrativeSituation.BigLaydown => BuildBigLaydownNarration(hand, winner, wName, pot, seed),

                _ => Pick(seed, _standardTemplates)
                    .Replace("{w}", wName).Replace("{rank}", rank).Replace("{pot}", pot),
            };

            // Board texture comment (only for showdown hands with community cards)
            string boardComment = "";
            if (hand.ReachedShowdown && hand.CommunityCards.Count >= 3)
                boardComment = GetBoardComment(board, seed);

            return boardComment.Length > 0 ? $"{main}\n<size=11>{boardComment}</size>" : main;
        }

        private string BuildBigLaydownNarration(HandSnapshot hand, PlayerHandRecord winner,
            string wName, string pot, int seed)
        {
            var folder = hand.PlayerRecords
                .Where(p => p.FinalStatus == "folded" && p.TotalInvested > hand.BigBlind * 5)
                .OrderByDescending(p => p.TotalInvested).FirstOrDefault();
            if (folder == null)
                return Pick(seed, _standardTemplates).Replace("{w}", wName).Replace("{rank}", "").Replace("{pot}", pot);

            string fName = ColoredName(folder);
            string invested = MoneyFormatter.Format(folder.TotalInvested);
            return Pick(seed, _laydownTemplates)
                .Replace("{f}", fName).Replace("{inv}", invested)
                .Replace("{w}", wName).Replace("{pot}", pot);
        }

        private static string GetBoardComment(BoardTexture board, int seed)
        {
            if (board.HasFlushDraw && board.HasStraightDraw)
                return Pick(seed + 10, _wetBoardTemplates).Replace("{suit}", BoardAnalyzer.SuitName(board.DominantSuit));
            if (board.HasFlushDraw)
                return $"Three {BoardAnalyzer.SuitName(board.DominantSuit)} on the board made things interesting.";
            if (board.IsPairedBoard)
                return Pick(seed + 10, _pairedBoardTemplates);
            if (board.HasStraightDraw)
                return "Connected cards on the board opened straight possibilities.";
            if (board.Wetness == "dry")
                return Pick(seed + 10, _dryBoardTemplates);
            return "";
        }

        // ── Templates ─────────────────────────────────────────

        private static readonly string[] _bluffTemplates =
        {
            "{w} fires big and takes {pot} uncontested. Bluff or value? Only they know.",
            "All fold to {w}'s pressure. A well-timed move worth {pot}.",
            "{w} shows strength and everyone backs down — {pot} slides their way.",
            "Nobody wants to find out. {w} takes {pot} without a showdown.",
        };

        private static readonly string[] _coolerTemplates =
        {
            "A brutal cooler! {w}'s {wr} edges out {l}'s {lr} in a {pot} pot.",
            "Both had monsters — {w}'s {wr} vs {l}'s {lr}. Nothing either could do.",
            "{w} vs {l}: {wr} over {lr}. Cooler for {pot}. Poker can be cruel.",
        };

        private static readonly string[] _allInTemplates =
        {
            "All the chips go in! {w} survives with {rank}, taking down {pot}!",
            "A dramatic all-in showdown — {w}'s {rank} holds up for {pot}!",
            "Stacks on the line! {w} emerges victorious with {rank} for {pot}.",
        };

        private static readonly string[] _dominantTemplates =
        {
            "{w} shows {rank} — an absolute monster, collecting {pot}.",
            "No contest. {w}'s {rank} crushes the table for {pot}.",
            "{w} flips over {rank}! The table watches as {pot} slides over.",
        };

        private static readonly string[] _checkRaiseTemplates =
        {
            "The check-raise trap springs! {w} takes {pot} with {rank}.",
            "Patience pays off — {w} check-raises and collects {pot} with {rank}.",
        };

        private static readonly string[] _laydownTemplates =
        {
            "{f} makes a tough fold after investing {inv}. {w} takes the {pot} pot.",
            "A big laydown from {f}, surrendering {inv}. {w} claims {pot}.",
        };

        private static readonly string[] _standardTemplates =
        {
            "{w} takes the {pot} pot with {rank}.",
            "{w} wins {pot}, showing {rank}.",
            "Hand goes to {w} — {rank} for {pot}.",
            "{w} scoops {pot} with {rank}. Clean.",
        };

        private static readonly string[] _wetBoardTemplates =
        {
            "A dangerous board with {suit} and straight draws kept everyone guessing.",
            "Flush and straight possibilities on a wet board — pure tension.",
        };

        private static readonly string[] _pairedBoardTemplates =
        {
            "The paired board threatened full houses at every turn.",
            "A paired board made anyone without trips nervous.",
        };

        private static readonly string[] _dryBoardTemplates =
        {
            "On a dry, disconnected board — hand strength was king.",
            "With few draws possible on this dry board, it was all about the cards.",
        };

        // ── Helpers ───────────────────────────────────────────

        private static string Pick(int seed, string[] arr) => arr[Mathf.Abs(seed) % arr.Length];

        private static string ColoredName(PlayerHandRecord p)
        {
            var c = SeatView.GetSeatColor(p.Seat);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            return $"<color=#{hex}><b>{p.Username}</b></color>";
        }

        // ── Banner UI ─────────────────────────────────────────

        private void ShowBanner(string text)
        {
            EnsureBanner();
            if (_banner == null) return;

            _bannerText.text = text;
            _banner.SetActive(true);

            DOTween.Kill(_bannerCG);
            _bannerCG.alpha = 0f;

            DOTween.Sequence()
                .Append(DOTween.To(() => _bannerCG.alpha, x => _bannerCG.alpha = x, 1f, 0.4f).SetEase(Ease.OutCubic))
                .AppendInterval(9f)
                .Append(DOTween.To(() => _bannerCG.alpha, x => _bannerCG.alpha = x, 0f, 1.2f).SetEase(Ease.InCubic))
                .OnComplete(() => _banner.SetActive(false))
                .SetTarget(_bannerCG);
        }

        private void EnsureBanner()
        {
            if (_banner != null) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _banner = new GameObject("HandNarrationBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _banner.transform.SetParent(canvas.transform, false);
            _banner.transform.SetAsLastSibling();

            var rt = _banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta = new Vector2(520f, 70f);

            _banner.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.94f);
            _bannerCG = _banner.GetComponent<CanvasGroup>();
            _bannerCG.alpha = 0f;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(_banner.transform, false);
            var tRt = textGO.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(14f, 6f);
            tRt.offsetMax = new Vector2(-14f, -6f);

            _bannerText = textGO.GetComponent<TextMeshProUGUI>();
            _bannerText.fontSize = 13;
            _bannerText.color = new Color(0.92f, 0.92f, 0.92f);
            _bannerText.alignment = TextAlignmentOptions.Center;
            _bannerText.richText = true;
            _bannerText.enableWordWrapping = true;

            _banner.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_bannerCG);
            if (_banner != null) Destroy(_banner);
        }
    }
}
