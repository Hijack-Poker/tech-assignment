using NUnit.Framework;
using HijackPoker.Animation;

namespace HijackPoker.Tests.EditMode
{
    [TestFixture]
    public class AnimationControllerTests
    {
        private AnimationController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new AnimationController();
        }

        [Test]
        public void IsPlaying_NoHandles_ReturnsFalse()
        {
            Assert.IsFalse(_controller.IsPlaying);
        }

        [Test]
        public void IsPlaying_WithActiveHandle_ReturnsTrue()
        {
            var handle = new TweenHandle();
            _controller.Play(handle);
            Assert.IsTrue(_controller.IsPlaying);
        }

        [Test]
        public void IsPlaying_AllComplete_ReturnsFalse()
        {
            var handle = new TweenHandle();
            _controller.Play(handle);
            handle.Cancel();
            Assert.IsFalse(_controller.IsPlaying);
        }

        [Test]
        public void Play_ReturnsTheSameHandle()
        {
            var handle = new TweenHandle();
            var returned = _controller.Play(handle);
            Assert.AreSame(handle, returned);
        }

        [Test]
        public void CancelAll_CancelsAllActiveHandles()
        {
            var h1 = new TweenHandle();
            var h2 = new TweenHandle();
            var h3 = new TweenHandle();
            _controller.Play(h1);
            _controller.Play(h2);
            _controller.Play(h3);

            _controller.CancelAll();

            Assert.IsTrue(h1.IsComplete);
            Assert.IsTrue(h2.IsComplete);
            Assert.IsTrue(h3.IsComplete);
            Assert.IsFalse(_controller.IsPlaying);
        }

        [Test]
        public void CancelAll_InvokesSnapToFinal()
        {
            bool snapped = false;
            var handle = new TweenHandle { SnapToFinal = () => snapped = true };
            _controller.Play(handle);

            _controller.CancelAll();

            Assert.IsTrue(snapped);
        }

        [Test]
        public void CancelAll_SkipsAlreadyCompleteHandles()
        {
            int snapCount = 0;
            var handle = new TweenHandle { SnapToFinal = () => snapCount++ };
            _controller.Play(handle);
            handle.Cancel(); // completes it

            _controller.CancelAll(); // should not snap again

            Assert.AreEqual(1, snapCount);
        }

        [Test]
        public void CancelAll_HandlesEmptyList()
        {
            Assert.DoesNotThrow(() => _controller.CancelAll());
        }

        [Test]
        public void CancelAll_MultipleCallsSafe()
        {
            var handle = new TweenHandle { SnapToFinal = () => { } };
            _controller.Play(handle);

            _controller.CancelAll();
            Assert.DoesNotThrow(() => _controller.CancelAll());
        }
    }
}
