using System;
using System.Collections.Generic;
using HijackPoker.Models;

namespace HijackPoker.Utils
{
    public static class SeatResolver
    {
        public static int ResolveLocalSeat(List<PlayerState> players, string localName, int fallback = 1)
        {
            if (players == null || players.Count == 0) return fallback;
            if (!string.IsNullOrEmpty(localName))
            {
                foreach (var p in players)
                {
                    if (!string.IsNullOrEmpty(p.Username) &&
                        p.Username.Equals(localName, StringComparison.OrdinalIgnoreCase) &&
                        p.Seat > 0)
                        return p.Seat;
                }
            }
            return fallback;
        }
    }
}
