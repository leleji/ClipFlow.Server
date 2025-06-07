using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using ClipFlow.Server.Models;

namespace ClipFlow.Server.Services
{
    public class ClipboardWebSocketManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastPingTime = new();
        private readonly ConcurrentDictionary<string, string> _tokenMap = new();
        private readonly ILogger<ClipboardWebSocketManager> _logger;
        private readonly AppSettings _appSettings;

        public ClipboardWebSocketManager(
            ILogger<ClipboardWebSocketManager> logger,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public bool ValidateToken(string token)
        {
            return !string.IsNullOrEmpty(token) && _appSettings.Tokens.Contains(token);
        }

        public void AddSocket(string connectionId, WebSocket socket, string token)
        {
            _sockets.TryAdd(connectionId, socket);
            _tokenMap.TryAdd(connectionId, token);
            _lastPingTime[connectionId] = DateTime.UtcNow;
            
            // 启动心跳检查
            _ = CheckHeartbeat(connectionId);
            
            _logger.LogInformation($"WebSocket connection added. ID: {connectionId}, Token: {token}");
        }

        public void RemoveSocket(string connectionId)
        {
            _sockets.TryRemove(connectionId, out _);
            _tokenMap.TryRemove(connectionId, out _);
            _lastPingTime.TryRemove(connectionId, out _);
            _logger.LogInformation($"WebSocket connection removed. ID: {connectionId}");
        }

        public void UpdatePing(string connectionId)
        {
            _lastPingTime[connectionId] = DateTime.UtcNow;
        }

        private async Task CheckHeartbeat(string connectionId)
        {
            while (_sockets.ContainsKey(connectionId))
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                if (_lastPingTime.TryGetValue(connectionId, out var lastPing))
                {
                    var timeSinceLastPing = DateTime.UtcNow - lastPing;
                    if (timeSinceLastPing > TimeSpan.FromMinutes(1))
                    {
                        if (_sockets.TryGetValue(connectionId, out var socket))
                        {
                            try
                            {
                                await socket.CloseAsync(
                                    WebSocketCloseStatus.PolicyViolation,
                                    "Heartbeat timeout",
                                    CancellationToken.None);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "关闭超时连接失败");
                            }
                            finally
                            {
                                RemoveSocket(connectionId);
                            }
                        }
                        break;
                    }
                }
            }
        }

        public async Task BroadcastToUserAsync(string token, string excludeConnectionId, byte[] message)
        {
            var tasks = _sockets
                .Where(kvp => _tokenMap.TryGetValue(kvp.Key, out var uk) && 
                             uk == token && 
                             kvp.Key != excludeConnectionId)
                .Select(kvp => SendAsync(kvp.Value, message));

            await Task.WhenAll(tasks);
        }

        private static async Task SendAsync(WebSocket socket, byte[] message)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
    }
} 