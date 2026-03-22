using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.AI
{
    public class CoachingTip
    {
        public string Category; // "Preflop", "Postflop", "Position", "Showdown", "Strategy", "Mental", "General"
        public string Message;
        public int Severity; // 0=info, 1=warning, 2=critical
    }

    public class SessionStats
    {
        public int HandsPlayed, HandsWon;
        public float WinRate => HandsPlayed > 0 ? (float)HandsWon / HandsPlayed : 0f;
        public float BiggestWin, BiggestLoss, TotalProfit, StartingStack;
        public int CurrentStreak, BestStreak, WorstStreak;

        // Core poker stats
        public int VpipCount, PfrCount, VpipEligibleHands;
        public float VPIP => VpipEligibleHands > 0 ? (float)VpipCount / VpipEligibleHands : 0f;
        public float PFR => VpipEligibleHands > 0 ? (float)PfrCount / VpipEligibleHands : 0f;
        public int PostflopBets, PostflopCalls;
        public float AF => PostflopCalls > 0 ? (float)PostflopBets / PostflopCalls : PostflopBets > 0 ? 10f : 0f;

        // Actions
        public int FoldCount, CallCount, RaiseCount, AllInCount;

        // Showdown stats
        public int WentToShowdownCount, WonAtShowdownCount;
        public float WTSD => HandsPlayed > 0 ? (float)WentToShowdownCount / HandsPlayed : 0f;
        public float WSD => WentToShowdownCount > 0 ? (float)WonAtShowdownCount / WentToShowdownCount : 0f;

        // Continuation bet tracking
        public int ContinuationBets, ContinuationBetOpps;
        public float CBet => ContinuationBetOpps > 0 ? (float)ContinuationBets / ContinuationBetOpps : 0f;

        // River play
        public int RiverBets, RiverCalls;

        // Position tracking
        public int WinsFromButton, WinsFromBlinds, WinsFromOther;
        public int HandsFromButton, HandsFromBlinds, HandsFromOther;

        // Per-hand review
        public string LastHandReview;

        // Coaching
        public List<CoachingTip> Tips = new();
    }

    public class SessionTracker : MonoBehaviour
    {
        private HandDataCollector _collector;
        private TableStateManager _stateManager;
        private string _localPlayerName;
        private int _localSeat;

        private readonly SessionStats _stats = new SessionStats();

        public event Action<SessionStats> OnStatsUpdated;
        public SessionStats CurrentStats => _stats;

        private void Awake()
        {
            _collector = FindObjectOfType<HandDataCollector>();
            _stateManager = FindObjectOfType<TableStateManager>();
        }

        private void OnEnable()
        {
            if (_collector != null) _collector.OnHandCompleted += OnHandCompleted;
            if (_stateManager != null) _stateManager.OnTableReset += OnTableReset;
        }

        private void OnDisable()
        {
            if (_collector != null) _collector.OnHandCompleted -= OnHandCompleted;
            if (_stateManager != null) _stateManager.OnTableReset -= OnTableReset;
        }

        private void OnTableReset()
        {
            _localSeat = 0;
        }

        private void OnHandCompleted(HandSnapshot hand)
        {
            ResolveLocalPlayer(hand);
            var rec = FindLocalRecord(hand);
            if (rec == null) return;

            ProcessHand(rec, hand);
            GenerateHandReview(rec, hand);
            GenerateCoachingTips();
            OnStatsUpdated?.Invoke(_stats);
        }

        private void ResolveLocalPlayer(HandSnapshot hand)
        {
            if (_localSeat > 0) return;
            _localPlayerName = PlayerPrefs.GetString("PlayerName", "Player");
            foreach (var r in hand.PlayerRecords)
            {
                if (!string.IsNullOrEmpty(r.Username) &&
                    r.Username.Equals(_localPlayerName, StringComparison.OrdinalIgnoreCase))
                {
                    _localSeat = r.Seat;
                    return;
                }
            }
            _localSeat = 1; // fallback
        }

        private PlayerHandRecord FindLocalRecord(HandSnapshot hand)
        {
            foreach (var r in hand.PlayerRecords)
                if (r.Seat == _localSeat) return r;
            return null;
        }

        private void ProcessHand(PlayerHandRecord rec, HandSnapshot hand)
        {
            _stats.HandsPlayed++;
            _stats.VpipEligibleHands++;

            if (_stats.StartingStack <= 0f)
                _stats.StartingStack = rec.StartingStack;

            // Core stats
            if (rec.VoluntarilyPutMoneyIn) _stats.VpipCount++;
            if (rec.RaisedPreflop) _stats.PfrCount++;

            // Postflop aggression
            foreach (var a in rec.Actions)
            {
                if (a.BettingRound == 0) continue;
                if (a.Action == "bet" || a.Action == "raise" || a.Action == "allin")
                    _stats.PostflopBets++;
                else if (a.Action == "call")
                    _stats.PostflopCalls++;
            }

            // Action counts
            foreach (var a in rec.Actions)
            {
                switch (a.Action)
                {
                    case "fold": _stats.FoldCount++; break;
                    case "call": case "check": _stats.CallCount++; break;
                    case "bet": case "raise": _stats.RaiseCount++; break;
                    case "allin": _stats.AllInCount++; break;
                }
            }

            // Continuation bet tracking
            if (rec.RaisedPreflop && hand.BettingRoundsCompleted >= 2)
            {
                _stats.ContinuationBetOpps++;
                bool cbet = rec.Actions.Any(a =>
                    a.BettingRound == 1 && (a.Action == "bet" || a.Action == "raise" || a.Action == "allin"));
                if (cbet) _stats.ContinuationBets++;
            }

            // Showdown tracking
            if (hand.ReachedShowdown && rec.FinalStatus != "folded")
            {
                _stats.WentToShowdownCount++;
                if (rec.IsWinner) _stats.WonAtShowdownCount++;
            }

            // River play
            foreach (var a in rec.Actions)
            {
                if (a.BettingRound == 3)
                {
                    if (a.Action == "bet" || a.Action == "raise" || a.Action == "allin")
                        _stats.RiverBets++;
                    else if (a.Action == "call")
                        _stats.RiverCalls++;
                }
            }

            // Position tracking
            switch (rec.Position)
            {
                case PositionType.Button:
                    _stats.HandsFromButton++;
                    if (rec.IsWinner) _stats.WinsFromButton++;
                    break;
                case PositionType.SmallBlind:
                case PositionType.BigBlind:
                    _stats.HandsFromBlinds++;
                    if (rec.IsWinner) _stats.WinsFromBlinds++;
                    break;
                default:
                    _stats.HandsFromOther++;
                    if (rec.IsWinner) _stats.WinsFromOther++;
                    break;
            }

            // Win/loss tracking
            if (rec.IsWinner)
            {
                _stats.HandsWon++;
                if (rec.Winnings > _stats.BiggestWin) _stats.BiggestWin = rec.Winnings;
                _stats.CurrentStreak = _stats.CurrentStreak >= 0 ? _stats.CurrentStreak + 1 : 1;
                if (_stats.CurrentStreak > _stats.BestStreak) _stats.BestStreak = _stats.CurrentStreak;
            }
            else
            {
                float loss = rec.TotalInvested;
                if (loss > _stats.BiggestLoss) _stats.BiggestLoss = loss;
                _stats.CurrentStreak = _stats.CurrentStreak <= 0 ? _stats.CurrentStreak - 1 : -1;
                if (_stats.CurrentStreak < _stats.WorstStreak) _stats.WorstStreak = _stats.CurrentStreak;
            }

            _stats.TotalProfit = rec.EndingStack - _stats.StartingStack;
        }

        // ── Per-Hand Review ──────────────────────────────────────

        private void GenerateHandReview(PlayerHandRecord rec, HandSnapshot hand)
        {
            var insights = new List<(string text, int priority)>();
            var board = BoardAnalyzer.Analyze(hand.CommunityCards);
            string posName = PositionName(rec.Position);
            float bbInvested = hand.BigBlind > 0 ? rec.TotalInvested / hand.BigBlind : 0f;
            int handTier = BoardAnalyzer.HandRankTier(rec.HandRank);

            var winner = hand.PlayerRecords.FirstOrDefault(p => p.IsWinner);
            int winnerTier = winner != null ? BoardAnalyzer.HandRankTier(winner.HandRank) : 0;

            // ── MISTAKES ──

            // Limping from non-blind position
            if (rec.VoluntarilyPutMoneyIn && !rec.RaisedPreflop &&
                rec.Position != PositionType.BigBlind && rec.Position != PositionType.SmallBlind)
            {
                insights.Add((
                    $"You limped from {posName}. Raise or fold instead \u2014 limping invites multiway pots where your hand equity drops and you lose control of the action.",
                    5));
            }

            // Overcommitting with weak hand at showdown
            if (!rec.IsWinner && bbInvested > 8 && hand.ReachedShowdown && handTier <= 2)
            {
                insights.Add((
                    $"You invested {MoneyFormatter.Format(rec.TotalInvested)} ({bbInvested:F0} BB) with {rec.HandRank ?? "a weak hand"} and lost. On a {board.Wetness} board, recognize when your one-pair hand is likely dominated and cut losses earlier.",
                    6));
            }

            // Calling down multiple streets to lose
            if (!rec.IsWinner && hand.ReachedShowdown && rec.FinalStatus != "folded")
            {
                int callCount = rec.Actions.Count(a => a.BettingRound > 0 && a.Action == "call");
                if (callCount >= 2 && handTier <= 2)
                {
                    insights.Add((
                        $"You called {callCount} streets postflop and lost at showdown. Ask yourself: \"Can I beat the hands that bet this way?\" If not, folding saves more than calling costs.",
                        5));
                }
            }

            // Late fold after heavy investment
            if (rec.FinalStatus == "folded" && bbInvested > 5)
            {
                insights.Add((
                    $"You folded after investing {MoneyFormatter.Format(rec.TotalInvested)} ({bbInvested:F0} BB). Plan ahead \u2014 commit with strong hands or fold early. Half-measures are the most expensive mistake.",
                    5));
            }

            // All-in with marginal hand and lost
            if (rec.FinalStatus == "allin" && !rec.IsWinner && handTier <= 3)
            {
                insights.Add((
                    $"Your all-in with {rec.HandRank ?? "a marginal hand"} didn't hold up. Reserve stack-committing plays for top pair/top kicker or better, or bluffs where opponents can fold.",
                    6));
            }

            // No c-bet after raising preflop
            if (rec.RaisedPreflop && hand.BettingRoundsCompleted >= 2 && rec.FinalStatus != "folded")
            {
                bool betFlop = rec.Actions.Any(a => a.BettingRound == 1 &&
                    (a.Action == "bet" || a.Action == "raise" || a.Action == "allin"));
                bool checkedFlop = rec.Actions.Any(a => a.BettingRound == 1 && a.Action == "check");
                if (!betFlop && checkedFlop && !rec.IsWinner)
                {
                    insights.Add((
                        "You raised preflop but didn't continuation bet the flop. As the preflop aggressor, fire a c-bet 60-70% of the time \u2014 you have the perceived range advantage.",
                        4));
                }
            }

            // ── MISSED OPPORTUNITIES ──

            // Under-valuing a strong hand (won but pot was small)
            if (rec.IsWinner && hand.ReachedShowdown && handTier >= 5)
            {
                float potBBs = hand.BigBlind > 0 ? hand.Pot / hand.BigBlind : 0;
                if (potBBs < 12)
                {
                    insights.Add((
                        $"You won with {rec.HandRank} but only built a {MoneyFormatter.Format(hand.Pot)} pot. Bet larger for value \u2014 opponents pay off strong hands more often than you think.",
                        4));
                }
            }

            // Slow-playing on wet board
            if (rec.IsWinner && hand.ReachedShowdown && handTier >= 4)
            {
                int checkCount = rec.Actions.Count(a => a.BettingRound > 0 && a.Action == "check");
                if (checkCount >= 2 && board.Wetness != "dry")
                {
                    insights.Add((
                        $"You checked {checkCount} times postflop with {rec.HandRank} on a {board.Wetness} board. Slow-playing here lets draws catch up for free \u2014 bet to protect your equity.",
                        4));
                }
            }

            // Folded BB to small raise
            if (rec.Position == PositionType.BigBlind && rec.FinalStatus == "folded" && bbInvested <= 1.5f)
            {
                insights.Add((
                    "You folded your big blind. You already have chips invested \u2014 defend with suited connectors, small pairs, and broadway cards when facing small raises. 3-4:1 pot odds make many hands profitable.",
                    3));
            }

            // Flat-calling from late position instead of raising
            if (!rec.RaisedPreflop && rec.VoluntarilyPutMoneyIn &&
                (rec.Position == PositionType.Button || rec.Position == PositionType.Cutoff))
            {
                insights.Add((
                    $"You flat-called from {posName}. Raising here puts pressure on the blinds, takes the initiative, and lets you win pots with c-bets even when you miss.",
                    3));
            }

            // Passive on the river with a strong hand
            if (rec.IsWinner && hand.BettingRoundsCompleted >= 4 && handTier >= 3)
            {
                bool betRiver = rec.Actions.Any(a => a.BettingRound == 3 &&
                    (a.Action == "bet" || a.Action == "raise"));
                bool checkedRiver = rec.Actions.Any(a => a.BettingRound == 3 && a.Action == "check");
                if (!betRiver && checkedRiver)
                {
                    insights.Add((
                        $"You checked the river with {rec.HandRank}. A value bet would often get called by worse hands. Missing river value bets is one of the biggest long-term leaks.",
                        3));
                }
            }

            // Playing from early position without raising
            if (rec.VoluntarilyPutMoneyIn && !rec.RaisedPreflop &&
                rec.Position == PositionType.EarlyPosition && !rec.IsWinner)
            {
                insights.Add((
                    "You entered the pot from early position without raising. EP requires the tightest range \u2014 you act first on every street. Only play premium hands here and always raise them.",
                    4));
            }

            // ── BOARD TEXTURE READS ──

            if (!rec.IsWinner && hand.ReachedShowdown && handTier <= 3)
            {
                if (board.HasFlushDraw && board.HasStraightDraw)
                    insights.Add((
                        "The board was extremely wet with flush and straight draws. Marginal hands lose value fast on coordinated boards \u2014 check/fold when facing bets unless you hold the nuts.",
                        3));
                else if (board.HasFlushDraw)
                    insights.Add((
                        $"Three {BoardAnalyzer.SuitName(board.DominantSuit)} on the board meant flush danger. When a flush completes, re-evaluate \u2014 top pair is often no good against a flush.",
                        2));
                else if (board.IsPairedBoard && !rec.IsWinner && handTier <= 2)
                    insights.Add((
                        "The paired board made full houses possible. With just a pair on a paired board, proceed cautiously \u2014 an opponent with trips or a full house has you crushed.",
                        2));
            }

            // ── STACK MANAGEMENT ──

            if (rec.EndingStack > 0 && hand.BigBlind > 0 && rec.EndingStack < hand.BigBlind * 10)
            {
                float remainingBBs = rec.EndingStack / hand.BigBlind;
                insights.Add((
                    $"Stack at {remainingBBs:F0} BB \u2014 push/fold territory. With a short stack, shove all-in with any decent hand or fold. No more small raises or limps.",
                    5));
            }

            // ── POSITIVE REINFORCEMENT ──

            // Good bluff win
            if (rec.IsWinner && hand.Outcome == HandOutcome.FoldWin && bbInvested > 3)
            {
                insights.Add((
                    "Well-timed aggression \u2014 you took down the pot without showdown. Selective bluffing creates fold equity and keeps opponents guessing.",
                    1));
            }

            // Winning from position
            if (rec.IsWinner && (rec.Position == PositionType.Button || rec.Position == PositionType.Cutoff))
            {
                insights.Add((
                    $"Nice win from {posName}. Position is the single biggest edge in poker \u2014 acting last gives you information and pot control.",
                    1));
            }

            // Good fold when opponent had monster
            if (rec.FinalStatus == "folded" && winner != null && winnerTier >= 5 && bbInvested < 5)
            {
                insights.Add((
                    $"Smart fold \u2014 the winner had {winner.HandRank ?? "a strong hand"}. Recognizing danger and saving chips is a hallmark of winning players.",
                    1));
            }

            // Cooler loss (nothing you could do)
            if (!rec.IsWinner && hand.ReachedShowdown && handTier >= 4 && winnerTier >= 5)
            {
                insights.Add((
                    $"Tough spot \u2014 your {rec.HandRank ?? "strong hand"} ran into {winner?.HandRank ?? "a monster"}. This is a cooler, not a mistake. Don't let it affect your next decisions.",
                    2));
            }

            // Good value bet that won
            if (rec.IsWinner && hand.ReachedShowdown && handTier >= 3)
            {
                int betCount = rec.Actions.Count(a => a.BettingRound > 0 &&
                    (a.Action == "bet" || a.Action == "raise"));
                if (betCount >= 2)
                {
                    insights.Add((
                        $"Great value betting \u2014 you bet multiple streets with {rec.HandRank} and got paid. Consistent aggression with strong hands maximizes profit.",
                        1));
                }
            }

            // ── BUILD FINAL REVIEW ──

            if (insights.Count == 0)
            {
                if (rec.IsWinner)
                    _stats.LastHandReview = "Solid play \u2014 pot taken down efficiently. Keep making disciplined decisions.";
                else if (rec.FinalStatus == "folded" && bbInvested <= 1.5f)
                    _stats.LastHandReview = "Disciplined fold. Saving chips is just as important as winning pots \u2014 patience is profitable.";
                else
                    _stats.LastHandReview = "Standard hand. Keep reading the board, tracking opponent tendencies, and playing position.";
                return;
            }

            insights.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Show top 1-2 insights
            if (insights.Count >= 2 && insights[0].priority >= 4 && insights[1].priority >= 3)
                _stats.LastHandReview = insights[0].text + "\n" + insights[1].text;
            else
                _stats.LastHandReview = insights[0].text;
        }

        // ── Leak Detection / Coaching ──────────────────────────

        private void GenerateCoachingTips()
        {
            _stats.Tips.Clear();

            int h = _stats.HandsPlayed;
            float vpip = _stats.VPIP;
            float pfr = _stats.PFR;
            float af = _stats.AF;

            // === EARLY GAME (hands 1-3) — strategic context ===
            if (h <= 3)
            {
                if (_stats.HandsWon > 0)
                    _stats.Tips.Add(new CoachingTip { Category = "Strategy", Severity = 0,
                        Message = "Good start. Focus on playing tight from early position and aggressive from late position \u2014 position is your biggest edge." });
                else
                    _stats.Tips.Add(new CoachingTip { Category = "Strategy", Severity = 0,
                        Message = "Don't worry about early results. Focus on making the best decision each hand \u2014 good process beats good luck." });

                _stats.Tips.Add(new CoachingTip { Category = "General", Severity = 0,
                    Message = "Watch opponents' bet sizing closely \u2014 large bets often mean strength or a bluff, while small bets invite calls with draws." });

                TrimTips(4);
                return;
            }

            // === PREFLOP LEAKS ===

            // VPIP too high
            if (vpip > 0.50f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 2,
                    Message = $"VPIP {vpip:P0} is way too loose. Fold junk like K5o, J3s, Q7o \u2014 they bleed chips long-term. Target 25-30%." });
            else if (vpip > 0.40f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 2,
                    Message = $"VPIP {vpip:P0} is too high. Drop weak suited hands and low offsuit connectors from early/middle position. You're entering pots with negative equity." });
            else if (vpip > 0.35f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 1,
                    Message = $"VPIP {vpip:P0} is a bit loose. Tighten early position opens and only play speculative hands (suited connectors, small pairs) in position." });

            // VPIP too low
            if (h >= 4 && vpip < 0.15f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 2,
                    Message = $"VPIP {vpip:P0} is too tight \u2014 blinds are eating your stack. Open wider from the button (40-50%) and cutoff (25-35%)." });
            else if (h >= 4 && vpip > 0f && vpip < 0.20f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 1,
                    Message = $"VPIP {vpip:P0} is very tight. You're missing profitable spots in late position. Selective aggression from the button and cutoff is +EV." });

            // VPIP-PFR gap (cold calling / limping)
            if (h >= 4 && vpip > 0.12f && vpip - pfr > 0.15f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 2,
                    Message = $"VPIP {vpip:P0} vs PFR {pfr:P0} \u2014 too much cold-calling. \"Raise or fold\" avoids bloating pots out of position with marginal hands." });

            // PFR too high
            if (h >= 5 && pfr > 0.40f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 1,
                    Message = $"PFR {pfr:P0} is extremely aggressive. Raising everything dilutes your strong range and makes you predictable. Target 18-25% PFR." });

            // === POSTFLOP LEAKS ===

            int totalPostflop = _stats.PostflopBets + _stats.PostflopCalls;

            // Passive postflop
            if (af < 0.8f && totalPostflop >= 4)
                _stats.Tips.Add(new CoachingTip { Category = "Postflop", Severity = 2,
                    Message = $"AF {af:F1} \u2014 you're a calling station postflop. Betting wins pots two ways: making better hands fold, and getting worse hands to call." });
            else if (af < 1.2f && totalPostflop >= 4)
                _stats.Tips.Add(new CoachingTip { Category = "Postflop", Severity = 1,
                    Message = $"AF {af:F1} is passive. Lead out with bets when you have draws (semi-bluffs) and strong hands (value). Aggression wins pots that checking never will." });

            // Over-aggressive
            if (af > 5f && _stats.PostflopCalls >= 3)
                _stats.Tips.Add(new CoachingTip { Category = "Postflop", Severity = 1,
                    Message = $"AF {af:F1} is very high. Mix in calls with medium-strength hands to trap aggressive opponents and balance your ranges." });

            // C-bet frequency
            if (_stats.ContinuationBetOpps >= 3)
            {
                float cbet = _stats.CBet;
                if (cbet < 0.40f)
                    _stats.Tips.Add(new CoachingTip { Category = "Postflop", Severity = 2,
                        Message = $"C-betting only {cbet:P0} after raising preflop. Fire a continuation bet 60-70% of flops \u2014 you have the range advantage as the preflop raiser." });
                else if (cbet > 0.85f)
                    _stats.Tips.Add(new CoachingTip { Category = "Postflop", Severity = 1,
                        Message = $"C-betting {cbet:P0} is too frequent. Check back boards that favor the caller's range (low, connected) to protect your checking range." });
            }

            // River play
            if (_stats.RiverBets + _stats.RiverCalls >= 3)
            {
                float riverAgg = _stats.RiverCalls > 0
                    ? (float)_stats.RiverBets / _stats.RiverCalls
                    : _stats.RiverBets > 0 ? 10f : 0f;
                if (riverAgg < 0.5f)
                    _stats.Tips.Add(new CoachingTip { Category = "Postflop", Severity = 1,
                        Message = "Too passive on the river \u2014 the biggest bets happen here. Missing value bets on the river is the most costly leak. Bet strong hands." });
            }

            // === SHOWDOWN ANALYSIS ===

            if (_stats.WentToShowdownCount >= 3)
            {
                float wsd = _stats.WSD;
                float wtsd = _stats.WTSD;

                if (wtsd > 0.50f)
                    _stats.Tips.Add(new CoachingTip { Category = "Showdown", Severity = 1,
                        Message = $"WTSD {wtsd:P0} is too high \u2014 you're calling too much on later streets. When the action screams strength, fold marginal hands." });

                if (wsd < 0.35f)
                    _stats.Tips.Add(new CoachingTip { Category = "Showdown", Severity = 2,
                        Message = $"Winning only {wsd:P0} at showdown. You're arriving with second-best hands. Tighten your calling range or convert some calls into bluff raises." });
                else if (wsd > 0.70f && _stats.WentToShowdownCount >= 4)
                    _stats.Tips.Add(new CoachingTip { Category = "Showdown", Severity = 0,
                        Message = $"WSD {wsd:P0} \u2014 excellent hand selection. Look for thin value bets to maximize profit from the weaker hands that call you." });
            }

            // === POSITION PLAY ===

            if (_stats.HandsFromButton >= 3 && _stats.WinsFromButton == 0 && vpip < 0.30f)
                _stats.Tips.Add(new CoachingTip { Category = "Position", Severity = 1,
                    Message = "Not exploiting the button \u2014 the most profitable seat. Raise 40-50% of hands here. Acting last on every street gives you maximum information and control." });

            if (_stats.HandsFromBlinds >= 4)
            {
                float blindWR = (float)_stats.WinsFromBlinds / _stats.HandsFromBlinds;
                if (blindWR == 0f)
                    _stats.Tips.Add(new CoachingTip { Category = "Position", Severity = 1,
                        Message = "No wins from the blinds. Defend your BB with suited connectors, pairs, and broadway cards \u2014 you're getting good pot odds on your investment." });
            }

            // === FOLD FREQUENCY ===

            float foldPct = 1f - vpip;
            if (h >= 5 && foldPct > 0.80f)
                _stats.Tips.Add(new CoachingTip { Category = "Preflop", Severity = 1,
                    Message = $"Folding {foldPct:P0} of hands. Opponents will exploit this by stealing your blinds every orbit. Look for steal spots from late position." });

            // === ALL-IN FREQUENCY ===

            if (_stats.AllInCount >= 2 && h >= 4)
            {
                float allInPct = (float)_stats.AllInCount / h;
                if (allInPct > 0.40f)
                    _stats.Tips.Add(new CoachingTip { Category = "Strategy", Severity = 2,
                        Message = $"All-in {allInPct:P0} of hands \u2014 too frequent. You're gambling, not playing poker. Reserve shoves for premium hands or bluffs with fold equity." });
            }

            // === MENTAL GAME ===

            if (_stats.CurrentStreak <= -4)
                _stats.Tips.Add(new CoachingTip { Category = "Mental", Severity = 2,
                    Message = $"{Mathf.Abs(_stats.CurrentStreak)}-hand losing streak. Variance is normal \u2014 take a breath and stick to your strategy. Chasing losses is the fastest way to tilt." });
            else if (_stats.CurrentStreak >= 5)
                _stats.Tips.Add(new CoachingTip { Category = "Mental", Severity = 0,
                    Message = $"{_stats.CurrentStreak}-hand heater! Stay disciplined \u2014 hot streaks tempt loose play. Keep making +EV decisions." });

            if (_stats.TotalProfit < 0 && _stats.StartingStack > 0 &&
                Mathf.Abs(_stats.TotalProfit) > _stats.StartingStack * 0.5f && h >= 5)
                _stats.Tips.Add(new CoachingTip { Category = "Strategy", Severity = 2,
                    Message = "Down over half your buy-in. Tighten up: play only premium hands, avoid marginal spots, and look for double-up opportunities." });

            // === NO CRITICAL ISSUES ===

            if (_stats.Tips.Count == 0)
            {
                if (_stats.TotalProfit > 0)
                    _stats.Tips.Add(new CoachingTip { Category = "General", Severity = 0,
                        Message = "Solid session! Focus on thin value bets and exploiting opponent tendencies to increase your edge further." });
                else
                    _stats.Tips.Add(new CoachingTip { Category = "General", Severity = 0,
                        Message = "Fundamentals look solid. Keep focusing on position, pot odds, and opponent patterns. Good decisions lead to good results." });
            }

            TrimTips(4);
        }

        private void TrimTips(int max)
        {
            _stats.Tips.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            if (_stats.Tips.Count > max) _stats.Tips.RemoveRange(max, _stats.Tips.Count - max);
        }

        private static string PositionName(PositionType pos)
        {
            return pos switch
            {
                PositionType.Button => "the Button",
                PositionType.SmallBlind => "the Small Blind",
                PositionType.BigBlind => "the Big Blind",
                PositionType.Cutoff => "the Cutoff",
                PositionType.EarlyPosition => "Early Position",
                PositionType.MiddlePosition => "Middle Position",
                _ => "your position",
            };
        }
    }
}
