using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class CardSpriteLoaderTests
    {
        [Test] public void SuitNames_Hearts() => Assert.AreEqual("Hearts", CardSpriteLoader.SuitNames["H"]);
        [Test] public void SuitNames_Diamonds() => Assert.AreEqual("Diamonds", CardSpriteLoader.SuitNames["D"]);
        [Test] public void SuitNames_Clubs() => Assert.AreEqual("Clubs", CardSpriteLoader.SuitNames["C"]);
        [Test] public void SuitNames_Spades() => Assert.AreEqual("Spades", CardSpriteLoader.SuitNames["S"]);
        [Test] public void SuitNames_HasFourEntries() => Assert.AreEqual(4, CardSpriteLoader.SuitNames.Count);

        [Test]
        public void LoadCardSprite_NullCode_ReturnsNull()
        {
            Assert.IsNull(CardSpriteLoader.LoadCardSprite(null));
        }

        [Test]
        public void LoadCardSprite_EmptyCode_ReturnsNull()
        {
            Assert.IsNull(CardSpriteLoader.LoadCardSprite(""));
        }
    }
}
