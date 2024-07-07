using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Quiz.Api.Services
{
    public class WebSocketServer
    {
        private readonly ILogger<WebSocketServer> _logger;
        private readonly GameScoreService _gameScoreService;
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        public WebSocketServer(ILogger<WebSocketServer> logger, GameScoreService gameScoreService)
        {
            _logger = logger;
            _gameScoreService = gameScoreService;
        }

        public async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var socketId = Guid.NewGuid().ToString();
            _sockets[socketId] = webSocket;

            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await _gameScoreService.PublishGameScoreAsync(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketServer", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket error");
            }
            finally
            {
                _sockets.TryRemove(socketId, out _);
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Closed by the WebSocketServer due to an error", CancellationToken.None);
            }
        }

        public async Task BroadcastAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            foreach (var socket in _sockets.Values)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
