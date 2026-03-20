using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace HijackPoker.Managers
{
    /// <summary>
    /// Manages auto-play state and loop. Extracted from GameManager.
    /// Uses a delegate callback for step processing.
    /// </summary>
    public class AutoPlayManager : MonoBehaviour
    {
        private bool _autoPlaying;
        private bool _stopRequested;
        private Coroutine _loopCoroutine;

        public bool IsActive => _autoPlaying;

        public event Action<bool> OnActiveChanged;

        /// <summary>
        /// Delegate that processes a single step. Returns a Task.
        /// </summary>
        public Func<Task> ProcessStepAsync { get; set; }

        /// <summary>
        /// Delegate that returns the current speed (delay in seconds).
        /// </summary>
        public Func<float> GetCurrentSpeed { get; set; }

        public void Toggle()
        {
            if (_autoPlaying)
                Stop();
            else
                Start();
        }

        public void Start()
        {
            if (_autoPlaying) return;
            _autoPlaying = true;
            _stopRequested = false;
            OnActiveChanged?.Invoke(true);
            _loopCoroutine = StartCoroutine(AutoPlayLoop());
        }

        public void Stop()
        {
            _stopRequested = true;
            _autoPlaying = false;
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
            OnActiveChanged?.Invoke(false);
        }

        private IEnumerator AutoPlayLoop()
        {
            while (_autoPlaying && !_stopRequested)
            {
                if (ProcessStepAsync != null)
                {
                    var stepTask = ProcessStepAsync();
                    while (!stepTask.IsCompleted) yield return null;
                }

                if (!_autoPlaying || _stopRequested) break;

                float speed = GetCurrentSpeed?.Invoke() ?? 1f;
                yield return new WaitForSeconds(speed);
            }

            _autoPlaying = false;
            OnActiveChanged?.Invoke(false);
        }
    }
}
