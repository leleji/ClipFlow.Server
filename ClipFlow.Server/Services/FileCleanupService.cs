using Microsoft.Extensions.Options;
using ClipFlow.Server.Models;

namespace ClipFlow.Server.Services
{
    public class FileCleanupService : IHostedService, IDisposable
    {
        private readonly ILogger<FileCleanupService> _logger;
        private readonly AppSettings _appSettings;
        private readonly string _fileStoragePath;
        private readonly ClipboardDataManager _clipboardManager;
        private Timer _timer;

        public FileCleanupService(
            ILogger<FileCleanupService> logger,
            IOptions<AppSettings> appSettings,
            ClipboardDataManager clipboardManager)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _clipboardManager = clipboardManager;
            _fileStoragePath = Path.Combine(AppContext.BaseDirectory, "files");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("文件清理服务已启动");

            // 每5分钟检查一次过期文件
            _timer = new Timer(CleanupExpiredFiles, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        private void CleanupExpiredFiles(object state)
        {
            try
            {
                if (!Directory.Exists(_fileStoragePath))
                {
                    return;
                }

                var expirationTime = DateTime.UtcNow.AddMinutes(-_appSettings.FileCacheMinutes);
                var files = Directory.GetFiles(_fileStoragePath);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < expirationTime)
                    {
                        try
                        {
                            // 从文件名中提取UUID
                            var uuid = Path.GetFileNameWithoutExtension(fileInfo.Name);
                            
                            // 检查文件是否在任何用户的剪贴板历史中使用
                            var isInUse = false;
                            foreach (var history in _clipboardManager.GetAllHistories())
                            {
                                if (history.Any(x => x.Uuid == uuid))
                                {
                                    isInUse = true;
                                    break;
                                }
                            }

                            if (!isInUse)
                            {
                                System.IO.File.Delete(file);
                                _logger.LogInformation($"已删除过期文件: {fileInfo.Name}");
                            }
                            else
                            {
                                _logger.LogInformation($"文件仍在使用中，跳过删除: {fileInfo.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"删除过期文件失败: {fileInfo.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期文件时发生错误");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("文件清理服务已停止");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
} 