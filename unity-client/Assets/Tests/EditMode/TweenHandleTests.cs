using NUnit.Framework;
using HijackPoker.Animation;

namespace HijackPoker.Tests.EditMode
{
    [TestFixture]
    public class TweenHandleTests
    {
        [Test]
        public void NewHandle_IsNotComplete()
        {
            var handle = new TweenHandle();
            Assert.IsFalse(handle.IsComplete);
        }

        [Test]
        public void Cancel_SetsIsComplete()
        {
            var handle = new TweenHandle();
            handle.Cancel();
            Assert.IsTrue(handle.IsComplete);
        }

        [Test]
        public void Cancel_InvokesSnapToFinal()
        {
            float result = 0f;
            var handle = new TweenHandle { SnapToFinal = () => result = 42f };
            handle.Cancel();
            Assert.AreEqual(42f, result);
        }

        [Test]
        public void Cancel_DoesNotInvokeOnComplete()
        {
            bool onCompleteCalled = false;
            var handle = new TweenHandle { SnapToFinal = () => { } };
            handle.OnComplete(() => onCompleteCalled = true);
            handle.Cancel();
            Assert.IsFalse(onCompleteCalled);
        }

        [Test]
        public void Cancel_SecondCallIsNoOp()
        {
            int snapCount = 0;
            var handle = new TweenHandle { SnapToFinal = () => snapCount++ };
            handle.Cancel();
            handle.Cancel();
            Assert.AreEqual(1, snapCount);
        }

        [Test]
        public void MarkComplete_SetsIsComplete()
        {
            var handle = new TweenHandle();
            handle.MarkComplete();
            Assert.IsTrue(handle.IsComplete);
        }

        [Test]
        public void MarkComplete_InvokesOnComplete()
        {
            bool called = false;
            var handle = new TweenHandle();
            handle.OnComplete(() => called = true);
            handle.MarkComplete();
            Assert.IsTrue(called);
        }

        [Test]
        public void MarkComplete_DoesNotInvokeSnapToFinal()
        {
            bool snapped = false;
            var handle = new TweenHandle { SnapToFinal = () => snapped = true };
            handle.MarkComplete();
            Assert.IsFalse(snapped);
        }

        [Test]
        public void MarkComplete_SecondCallIsNoOp()
        {
            int callCount = 0;
            var handle = new TweenHandle();
            handle.OnComplete(() => callCount++);
            handle.MarkComplete();
            handle.MarkComplete();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Cancel_AfterMarkComplete_IsNoOp()
        {
            bool snapped = false;
            var handle = new TweenHandle { SnapToFinal = () => snapped = true };
            handle.MarkComplete();
            handle.Cancel();
            Assert.IsFalse(snapped);
        }

        [Test]
        public void OnComplete_ReturnsHandle_ForChaining()
        {
            var handle = new TweenHandle();
            var returned = handle.OnComplete(() => { });
            Assert.AreSame(handle, returned);
        }

        [Test]
        public void SnapToFinal_CanBeOverridden()
        {
            float value = 0f;
            var handle = new TweenHandle { SnapToFinal = () => value = 1f };
            handle.SnapToFinal = () => value = 99f;
            handle.Cancel();
            Assert.AreEqual(99f, value);
        }

        [Test]
        public void Cancel_WithNullSnapToFinal_DoesNotThrow()
        {
            var handle = new TweenHandle();
            Assert.DoesNotThrow(() => handle.Cancel());
        }

        [Test]
        public void MarkComplete_WithNullOnComplete_DoesNotThrow()
        {
            var handle = new TweenHandle();
            Assert.DoesNotThrow(() => handle.MarkComplete());
        }
    }
}
