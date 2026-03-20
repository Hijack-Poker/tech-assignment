using System.Collections.Generic;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    /// <summary>
    /// Tracks cumulative player statistics across hands within a session.
    /// Pure C# class — no MonoBehaviour dependency.
    /// </summary>
    public class SessionTracker
    {
        public class PlayerSession
        {
            public float InitialStack;
            public float CurrentStack;
            public int HandsPlayed;
            public int HandsWon;
            public float BiggestPot;
        }

        private readonly Dictionary<int, PlayerSession> _sessions = new();
        private int _lastGameNo = -1;

        public void RecordHandEnd(TableResponse state)
        {
            if (state?.Game == null || state.Players == null) return;

            int gameNo = state.Game.GameNo;
            if (gameNo == _lastGameNo) return;
            _lastGameNo = gameNo;

            foreach (var player in state.Players)
            {
                if (player.Seat < 1) continue;

                if (!_sessions.TryGetValue(player.Seat, out var session))
                {
                    session = new PlayerSession
                    {
                        InitialStack = player.Stack,
                        CurrentStack = player.Stack,
                        HandsPlayed = 0,
                        HandsWon = 0,
                        BiggestPot = 0
                    };
                    _sessions[player.Seat] = session;
                }

                session.CurrentStack = player.Stack;
                session.HandsPlayed++;

                if (player.IsWinner)
                {
                    session.HandsWon++;
                    if (player.Winnings > session.BiggestPot)
                        session.BiggestPot = player.Winnings;
                }
            }
        }

        public float GetDelta(int seat)
        {
            if (!_sessions.TryGetValue(seat, out var session))
                return 0f;
            return session.CurrentStack - session.InitialStack;
        }

        public PlayerSession GetSession(int seat)
        {
            _sessions.TryGetValue(seat, out var session);
            return session;
        }

        public void Reset()
        {
            _sessions.Clear();
            _lastGameNo = -1;
        }
    }
}
