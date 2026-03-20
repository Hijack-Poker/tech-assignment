using System.Collections.Generic;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Tracks active tween handles and supports bulk cancellation with snap-to-final.
    /// </summary>
    public class AnimationController
    {
        private readonly List<TweenHandle> _activeHandles = new List<TweenHandle>();

        public bool IsPlaying
        {
            get
            {
                CleanUp();
                return _activeHandles.Count > 0;
            }
        }

        public TweenHandle Play(TweenHandle handle)
        {
            _activeHandles.Add(handle);
            return handle;
        }

        /// <summary>
        /// Convenience: build and play a Timeline, returning the master handle.
        /// </summary>
        public TweenHandle PlayTimeline(Timeline timeline, System.Action snapToFinal = null)
        {
            return timeline.Play(this, snapToFinal);
        }

        public void CancelAll()
        {
            // Copy to avoid mutation during iteration (OnComplete may add new handles)
            var snapshot = new List<TweenHandle>(_activeHandles);
            _activeHandles.Clear();

            foreach (var handle in snapshot)
            {
                if (!handle.IsComplete)
                    handle.Cancel();
            }
        }

        private void CleanUp()
        {
            _activeHandles.RemoveAll(h => h.IsComplete);
        }
    }
}
