using NUnit.Framework;
using HijackPoker.Models;
using HijackPoker.UI;

namespace HijackPoker.Tests
{
    public class HandHistoryFormatTests
    {
        private PlayerState MakePlayer(string action, float bet = 0f, float totalBet = 0f)
        {
            return new PlayerState
            {
                Username = "TestPlayer",
                Seat = 1,
                Action = action,
                Bet = bet,
                TotalBet = totalBet,
                Status = "1"
            };
        }

        [Test]
        public void FormatAction_Fold()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("fold"));
            Assert.IsTrue(result.Contains("folds"));
        }

        [Test]
        public void FormatAction_Check()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("check"));
            Assert.IsTrue(result.Contains("checks"));
        }

        [Test]
        public void FormatAction_Call()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("call", bet: 10f));
            Assert.IsTrue(result.Contains("calls"));
            Assert.IsTrue(result.Contains("$10.00"));
        }

        [Test]
        public void FormatAction_Bet()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("bet", bet: 20f));
            Assert.IsTrue(result.Contains("bets"));
            Assert.IsTrue(result.Contains("$20.00"));
        }

        [Test]
        public void FormatAction_Raise()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("raise", totalBet: 40f));
            Assert.IsTrue(result.Contains("raises to"));
            Assert.IsTrue(result.Contains("$40.00"));
        }

        [Test]
        public void FormatAction_AllIn()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("allin", totalBet: 100f));
            Assert.IsTrue(result.Contains("ALL-IN"));
            Assert.IsTrue(result.Contains("$100.00"));
        }

        [Test]
        public void FormatAction_Unknown_ReturnsNull()
        {
            var result = HandHistoryView.FormatAction(MakePlayer("unknown"));
            Assert.IsNull(result);
        }

        [Test]
        public void FormatAction_Null_ReturnsNull()
        {
            var result = HandHistoryView.FormatAction(MakePlayer(null));
            Assert.IsNull(result);
        }
    }
}
