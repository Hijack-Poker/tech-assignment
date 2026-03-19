using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HijackPoker.Api
{
    /// <summary>
    /// WebSocket client for cash-game-broadcast service.
    /// Receives TableResponse JSON when game state changes.
    /// Uses ConcurrentQueue for thread-safe message delivery to main thread.
    /// </summary>
    public class WebSocketClient
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private readonly string _url;
        private readonly ConcurrentQueue<string> _incomingMessages = new();
        private volatile bool _disconnected;
        private volatile bool _connected;

        public bool IsConnected => _connected && _ws?.State == WebSocketState.Open;
        public bool WasDisconnected => _disconnected;

        public WebSocketClient(string url = null)
        {
            _url = url ?? ServerConfig.WsBaseUrl;
        }

        public async Task ConnectAsync(int tableId)
        {
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            _disconnected = false;
            _connected = false;

            try
            {
                await _ws.ConnectAsync(new Uri(_url), _cts.Token);
                _connected = true;

                // Subscribe to table updates
                var sub = $"{{\"action\":\"subscribe\",\"tableId\":{tableId}}}";
                var bytes = Encoding.UTF8.GetBytes(sub);
                await _ws.SendAsync(new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text, true, _cts.Token);

                // Start receive loop on background thread
                _ = Task.Run(() => ReceiveLoop());
            }
            catch
            {
                _connected = false;
                _cts?.Cancel();
                _ws?.Dispose();
                _ws = null;
                throw;
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[16384];
            var sb = new StringBuilder();

            try
            {
                while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(
                            new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _disconnected = true;
                            _connected = false;
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    _incomingMessages.Enqueue(sb.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex)
            {
                Debug.LogWarning($"WebSocket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WebSocket unexpected error: {ex.Message}");
            }
            finally
            {
                _disconnected = true;
                _connected = false;
            }
        }

        /// <summary>
        /// Dequeue a received message (call from main thread in Update).
        /// </summary>
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
            _cts?.Cancel();
            _connected = false;

            if (_ws?.State == WebSocketState.Open)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(2000);
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        "Client closing", timeout.Token);
                }
                catch { }
            }

            _ws?.Dispose();
            _ws = null;
        }
    }
}
