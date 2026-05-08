using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    /// <summary>
    /// Base file logger cho các API gọi từ/đến bên thứ 3 (Webhook, WinInvoice, ...).
    /// Kế thừa class này và truyền apiName để có log file riêng biệt.
    ///
    /// Log folder: appsettings → ExternalApiLogConfig:LogPath (mặc định: Logs/ExternalApi)
    /// File format: {apiName}_{yyyy-MM-dd}.log
    ///
    /// Ví dụ kế thừa:
    ///   public class WebhookFileLogger : ExternalApiFileLogger
    ///   {
    ///       public WebhookFileLogger(IConfiguration cfg)
    ///           : base(cfg, "Webhook") { }
    ///   }
    /// </summary>
    public class ExternalApiFileLogger
    {
        private readonly string _logDirectory;
        private readonly string _apiName;
        private readonly int    _retentionDays;

        // Lock riêng theo từng apiName để tránh block lẫn nhau giữa các logger
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim>
            _locks = new();

        public ExternalApiFileLogger(IConfiguration configuration, string apiName)
        {
            _apiName = apiName.Trim().Replace(" ", "_");

            var configPath = configuration["ExternalApiLogConfig:LogPath"]
                          ?? "Logs/ExternalApi";

            _logDirectory = Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(AppContext.BaseDirectory, configPath);

            _retentionDays = int.TryParse(
                configuration["ExternalApiLogConfig:RetentionDays"], out var d)
                ? d : 30;

            Directory.CreateDirectory(_logDirectory);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Log một request đến từ bên thứ 3 (INBOUND).</summary>
        public Task LogInboundAsync(string correlationId, string endpoint,
            string status, string? clientIp = null, object? payload = null, string? message = null)
            => WriteAsync("INBOUND", correlationId, endpoint, status, clientIp, payload, message);

        /// <summary>Log một request gọi ra bên thứ 3 (OUTBOUND).</summary>
        public Task LogOutboundAsync(string correlationId, string endpoint,
            string status, object? payload = null, string? message = null)
            => WriteAsync("OUTBOUND", correlationId, endpoint, status, null, payload, message);

        /// <summary>Log lỗi.</summary>
        public Task LogErrorAsync(string correlationId, string endpoint,
            string errorMessage, object? payload = null)
            => WriteAsync("ERROR", correlationId, endpoint, "FAILED", null, payload, errorMessage);

        /// <summary>Log thông tin chung.</summary>
        public Task LogInfoAsync(string correlationId, string message, object? data = null)
            => WriteAsync("INFO", correlationId, "", "INFO", null, data, message);

        // ── Core writer ───────────────────────────────────────────────────────

        protected async Task WriteAsync(
            string direction,
            string correlationId,
            string endpoint,
            string status,
            string? clientIp,
            object? payload,
            string? message)
        {
            CleanOldLogs();

            var sb = new StringBuilder();
            sb.AppendLine($"┌─ [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{direction}] [{status}]");
            sb.AppendLine($"│  API          : {_apiName}");
            if (!string.IsNullOrEmpty(correlationId))
                sb.AppendLine($"│  CorrelationId: {correlationId}");
            if (!string.IsNullOrEmpty(endpoint))
                sb.AppendLine($"│  Endpoint     : {endpoint}");
            if (!string.IsNullOrEmpty(clientIp))
                sb.AppendLine($"│  Client IP    : {clientIp}");
            if (!string.IsNullOrEmpty(message))
                sb.AppendLine($"│  Message      : {message}");
            if (payload is not null)
            {
                var json = JsonSerializer.Serialize(payload,
                    new JsonSerializerOptions { WriteIndented = true });
                // Indent mỗi dòng json để dễ đọc trong log file
                var indented = string.Join("\n", json.Split('\n').Select(l => "│    " + l));
                sb.AppendLine($"│  Payload      :");
                sb.AppendLine(indented);
            }
            sb.AppendLine($"└─ {new string('─', 72)}");

            var fileLock = _locks.GetOrAdd(_apiName, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(GetLogFilePath(), sb.ToString(), Encoding.UTF8);
            }
            finally
            {
                fileLock.Release();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetLogFilePath()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(_logDirectory, $"{_apiName}_{date}.log");
        }

        private void CleanOldLogs()
        {
            try
            {
                var cutoff  = DateTime.Now.AddDays(-_retentionDays);
                var pattern = $"{_apiName}_*.log";
                foreach (var f in Directory.GetFiles(_logDirectory, pattern)
                                           .Where(f => File.GetCreationTime(f) < cutoff))
                    try { File.Delete(f); } catch { }
            }
            catch { }
        }
    }
}
