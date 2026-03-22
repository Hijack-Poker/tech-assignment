using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class InputValidationTests
    {
        [Test]
        public void MaxNameLength_TruncatesLongName()
        {
            string longName = new string('A', 30);
            string truncated = longName.Length > PokerConstants.MaxNameLength
                ? longName[..PokerConstants.MaxNameLength]
                : longName;
            Assert.AreEqual(20, truncated.Length);
        }

        [Test]
        public void MaxNameLength_ShortNameUnchanged()
        {
            string shortName = "Alice";
            string result = shortName.Length > PokerConstants.MaxNameLength
                ? shortName[..PokerConstants.MaxNameLength]
                : shortName;
            Assert.AreEqual("Alice", result);
        }

        [Test]
        public void MaxNameLength_ExactLengthUnchanged()
        {
            string exactName = new string('B', 20);
            string result = exactName.Length > PokerConstants.MaxNameLength
                ? exactName[..PokerConstants.MaxNameLength]
                : exactName;
            Assert.AreEqual(20, result.Length);
            Assert.AreEqual(exactName, result);
        }
    }
}
