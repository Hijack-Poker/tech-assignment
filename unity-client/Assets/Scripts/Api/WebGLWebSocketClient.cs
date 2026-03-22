using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace HijackPoker.Api
{
    /// <summary>
    /// WebSocket client for WebGL builds using JavaScript interop.
    /// Mirrors the WebSocketClient API so ConnectionManager can use either.
    ///
    /// Does NOT use Task.Delay or polling in ConnectAsync — WebGL is single-threaded
    /// so blocking awaits prevent JS callbacks from firing. Instead, connection
    /// state is detected in PollMessages() called from Update().
    /// </summary>
    public class WebGLWebSocketClient
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int WebSocket_Create(string url);
        [DllImport("__Internal")] private static extern int WebSocket_GetState(int id);
        [DllImport("__Internal")] private static extern void WebSocket_Send(int id, string msg);
        [DllImport("__Internal")] private static extern int WebSocket_Receive(int id, byte[] buffer, int bufferSize);
        [DllImport("__Internal")] private static extern void WebSocket_Close(int id);
#else
        private static int WebSocket_Create(string url) => -1;
        private static int WebSocket_GetState(int id) => 3;
        private static void WebSocket_Send(int id, string msg) { }
        private static int WebSocket_Receive(int id, byte[] buffer, int bufferSize) => 0;
        private static void WebSocket_Close(int id) { }
#endif

        private int _socketId = -1;
        private int _pendingTableId = -1;
        private bool _subscribed;
        private readonly string _url;
        private readonly byte[] _recvBuffer = new byte[16384];
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _incomingMessages = new();
        private volatile bool _connected;
        private volatile bool _disconnected;

        public bool IsConnected => _connected;
        public bool WasDisconnected => _disconnected;

        public WebGLWebSocketClient(string url = null)
        {
            _url = url ?? ServerConfig.WsBaseUrl;
        }

        /// <summary>
        /// Creates the WebSocket and returns immediately.
        /// Actual connection completion is detected in PollMessages().
        /// </summary>
        public async Task ConnectAsync(int tableId)
        {
            _disconnected = false;
            _connected = false;
            _subscribed = false;
            _socketId = WebSocket_Create(_url);

            if (_socketId < 0)
                throw new Exception("Failed to create WebSocket");

            _pendingTableId = tableId;

            // Don't poll here — WebGL is single-threaded, Task.Delay blocks the JS event loop.
            // PollMessages() will detect the open state and send the subscribe message.
            await Task.CompletedTask;
        }

        /// <summary>
        /// Poll for connection state and messages from JS. Call from Update().
        /// </summary>
        public void PollMessages()
        {
            if (_socketId < 0) return;

            int state = WebSocket_GetState(_socketId);

            // Detect connection opened
            if (!_connected && state == 1) // OPEN
            {
                _connected = true;

                if (!_subscribed && _pendingTableId >= 0)
                {
                    var sub = $"{{\"action\":\"subscribe\",\"tableId\":{_pendingTableId}}}";
                    WebSocket_Send(_socketId, sub);
                    _subscribed = true;
                    Debug.Log($"[WS] Subscribed to table {_pendingTableId}");
                }
            }

            // Detect disconnect
            if (state == 3 && _connected) // CLOSED
            {
                _connected = false;
                _disconnected = true;
                return;
            }

            // Detect connection failed (went from CONNECTING to CLOSED without OPEN)
            if (state == 3 && !_connected && !_disconnected)
            {
                _disconnected = true;
                return;
            }

            // Drain message queue
            while (state == 1)
            {
                int len = WebSocket_Receive(_socketId, _recvBuffer, _recvBuffer.Length);
                if (len <= 0) break;
                string msg = System.Text.Encoding.UTF8.GetString(_recvBuffer, 0, len);
                _incomingMessages.Enqueue(msg);
            }
        }

        public bool TryDequeueMessage(out string message)
        {
            return _incomingMessages.TryDequeue(out message);
        }

        public void ClearDisconnectFlag()
        {
            _disconnected = false;
        }

        public async Task DisconnectAsync()
        {
            _connected = false;
            if (_socketId >= 0)
            {
                WebSocket_Close(_socketId);
                _socketId = -1;
            }
            await Task.CompletedTask;
        }
    }
}
