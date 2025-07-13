using Microsoft.AspNetCore.Mvc;
using ClipFlow.Server.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ClipFlow.Server.Services;
using ClipFlow.Server.Filters;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ClipFlow.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClipboardController : BaseController
    {
        private readonly ClipboardWebSocketManager _webSocketManager;
        private readonly ClipboardDataManager _clipboardManager;
        private readonly ILogger<ClipboardController> _logger;
        private readonly string _fileStoragePath;
        private readonly AppSettings _appSettings;
        public ClipboardController(
            ClipboardWebSocketManager webSocketManager,
            ClipboardDataManager clipboardManager,
            ILogger<ClipboardController> logger,
            IOptions<AppSettings> appSettings)
        {
            _webSocketManager = webSocketManager;
            _clipboardManager = clipboardManager;
            _logger = logger;
            _appSettings = appSettings.Value;
            
            _fileStoragePath = Path.Combine(AppContext.BaseDirectory, "files");
            if (!Directory.Exists(_fileStoragePath))
            {
                Directory.CreateDirectory(_fileStoragePath);
            }
        }


        [HttpPost("{type}")]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<ActionResult<ApiResponse<object>>> Upload(string type,ulong dataLength)
        {
            try
            {
                var setting = _appSettings.TokenSettings.FirstOrDefault(t => t.Token == AuthToken);
                var contentLength = (ulong)(Request.ContentLength ?? 0);
                // 检查文件大小限制（仅当 MaxFileSize > 0 时）
                if (setting.MaxFileSize > 0)
                {
                    var maxSizeInBytes = setting.MaxFileSize * 1024 * 1024; // 转换为字节
                    if (contentLength > maxSizeInBytes)
                    {
                        return BadRequest(ApiResponse<object>.Error(413, $"文件大小超过限制。最大允许: {setting.MaxFileSize}MB，当前文件: {contentLength / 1024.0 / 1024.0:F2}MB"));
                    }
                }

                var record = new ClipboardData
                {
                    Uuid = Guid.NewGuid().ToString(),
                    Type = Enum.Parse<ClipboardType>(type, true),
                    DataLength= dataLength
                };

                // 保存文件时使用 UUID 作为前缀
                var physicalFileName = $"{record.Uuid}.dat";
                var filePath = Path.Combine(_fileStoragePath, physicalFileName);

                const int bufferSize = 81920; // 80KB 缓冲区
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous))
                {
                    var buffer = new byte[bufferSize];
                    int bytesRead;
                    long totalBytesRead = 0;
                    var body = Request.Body;

                    while ((bytesRead = await body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }

                    await fileStream.FlushAsync();
                }
                // 添加到历史记录
                _clipboardManager.AddRecord(UserKet, record);

                // 获取当前连接的客户端ID并记录日志
                _logger.LogInformation($"上传请求来自 client: {ClientId}, ClientKet: {UserKet}");

                // 通知其他客户端
                var json = JsonSerializer.Serialize(record);
                var jsonbuffer = Encoding.UTF8.GetBytes(json);
                await _webSocketManager.BroadcastToUserAsync(UserKet, ClientId, jsonbuffer);

                return Ok(ApiResponse<object>.Success(new { uuid = record.Uuid }, "数据已同步"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传剪贴板数据失败");
                if (ex is BadHttpRequestException badRequestEx)
                {
                    return BadRequest(ApiResponse<object>.Error(400, $"请求体太大: {badRequestEx.Message}"));
                }
                else if (ex is IOException ioEx)
                {
                    _logger.LogError(ioEx, "文件IO错误");
                    return StatusCode(500, ApiResponse<object>.Error(500, "文件保存失败"));
                }
                return StatusCode(500, ApiResponse<object>.Error(500, "服务器内部错误"));
            }
        }

        [HttpGet("file/{uuid}")]
        public ActionResult<ApiResponse<object>> GetFile(string uuid)
        {
            var record = _clipboardManager.GetByUuid(UserKet, uuid);
            if (record == null)
            {
                return NotFound(ApiResponse<object>.Error(404, "数据未找到"));
            }

            var physicalFileName = $"{uuid}.dat";
            var filePath = Path.Combine(_fileStoragePath, physicalFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse<object>.Error(404, "文件未找到"));
            }
            return PhysicalFile(filePath, "application/octet-stream");
        }

        [HttpGet]
        public ActionResult<ApiResponse<ClipboardData>> GetLatest([FromQuery] bool onlyText = false)
        {
            var latest = onlyText
                ? _clipboardManager.GetLatestText(UserKet)
                : _clipboardManager.GetLatest(UserKet);

            if (latest == null)
            {
                return NotFound(ApiResponse<object>.Error(404, "暂无数据"));
            }
            return ApiResponse<ClipboardData>.Success(latest);
        }

        [HttpGet("ws")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task Socket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest && !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(UserKet))
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                try
                {
                    _webSocketManager.AddSocket(ClientId, webSocket, UserKet);

                    var buffer = new byte[1024 * 4];
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            
                            if (message == "ping")
                            {
                                _webSocketManager.UpdatePing(ClientId);
                                await webSocket.SendAsync(
                                    Encoding.UTF8.GetBytes("pong"),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);
                            }
                            else
                            {
                                _logger.LogWarning("收到意外的WebSocket消息");
                            }
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation($"WebSocket closed. ClientId: {ClientId}, Reason: {result.CloseStatusDescription}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocket连接异常");
                }
                finally
                {
                    _webSocketManager.RemoveSocket(ClientId);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
} 