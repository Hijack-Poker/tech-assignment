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
    public class HandNarrator : MonoBehaviour
    {
        private TableStateManager _stateManager;
        private int _lastNarratedGameNo = -1;
        private GameObject _banner;
        private TextMeshProUGUI _bannerText;
        private CanvasGroup _bannerCG;

        private static readonly string[] OpenTemplates =
        {
            "{winner} takes it down",
            "{winner} scoops the pot",
            "{winner} claims victory",
            "{winner} rakes in the chips",
            "The hand goes to {winner}",
        };

        private static readonly string[] HandRankTemplates =
        {
            "with {rank}",
            "holding {rank}",
            "showing {rank}",
            "revealing {rank}",
        };

        private static readonly string[] PotTemplates =
        {
            "collecting {pot}",
            "winning {pot}",
            "a {pot} pot",
            "for a pot of {pot}",
        };

        private static readonly string[] DramaFoldTemplates =
        {
            "Everyone else folded under the pressure.",
            "The rest of the table gave way.",
            "No one dared to contest.",
            "All opponents stepped aside.",
        };

        private static readonly string[] DramaAllInTemplates =
        {
            "An all-in showdown decided it!",
            "Chips flew as players went all-in!",
            "The all-in moment had the table buzzing.",
        };

        private void Awake()
        {
            _stateManager = FindObjectOfType<TableStateManager>();
        }

        private void OnEnable()
        {
            if (_stateManager != null)
                _stateManager.OnTableStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_stateManager != null)
                _stateManager.OnTableStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(TableResponse state)
        {
            if (state?.Game == null || state.Players == null) return;

            int step = state.Game.HandStep;
            int gameNo = state.Game.GameNo;

            // Fire at step 14-15, once per hand
            if (step < 14 || gameNo == _lastNarratedGameNo) return;
            _lastNarratedGameNo = gameNo;

            // Find winners with actual winnings
            var winners = state.Players.Where(p => p.IsWinner && p.Winnings > 0).ToList();
            if (winners.Count == 0) return;

            string narration = BuildNarration(winners, state, gameNo);
            ShowBanner(narration);
        }

        private string BuildNarration(List<PlayerState> winners, TableResponse state, int gameNo)
        {
            // Use gameNo as seed for varied but deterministic template selection
            int seed = gameNo;

            var mainWinner = winners[0];
            string winnerName = ColoredName(mainWinner);

            // Sentence 1: Winner announcement + hand rank
            string open = Pick(OpenTemplates, seed);
            open = open.Replace("{winner}", winnerName);

            string rankPart = "";
            if (!string.IsNullOrEmpty(mainWinner.HandRank))
            {
                string rankTemplate = Pick(HandRankTemplates, seed + 1);
                rankPart = " " + rankTemplate.Replace("{rank}", $"<b>{mainWinner.HandRank}</b>");
            }

            // Sentence 2: Pot size
            string potTemplate = Pick(PotTemplates, seed + 2);
            string potStr = potTemplate.Replace("{pot}", $"<color=#FFD700>{MoneyFormatter.Format(state.Game.Pot)}</color>");

            // Sentence 3: Drama (fold/all-in flavor)
            string drama = "";
            int foldCount = state.Players.Count(p => p.IsFolded);
            bool hasAllIn = state.Players.Any(p => p.IsAllIn);

            if (foldCount >= state.Players.Count - 1)
            {
                drama = " " + Pick(DramaFoldTemplates, seed + 3);
            }
            else if (hasAllIn)
            {
                drama = " " + Pick(DramaAllInTemplates, seed + 3);
            }

            // Multiple winners
            string multiWinner = "";
            if (winners.Count > 1)
            {
                var otherNames = winners.Skip(1).Select(p => ColoredName(p));
                multiWinner = $" {string.Join(" and ", otherNames)} also won a share.";
            }

            return $"{open}{rankPart}, {potStr}.{drama}{multiWinner}";
        }

        private static string Pick(string[] templates, int seed)
        {
            return templates[Mathf.Abs(seed) % templates.Length];
        }

        private static string ColoredName(PlayerState p)
        {
            var c = SeatView.GetSeatColor(p.Seat);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            return $"<color=#{hex}><b>{p.Username}</b></color>";
        }

        private void ShowBanner(string text)
        {
            EnsureBanner();
            if (_banner == null) return;

            _bannerText.text = text;
            _banner.SetActive(true);

            DOTween.Kill(_bannerCG);
            _bannerCG.alpha = 0f;

            DOTween.Sequence()
                .Append(DOTween.To(() => _bannerCG.alpha, x => _bannerCG.alpha = x, 1f, 0.5f).SetEase(Ease.OutCubic))
                .AppendInterval(8f)
                .Append(DOTween.To(() => _bannerCG.alpha, x => _bannerCG.alpha = x, 0f, 1f).SetEase(Ease.InCubic))
                .OnComplete(() => _banner.SetActive(false))
                .SetTarget(_bannerCG);
        }

        private void EnsureBanner()
        {
            if (_banner != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Dark banner at bottom-center
            _banner = new GameObject("HandNarrationBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _banner.transform.SetParent(canvas.transform, false);

            var rt = _banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            rt.sizeDelta = new Vector2(500f, 60f);

            var bg = _banner.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            _bannerCG = _banner.GetComponent<CanvasGroup>();
            _bannerCG.alpha = 0f;

            // Text child
            var textGO = new GameObject("NarrationText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(_banner.transform, false);

            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 6f);
            textRt.offsetMax = new Vector2(-12f, -6f);

            _bannerText = textGO.GetComponent<TextMeshProUGUI>();
            _bannerText.fontSize = 14;
            _bannerText.color = new Color(0.9f, 0.9f, 0.9f);
            _bannerText.alignment = TextAlignmentOptions.Center;
            _bannerText.richText = true;
            _bannerText.enableWordWrapping = true;

            _banner.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_bannerCG);
            if (_banner != null)
                Destroy(_banner);
        }
    }
}
