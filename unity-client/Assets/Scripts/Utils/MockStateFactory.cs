using System.Collections.Generic;
using HijackPoker.Models;

namespace HijackPoker.Utils
{
    /// <summary>
    /// Creates mock table state for offline/demo mode.
    /// </summary>
    public static class MockStateFactory
    {
        public static TableResponse CreateMockState()
        {
            return new TableResponse
            {
                Game = new GameState
                {
                    Id = 1,
                    TableId = 1,
                    TableName = "Table 1",
                    GameNo = 1,
                    HandStep = 7,
                    StepName = "FLOP_BETTING_ROUND",
                    DealerSeat = 1,
                    SmallBlindSeat = 2,
                    BigBlindSeat = 3,
                    CommunityCards = new List<string> { "AH", "7D", "2C" },
                    Pot = 24.00f,
                    SidePots = new List<SidePot>(),
                    Move = 4,
                    Status = "in_progress",
                    SmallBlind = 1.00f,
                    BigBlind = 2.00f,
                    MaxSeats = 6,
                    CurrentBet = 4.00f,
                    Winners = new List<Winner>()
                },
                Players = new List<PlayerState>
                {
                    new PlayerState
                    {
                        PlayerId = 101, Username = "Alice", Seat = 1,
                        Stack = 196.00f, Bet = 4.00f, TotalBet = 6.00f,
                        Status = "1", Action = "call",
                        Cards = new List<string> { "KS", "QH" },
                        HandRank = "", Winnings = 0
                    },
                    new PlayerState
                    {
                        PlayerId = 102, Username = "Bob", Seat = 2,
                        Stack = 147.00f, Bet = 0f, TotalBet = 1.00f,
                        Status = "11", Action = "fold",
                        Cards = new List<string> { "3D", "5C" },
                        HandRank = "", Winnings = 0
                    },
                    new PlayerState
                    {
                        PlayerId = 103, Username = "Charlie", Seat = 3,
                        Stack = 98.00f, Bet = 4.00f, TotalBet = 6.00f,
                        Status = "1", Action = "raise",
                        Cards = new List<string> { "AH", "KD" },
                        HandRank = "", Winnings = 0
                    },
                    new PlayerState
                    {
                        PlayerId = 104, Username = "Diana", Seat = 4,
                        Stack = 200.00f, Bet = 0f, TotalBet = 0f,
                        Status = "1", Action = "",
                        Cards = new List<string> { "JH", "10S" },
                        HandRank = "", Winnings = 0
                    },
                    new PlayerState
                    {
                        PlayerId = 105, Username = "Eve", Seat = 5,
                        Stack = 0f, Bet = 4.00f, TotalBet = 6.00f,
                        Status = "12", Action = "allin",
                        Cards = new List<string> { "9H", "9D" },
                        HandRank = "", Winnings = 0
                    },
                    new PlayerState
                    {
                        PlayerId = 106, Username = "Frank", Seat = 6,
                        Stack = 246.00f, Bet = 4.00f, TotalBet = 6.00f,
                        Status = "1", Action = "call",
                        Cards = new List<string> { "8C", "8S" },
                        HandRank = "", Winnings = 0
                    }
                }
            };
        }
    }
}
