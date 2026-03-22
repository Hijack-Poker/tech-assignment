using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.AI
{
    // ── Data Models ───────────────────────────────────────────

    public enum PositionType { Button, SmallBlind, BigBlind, EarlyPosition, MiddlePosition, Cutoff }
    public enum HandOutcome { FoldWin, ShowdownWin, SplitPot }

    public struct ActionRecord
    {
        public int BettingRound; // 0=preflop, 1=flop, 2=turn, 3=river
        public string Action;
        public float Amount;
        public float PotAtTime;
    }

    public class PlayerHandRecord
    {
        public int Seat;
        public string Username;
        public float StartingStack;
        public float EndingStack;
        public float TotalInvested;
        public float Winnings;
        public bool IsWinner;
        public bool VoluntarilyPutMoneyIn;
        public bool RaisedPreflop;
        public string HandRank;
        public string FinalStatus; // "active", "folded", "allin"
        public PositionType Position;
        public List<ActionRecord> Actions = new();
    }

    public class HandSnapshot
    {
        public int GameNo;
        public int DealerSeat, SmallBlindSeat, BigBlindSeat;
        public float Pot, BigBlind;
        public List<string> CommunityCards = new();
        public List<PlayerHandRecord> PlayerRecords = new();
        public HandOutcome Outcome;
        public bool ReachedShowdown;
        public int BettingRoundsCompleted;
    }

    public class PlayerProfile
    {
        public int Seat;
        public string Username;
        public int HandsSeen;
        public int VpipCount, PfrCount;
        public int PostflopBets, PostflopCalls;
        public int FoldCount;
        public int WentToShowdownCount, WonAtShowdownCount;
        public int ConsecutiveLosses;
        public float PeakStack, CurrentStack;

        private readonly Queue<bool> _recentVpip = new();
        private readonly Queue<bool> _recentPfr = new();
        private const int WindowSize = 8;

        public float VPIP => HandsSeen > 0 ? (float)VpipCount / HandsSeen : 0f;
        public float PFR => HandsSeen > 0 ? (float)PfrCount / HandsSeen : 0f;
        public float AF => PostflopCalls > 0 ? (float)PostflopBets / PostflopCalls : PostflopBets > 0 ? 10f : 0f;
        public float FoldPct => HandsSeen > 0 ? (float)FoldCount / HandsSeen : 0f;
        public float WTSD => HandsSeen > 0 ? (float)WentToShowdownCount / HandsSeen : 0f;

        public float RecentVPIP
        {
            get
            {
                if (_recentVpip.Count == 0) return VPIP;
                int c = 0; foreach (var v in _recentVpip) if (v) c++;
                return (float)c / _recentVpip.Count;
            }
        }

        public float TiltScore
        {
            get
            {
                float s = 0f;
                s += Mathf.Min(ConsecutiveLosses * 0.15f, 0.45f);
                if (PeakStack > 0f) s += Mathf.Clamp(1f - CurrentStack / PeakStack, 0f, 1f) * 0.3f;
                if (HandsSeen >= 5 && RecentVPIP - VPIP > 0.2f) s += 0.15f;
                return Mathf.Clamp01(s);
            }
        }

        public void AddToWindow(bool vpip, bool pfr)
        {
            _recentVpip.Enqueue(vpip);
            _recentPfr.Enqueue(pfr);
            while (_recentVpip.Count > WindowSize) _recentVpip.Dequeue();
            while (_recentPfr.Count > WindowSize) _recentPfr.Dequeue();
        }

        public string GetPlayStyle()
        {
            if (HandsSeen < 5) return null;
            if (TiltScore > 0.6f) return "TILT";
            if (VPIP > 0.50f && AF > 3f) return "Maniac";
            if (VPIP < 0.15f && PFR < 0.10f) return "Nit";
            if (VPIP < 0.25f && (AF > 2f || PFR > 0.15f)) return "TAG";
            if (VPIP > 0.35f && (AF > 2f || PFR > 0.20f)) return "LAG";
            if (VPIP > 0.40f && AF < 1.5f) return "Fish";
            if (VPIP < 0.20f && AF < 1.2f) return "Rock";
            return "Reg";
        }

        public string GetPlayStyleDescription()
        {
            return GetPlayStyle() switch
            {
                "TAG" => "Tight-Aggressive",
                "LAG" => "Loose-Aggressive",
                "Fish" => "Loose-Passive",
                "Nit" => "Ultra-Tight",
                "Rock" => "Tight-Passive",
                "Maniac" => "Hyper-Aggressive",
                "TILT" => "On Tilt",
                "Reg" => "Regular",
                _ => null,
            };
        }
    }

    // ── Collector ─────────────────────────────────────────────

    public class HandDataCollector : MonoBehaviour
    {
        public List<HandSnapshot> History { get; } = new();
        public Dictionary<int, PlayerProfile> Profiles { get; } = new();
        public event Action<HandSnapshot> OnHandCompleted;

        private TableStateManager _stateManager;
        private int _currentGameNo = -1;
        private int _completedGameNo = -1;
        private int _currentDealerSeat, _currentSBSeat, _currentBBSeat;
        private float _currentBigBlind;
        private readonly Dictionary<int, float> _startingStacks = new();
        private readonly Dictionary<int, List<ActionRecord>> _playerActions = new();
        private readonly Dictionary<int, string> _lastActionKeys = new();
        private readonly HashSet<int> _vpipPlayers = new();
        private readonly HashSet<int> _pfrPlayers = new();
        private int _highestBettingRound = -1;

        private void Awake() => _stateManager = FindObjectOfType<TableStateManager>();

        private void OnEnable()
        {
            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged += OnStateChanged;
                _stateManager.OnTableReset += OnTableReset;
            }
        }

        private void OnDisable()
        {
            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged -= OnStateChanged;
                _stateManager.OnTableReset -= OnTableReset;
            }
        }

        private void OnTableReset()
        {
            _currentGameNo = -1;
            _completedGameNo = -1;
            Profiles.Clear();
            History.Clear();
        }

        private void OnStateChanged(TableResponse state)
        {
            if (state?.Game == null || state.Players == null) return;

            int gameNo = state.Game.GameNo;
            int step = state.Game.HandStep;

            // New hand detected
            if (gameNo != _currentGameNo)
            {
                _currentGameNo = gameNo;
                BeginHandTracking(state);
            }

            // Capture actions during betting rounds
            if (PokerConstants.IsBettingStep(step))
                CaptureActions(state, step);

            // Update stacks for profiles
            foreach (var p in state.Players)
            {
                if (Profiles.TryGetValue(p.Seat, out var prof))
                {
                    prof.CurrentStack = p.Stack;
                    float total = p.Stack + p.TotalBet;
                    if (total > prof.PeakStack) prof.PeakStack = total;
                }
            }

            // Hand complete — wait for winners to be populated
            if (step >= 15 && gameNo != _completedGameNo)
            {
                var winners = state.Players.Where(p => p.IsWinner && p.Winnings > 0).ToList();
                if (winners.Count > 0 || state.Players.All(p => p.IsFolded || p.Winnings > 0))
                {
                    _completedGameNo = gameNo;
                    FinalizeHand(state);
                }
            }
        }

        private void BeginHandTracking(TableResponse state)
        {
            _startingStacks.Clear();
            _playerActions.Clear();
            _lastActionKeys.Clear();
            _vpipPlayers.Clear();
            _pfrPlayers.Clear();
            _highestBettingRound = -1;

            _currentDealerSeat = state.Game.DealerSeat;
            _currentSBSeat = state.Game.SmallBlindSeat;
            _currentBBSeat = state.Game.BigBlindSeat;
            _currentBigBlind = state.Game.BigBlind;

            foreach (var p in state.Players)
            {
                _startingStacks[p.Seat] = p.Stack + p.TotalBet;
                _playerActions[p.Seat] = new List<ActionRecord>();

                if (!Profiles.ContainsKey(p.Seat))
                    Profiles[p.Seat] = new PlayerProfile
                    {
                        Seat = p.Seat,
                        Username = p.Username,
                        PeakStack = p.Stack + p.TotalBet,
                        CurrentStack = p.Stack,
                    };
            }
        }

        private void CaptureActions(TableResponse state, int step)
        {
            int round = step switch { 5 => 0, 7 => 1, 9 => 2, 11 => 3, _ => -1 };
            if (round < 0) return;
            if (round > _highestBettingRound) _highestBettingRound = round;

            foreach (var p in state.Players)
            {
                if (string.IsNullOrEmpty(p.Action)) continue;

                string key = $"{p.Action}_{p.Bet}_{p.TotalBet}_{step}";
                if (_lastActionKeys.TryGetValue(p.Seat, out var prev) && prev == key) continue;
                _lastActionKeys[p.Seat] = key;

                string act = p.Action.ToLower();
                if (!_playerActions.ContainsKey(p.Seat))
                    _playerActions[p.Seat] = new List<ActionRecord>();

                _playerActions[p.Seat].Add(new ActionRecord
                {
                    BettingRound = round,
                    Action = act,
                    Amount = p.TotalBet,
                    PotAtTime = state.Game.Pot,
                });

                if (round == 0 && (act == "call" || act == "raise" || act == "bet" || act == "allin"))
                    _vpipPlayers.Add(p.Seat);
                if (round == 0 && (act == "raise" || act == "bet" || act == "allin"))
                    _pfrPlayers.Add(p.Seat);
            }
        }

        private void FinalizeHand(TableResponse state)
        {
            var occupied = state.Players.Select(p => p.Seat).Where(s => s > 0).OrderBy(s => s).ToList();
            int showdownCount = state.Players.Count(p => !p.IsFolded);
            var winners = state.Players.Where(p => p.IsWinner && p.Winnings > 0).ToList();

            var snapshot = new HandSnapshot
            {
                GameNo = _currentGameNo,
                DealerSeat = _currentDealerSeat,
                SmallBlindSeat = _currentSBSeat,
                BigBlindSeat = _currentBBSeat,
                Pot = state.Game.Pot,
                BigBlind = _currentBigBlind > 0 ? _currentBigBlind : 2f,
                CommunityCards = state.Game.CommunityCards != null ? new List<string>(state.Game.CommunityCards) : new(),
                ReachedShowdown = showdownCount >= 2 && state.Game.HandStep >= 12,
                BettingRoundsCompleted = _highestBettingRound + 1,
                Outcome = showdownCount < 2 ? HandOutcome.FoldWin
                    : winners.Count > 1 ? HandOutcome.SplitPot
                    : HandOutcome.ShowdownWin,
            };

            foreach (var p in state.Players)
            {
                _startingStacks.TryGetValue(p.Seat, out float startStack);
                var record = new PlayerHandRecord
                {
                    Seat = p.Seat,
                    Username = p.Username,
                    StartingStack = startStack,
                    EndingStack = p.Stack,
                    TotalInvested = Mathf.Max(0f, startStack - p.Stack + p.Winnings),
                    Winnings = p.Winnings,
                    IsWinner = p.IsWinner && p.Winnings > 0,
                    VoluntarilyPutMoneyIn = _vpipPlayers.Contains(p.Seat),
                    RaisedPreflop = _pfrPlayers.Contains(p.Seat),
                    HandRank = !string.IsNullOrEmpty(p.HandRank) ? p.HandRank : null,
                    FinalStatus = p.IsFolded ? "folded" : p.IsAllIn ? "allin" : "active",
                    Position = GetPosition(p.Seat, _currentDealerSeat, _currentSBSeat, _currentBBSeat, occupied),
                    Actions = _playerActions.TryGetValue(p.Seat, out var acts) ? new List<ActionRecord>(acts) : new(),
                };
                snapshot.PlayerRecords.Add(record);
            }

            // Update profiles
            foreach (var rec in snapshot.PlayerRecords)
                UpdateProfile(rec, snapshot);

            History.Add(snapshot);
            if (History.Count > 100) History.RemoveAt(0);

            OnHandCompleted?.Invoke(snapshot);
        }

        private void UpdateProfile(PlayerHandRecord rec, HandSnapshot snap)
        {
            if (!Profiles.TryGetValue(rec.Seat, out var p))
            {
                p = new PlayerProfile { Seat = rec.Seat, Username = rec.Username };
                Profiles[rec.Seat] = p;
            }

            p.Username = rec.Username;
            p.HandsSeen++;
            if (rec.VoluntarilyPutMoneyIn) p.VpipCount++;
            if (rec.RaisedPreflop) p.PfrCount++;
            if (rec.FinalStatus == "folded") p.FoldCount++;

            foreach (var a in rec.Actions)
            {
                if (a.BettingRound == 0) continue;
                if (a.Action == "bet" || a.Action == "raise" || a.Action == "allin") p.PostflopBets++;
                else if (a.Action == "call") p.PostflopCalls++;
            }

            if (snap.ReachedShowdown && rec.FinalStatus != "folded")
            {
                p.WentToShowdownCount++;
                if (rec.IsWinner) p.WonAtShowdownCount++;
            }

            p.CurrentStack = rec.EndingStack;
            float peak = rec.StartingStack;
            if (peak > p.PeakStack) p.PeakStack = peak;

            if (rec.IsWinner) p.ConsecutiveLosses = 0;
            else if (rec.FinalStatus != "folded") p.ConsecutiveLosses++;

            p.AddToWindow(rec.VoluntarilyPutMoneyIn, rec.RaisedPreflop);
        }

        private static PositionType GetPosition(int seat, int dealer, int sb, int bb, List<int> occupied)
        {
            if (seat == dealer) return PositionType.Button;
            if (seat == sb) return PositionType.SmallBlind;
            if (seat == bb) return PositionType.BigBlind;

            int bbIdx = occupied.IndexOf(bb);
            int btnIdx = occupied.IndexOf(dealer);
            if (bbIdx < 0 || btnIdx < 0) return PositionType.MiddlePosition;

            var afterBB = new List<int>();
            for (int i = 1; i < occupied.Count; i++)
            {
                int idx = (bbIdx + i) % occupied.Count;
                if (occupied[idx] == dealer) break;
                afterBB.Add(occupied[idx]);
            }

            int posIdx = afterBB.IndexOf(seat);
            if (posIdx < 0) return PositionType.MiddlePosition;
            if (afterBB.Count <= 1) return PositionType.Cutoff;
            if (posIdx == afterBB.Count - 1) return PositionType.Cutoff;
            if (posIdx == 0) return PositionType.EarlyPosition;
            return PositionType.MiddlePosition;
        }
    }
}
