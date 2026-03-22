using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HijackPoker.Api
{
    /// <summary>
    /// WebSocket client for real-time table updates from cash-game-broadcast.
    /// Falls back gracefully when WebSocket is unavailable (local dev).
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        [SerializeField] private string wsUrl = "ws://localhost:3032";
        [SerializeField] private float reconnectDelaySec = 5f;

        public event Action<int> OnTableUpdate;
        public bool IsConnected { get; private set; }

        private System.Net.WebSockets.ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private bool _shouldRun;

        public void Connect(int tableId)
        {
            _shouldRun = true;
            _cts = new CancellationTokenSource();
            _ = ConnectLoop(tableId);
        }

        public void Disconnect()
        {
            _shouldRun = false;
            _cts?.Cancel();
            CloseSocket();
        }

        private async Task ConnectLoop(int tableId)
        {
            while (_shouldRun && !_cts.IsCancellationRequested)
            {
                try
                {
                    _ws = new System.Net.WebSockets.ClientWebSocket();
                    var uri = new Uri($"{wsUrl}?tableId={tableId}");
                    await _ws.ConnectAsync(uri, _cts.Token);
                    IsConnected = true;
                    Debug.Log($"[WebSocket] Connected to {uri}");

                    await ReceiveLoop();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.Log($"[WebSocket] Connection unavailable ({ex.GetType().Name}), will retry in {reconnectDelaySec}s");
                    IsConnected = false;
                }

                if (!_shouldRun) break;

                try
                {
                    await Task.Delay((int)(reconnectDelaySec * 1000), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            IsConnected = false;
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            var sb = new StringBuilder();

            while (_ws.State == System.Net.WebSockets.WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await _ws.ReceiveAsync(segment, _cts.Token);

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    Debug.Log("[WebSocket] Server closed connection");
                    break;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    string message = sb.ToString();
                    sb.Clear();
                    HandleMessage(message);
                }
            }

            IsConnected = false;
        }

        private void HandleMessage(string message)
        {
            try
            {
                var json = JObject.Parse(message);
                string eventType = json["type"]?.ToString() ?? json["eventType"]?.ToString() ?? "";

                if (eventType == "TABLE_UPDATE")
                {
                    int tableId = json["tableId"]?.Value<int>() ?? 0;
                    if (tableId > 0)
                    {
                        // Dispatch to main thread
                        UnityMainThread.Enqueue(() => OnTableUpdate?.Invoke(tableId));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebSocket] Failed to parse message: {ex.Message}");
            }
        }

        private void CloseSocket()
        {
            try
            {
                if (_ws != null && _ws.State == System.Net.WebSockets.WebSocketState.Open)
                    _ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
            }
            catch { }
            _ws?.Dispose();
            _ws = null;
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Simple main-thread dispatcher for WebSocket callbacks.
    /// </summary>
    public class UnityMainThread : MonoBehaviour
    {
        private static UnityMainThread _instance;
        private readonly System.Collections.Generic.Queue<Action> _queue = new();

        private void Awake()
        {
            if (_instance != null) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                    _queue.Dequeue()?.Invoke();
            }
        }

        public static void Enqueue(Action action)
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThread", typeof(UnityMainThread));
                DontDestroyOnLoad(go);
                _instance = go.GetComponent<UnityMainThread>();
            }
            lock (_instance._queue)
            {
                _instance._queue.Enqueue(action);
            }
        }
    }
}
