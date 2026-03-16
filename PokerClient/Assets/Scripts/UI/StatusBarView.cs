using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    public class StatusBarView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Image _background;

        private static readonly Color ConnectedColor = new Color(0.1f, 0.5f, 0.1f);
        private static readonly Color ConnectingColor = new Color(0.5f, 0.4f, 0.0f);
        private static readonly Color ErrorColor = new Color(0.5f, 0.1f, 0.1f);

        private Coroutine _clearErrorCoroutine;

        private void OnEnable() => _stateManager.OnConnectionStatusChanged += OnStatusChanged;
        private void OnDisable() => _stateManager.OnConnectionStatusChanged -= OnStatusChanged;

        private void OnStatusChanged(string status)
        {
            if (_clearErrorCoroutine != null)
            {
                StopCoroutine(_clearErrorCoroutine);
                _clearErrorCoroutine = null;
            }

            _statusText.text = status;

            if (status == "Connected")
                _background.color = ConnectedColor;
            else if (status == "Connecting...")
                _background.color = ConnectingColor;
            else
            {
                _background.color = ErrorColor;
                _statusText.transform.DOKill();
                _statusText.transform.DOShakePosition(0.3f, new Vector3(5f, 0f, 0f), 20, 0f);
                _clearErrorCoroutine = StartCoroutine(ClearErrorAfterDelay(5f));
            }
        }

        private IEnumerator ClearErrorAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _statusText.text = "Connected";
            _background.color = ConnectedColor;
        }

        private void OnDestroy()
        {
            if (_statusText != null) _statusText.transform.DOKill();
        }
    }
}
