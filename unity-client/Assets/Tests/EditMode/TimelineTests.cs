using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Animation;

namespace HijackPoker.Tests.EditMode
{
    [TestFixture]
    public class TimelineTests
    {
        private AnimationController _anim;

        [SetUp]
        public void SetUp()
        {
            _anim = new AnimationController();
        }

        [Test]
        public void EmptyTimeline_CompletesImmediately()
        {
            var handle = new Timeline().Play(_anim);
            Assert.IsTrue(handle.IsComplete);
        }

        [Test]
        public void AppendCallback_Executes()
        {
            bool called = false;
            new Timeline()
                .AppendCallback(() => called = true)
                .Play(_anim);
            Assert.IsTrue(called);
        }

        [Test]
        public void SequentialCallbacks_ExecuteInOrder()
        {
            var order = new List<int>();
            new Timeline()
                .AppendCallback(() => order.Add(1))
                .AppendCallback(() => order.Add(2))
                .AppendCallback(() => order.Add(3))
                .Play(_anim);
            Assert.AreEqual(new List<int> { 1, 2, 3 }, order);
        }

        [Test]
        public void Append_PendingTween_BlocksAdvancement()
        {
            bool called = false;
            new Timeline()
                .Append(() => new TweenHandle()) // never completes
                .AppendCallback(() => called = true)
                .Play(_anim);
            Assert.IsFalse(called);
        }

        [Test]
        public void Append_CompletedTween_Advances()
        {
            bool called = false;
            var handle = new TweenHandle();
            new Timeline()
                .Append(() => handle)
                .AppendCallback(() => called = true)
                .Play(_anim);

            Assert.IsFalse(called);
            handle.MarkComplete();
            Assert.IsTrue(called);
        }

        [Test]
        public void Join_WaitsForAllParallelSteps()
        {
            bool advanced = false;
            var h1 = new TweenHandle();
            var h2 = new TweenHandle();
            new Timeline()
                .Append(() => h1)
                .Join(() => h2)
                .AppendCallback(() => advanced = true)
                .Play(_anim);

            Assert.IsFalse(advanced);
            h1.MarkComplete();
            Assert.IsFalse(advanced); // still waiting for h2
            h2.MarkComplete();
            Assert.IsTrue(advanced);
        }

        [Test]
        public void Cancel_PropagatesTo_ActiveHandles()
        {
            bool childSnapped = false;
            var child = new TweenHandle { SnapToFinal = () => childSnapped = true };
            var master = new Timeline()
                .Append(() => child)
                .Play(_anim);
            master.Cancel();
            Assert.IsTrue(childSnapped);
        }

        [Test]
        public void Cancel_InvokesCustomSnapToFinal()
        {
            bool snapped = false;
            var master = new Timeline()
                .Append(() => new TweenHandle())
                .Play(_anim, () => snapped = true);
            master.Cancel();
            Assert.IsTrue(snapped);
        }

        [Test]
        public void Cancel_DoesNotExecuteRemainingCallbacks()
        {
            bool callbackReached = false;
            var master = new Timeline()
                .Append(() => new TweenHandle()) // blocks
                .AppendCallback(() => callbackReached = true)
                .Play(_anim);
            master.Cancel();
            Assert.IsFalse(callbackReached);
        }

        [Test]
        public void AppendInterval_Zero_IsSkipped()
        {
            bool called = false;
            new Timeline()
                .AppendInterval(0f)
                .AppendCallback(() => called = true)
                .Play(_anim);
            Assert.IsTrue(called);
        }

        [Test]
        public void MasterHandle_IsComplete_AfterAllSteps()
        {
            var h = new TweenHandle();
            var master = new Timeline()
                .AppendCallback(() => { })
                .Append(() => h)
                .AppendCallback(() => { })
                .Play(_anim);

            Assert.IsFalse(master.IsComplete);
            h.MarkComplete();
            Assert.IsTrue(master.IsComplete);
        }

        [Test]
        public void CallbackBefore_TweenInSameGroup_ExecutesFirst()
        {
            // AppendCallback creates its own group, then Append creates the next group.
            // The callback group finishes first, then the tween group starts.
            var order = new List<string>();
            var h = new TweenHandle();
            new Timeline()
                .AppendCallback(() => order.Add("callback"))
                .Append(() => { order.Add("factory"); return h; })
                .Play(_anim);

            Assert.AreEqual(new List<string> { "callback", "factory" }, order);
        }

        [Test]
        public void CancelAll_OnController_CancelsMasterAndChildren()
        {
            bool childSnapped = false;
            bool masterSnapped = false;
            var child = new TweenHandle { SnapToFinal = () => childSnapped = true };
            new Timeline()
                .Append(() => child)
                .Play(_anim, () => masterSnapped = true);

            _anim.CancelAll();
            Assert.IsTrue(childSnapped);
            Assert.IsTrue(masterSnapped);
        }

        [Test]
        public void MultipleJoins_AllMustComplete()
        {
            bool advanced = false;
            var h1 = new TweenHandle();
            var h2 = new TweenHandle();
            var h3 = new TweenHandle();
            new Timeline()
                .Append(() => h1)
                .Join(() => h2)
                .Join(() => h3)
                .AppendCallback(() => advanced = true)
                .Play(_anim);

            h1.MarkComplete();
            h2.MarkComplete();
            Assert.IsFalse(advanced);
            h3.MarkComplete();
            Assert.IsTrue(advanced);
        }
    }
}
