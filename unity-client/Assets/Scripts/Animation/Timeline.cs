using System;
using System.Collections.Generic;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Composable animation sequence builder with fluent API.
    /// Replaces deeply-nested OnComplete callback chains with flat, readable sequences.
    ///
    /// Usage:
    ///   new Timeline()
    ///     .AppendInterval(delay)
    ///     .AppendCallback(() => Setup())
    ///     .Append(() => Tweener.TweenFloat(...))
    ///     .Join(() => Tweener.ScalePop(...))
    ///     .AppendCallback(() => OnComplete())
    ///     .Play(anim, snapCleanup);
    /// </summary>
    public class Timeline
    {
        private interface IStep { }

        private class TweenStep : IStep
        {
            public readonly Func<TweenHandle> Factory;
            public TweenStep(Func<TweenHandle> factory) => Factory = factory;
        }

        private class CallbackStep : IStep
        {
            public readonly Action Callback;
            public CallbackStep(Action callback) => Callback = callback;
        }

        private class IntervalStep : IStep
        {
            public readonly float Duration;
            public IntervalStep(float duration) => Duration = duration;
        }

        private class StepGroup
        {
            public readonly List<IStep> Steps = new();
            public StepGroup() { }
            public StepGroup(IStep step) => Steps.Add(step);
        }

        private readonly List<StepGroup> _groups = new();

        /// <summary>Run a tween after the previous step completes.</summary>
        public Timeline Append(Func<TweenHandle> factory)
        {
            _groups.Add(new StepGroup(new TweenStep(factory)));
            return this;
        }

        /// <summary>Run a tween in parallel with the previous step.</summary>
        public Timeline Join(Func<TweenHandle> factory)
        {
            if (_groups.Count == 0)
                _groups.Add(new StepGroup());
            _groups[_groups.Count - 1].Steps.Add(new TweenStep(factory));
            return this;
        }

        /// <summary>Execute a zero-duration callback after the previous step completes.</summary>
        public Timeline AppendCallback(Action callback)
        {
            _groups.Add(new StepGroup(new CallbackStep(callback)));
            return this;
        }

        /// <summary>Wait for a duration after the previous step completes. Skipped if &lt;= 0.</summary>
        public Timeline AppendInterval(float duration)
        {
            if (duration > 0f)
                _groups.Add(new StepGroup(new IntervalStep(duration)));
            return this;
        }

        /// <summary>
        /// Execute the timeline and return a single master TweenHandle.
        /// The master handle's Cancel() propagates SnapToFinal to all active steps.
        /// </summary>
        /// <param name="anim">AnimationController to track all handles.</param>
        /// <param name="snapToFinal">Optional cleanup to run when the master handle is cancelled.</param>
        public TweenHandle Play(AnimationController anim, Action snapToFinal = null)
        {
            var master = new TweenHandle();

            if (_groups.Count == 0)
            {
                master.MarkComplete();
                return master;
            }

            var activeHandles = new List<TweenHandle>();
            bool cancelled = false;

            void StartGroup(int idx)
            {
                if (cancelled) return;

                if (idx >= _groups.Count)
                {
                    master.MarkComplete();
                    return;
                }

                var group = _groups[idx];
                int pending = 0;

                // Count async steps first to avoid premature advancement
                foreach (var step in group.Steps)
                    if (step is not CallbackStep)
                        pending++;

                // Execute all steps in the group
                foreach (var step in group.Steps)
                {
                    if (step is CallbackStep cb)
                    {
                        cb.Callback?.Invoke();
                    }
                    else
                    {
                        TweenHandle h;
                        if (step is TweenStep ts)
                            h = ts.Factory();
                        else
                            h = Tweener.Delay(((IntervalStep)step).Duration);

                        anim.Play(h);
                        activeHandles.Add(h);
                        h.OnComplete(() =>
                        {
                            if (cancelled) return;
                            if (--pending <= 0)
                                StartGroup(idx + 1);
                        });
                    }
                }

                // If group had only callbacks (no async steps), advance immediately
                if (pending <= 0)
                    StartGroup(idx + 1);
            }

            master.SnapToFinal = () =>
            {
                cancelled = true;
                foreach (var h in activeHandles)
                    if (!h.IsComplete) h.Cancel();
                snapToFinal?.Invoke();
            };

            StartGroup(0);
            return anim.Play(master);
        }
    }
}
