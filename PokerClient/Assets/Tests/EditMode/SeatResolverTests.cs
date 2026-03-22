using NUnit.Framework;
using System.Collections.Generic;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class SeatResolverTests
    {
        [Test]
        public void EmptyList_ReturnsFallback()
        {
            int seat = SeatResolver.ResolveLocalSeat(new List<PlayerState>(), "Alice");
            Assert.AreEqual(1, seat);
        }

        [Test]
        public void NullList_ReturnsFallback()
        {
            int seat = SeatResolver.ResolveLocalSeat(null, "Alice");
            Assert.AreEqual(1, seat);
        }

        [Test]
        public void NullName_ReturnsFallback()
        {
            var players = new List<PlayerState>
            {
                new PlayerState { Username = "Alice", Seat = 3 }
            };
            int seat = SeatResolver.ResolveLocalSeat(players, null);
            Assert.AreEqual(1, seat);
        }

        [Test]
        public void ExactMatch_ReturnsSeat()
        {
            var players = new List<PlayerState>
            {
                new PlayerState { Username = "Bob", Seat = 2 },
                new PlayerState { Username = "Alice", Seat = 4 }
            };
            int seat = SeatResolver.ResolveLocalSeat(players, "Alice");
            Assert.AreEqual(4, seat);
        }

        [Test]
        public void CaseInsensitiveMatch_ReturnsSeat()
        {
            var players = new List<PlayerState>
            {
                new PlayerState { Username = "ALICE", Seat = 5 }
            };
            int seat = SeatResolver.ResolveLocalSeat(players, "alice");
            Assert.AreEqual(5, seat);
        }

        [Test]
        public void NoMatch_ReturnsFallback()
        {
            var players = new List<PlayerState>
            {
                new PlayerState { Username = "Bob", Seat = 2 }
            };
            int seat = SeatResolver.ResolveLocalSeat(players, "Charlie");
            Assert.AreEqual(1, seat);
        }

        [Test]
        public void CustomFallback_Returned()
        {
            int seat = SeatResolver.ResolveLocalSeat(null, "Alice", fallback: 3);
            Assert.AreEqual(3, seat);
        }
    }
}
