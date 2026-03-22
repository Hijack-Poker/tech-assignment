using NUnit.Framework;
using HijackPoker.UI;

namespace HijackPoker.Tests
{
    public class TextureGeneratorTests
    {
        [TearDown]
        public void TearDown()
        {
            TextureGenerator.ClearCache();
        }

        [Test]
        public void GetRoundedRect_ReturnsSameSprite_ForIdenticalParams()
        {
            var a = TextureGenerator.GetRoundedRect(64, 64, 8);
            var b = TextureGenerator.GetRoundedRect(64, 64, 8);
            Assert.AreSame(a, b);
        }

        [Test]
        public void GetRoundedRect_ReturnsDifferentSprite_ForDifferentParams()
        {
            var a = TextureGenerator.GetRoundedRect(64, 64, 8);
            var b = TextureGenerator.GetRoundedRect(128, 64, 8);
            Assert.AreNotSame(a, b);
        }

        [Test]
        public void GetRoundedRect_HasCorrectDimensions()
        {
            var sprite = TextureGenerator.GetRoundedRect(100, 50, 10);
            Assert.AreEqual(100, sprite.texture.width);
            Assert.AreEqual(50, sprite.texture.height);
        }

        [Test]
        public void GetCircle_ReturnsSameSprite_ForIdenticalParams()
        {
            var a = TextureGenerator.GetCircle(56);
            var b = TextureGenerator.GetCircle(56);
            Assert.AreSame(a, b);
        }

        [Test]
        public void GetCircle_ReturnsDifferentSprite_ForDifferentDiameter()
        {
            var a = TextureGenerator.GetCircle(56);
            var b = TextureGenerator.GetCircle(62);
            Assert.AreNotSame(a, b);
        }

        [Test]
        public void GetCircle_HasCorrectDimensions()
        {
            var sprite = TextureGenerator.GetCircle(40);
            Assert.AreEqual(40, sprite.texture.width);
            Assert.AreEqual(40, sprite.texture.height);
        }

        [Test]
        public void ClearCache_ProducesFreshSprites()
        {
            var before = TextureGenerator.GetRoundedRect(32, 32, 4);
            TextureGenerator.ClearCache();
            var after = TextureGenerator.GetRoundedRect(32, 32, 4);
            Assert.AreNotSame(before, after);
        }
    }
}
