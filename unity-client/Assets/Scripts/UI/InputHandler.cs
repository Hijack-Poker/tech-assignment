using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace HijackPoker.UI
{
    /// <summary>
    /// Keyboard shortcuts for desktop/WebGL: Space (next step), R (reset), A (auto-play), S (speed), H (hand history).
    /// Skips input when an InputField is focused.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public event Action OnNextStep;
        public event Action OnReset;
        public event Action OnAutoPlayToggle;
        public event Action OnSpeedCycle;
        public event Action OnHandHistoryToggle;
        public event Action OnMuteToggle;

#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
        private void Update()
        {
            // Skip if an input field is focused
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.GetComponent<TMP_InputField>() != null)
                return;

            if (Input.GetKeyDown(KeyCode.Space))
                OnNextStep?.Invoke();
            else if (Input.GetKeyDown(KeyCode.R))
                OnReset?.Invoke();
            else if (Input.GetKeyDown(KeyCode.A))
                OnAutoPlayToggle?.Invoke();
            else if (Input.GetKeyDown(KeyCode.S))
                OnSpeedCycle?.Invoke();
            else if (Input.GetKeyDown(KeyCode.H))
                OnHandHistoryToggle?.Invoke();
            else if (Input.GetKeyDown(KeyCode.M))
                OnMuteToggle?.Invoke();
        }
#endif
    }
}
