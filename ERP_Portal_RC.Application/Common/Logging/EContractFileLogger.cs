using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    public class EContractFileLogger
    {
        private readonly string _logDirectory;
        private readonly int _retentionDays;

        public EContractFileLogger(IConfiguration configuration)
        {
            _logDirectory = configuration["EContractLogConfig:LogPath"]
                         ?? Path.Combine(AppContext.BaseDirectory, "Logs", "EContract");
            _retentionDays = int.TryParse(configuration["EContractLogConfig:RetentionDays"], out var days)
                             ? days : 14;

            Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogAsync(string level, string orderOid, string message, object? data = null)
        {
            // Xóa log cũ hơn 14 ngày
            CleanOldLogs();

            var logFile = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
            var logEntry = new StringBuilder();

            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] OID: {orderOid}");
            logEntry.AppendLine($"  Message : {message}");

            if (data != null)
                logEntry.AppendLine($"  Data    : {JsonSerializer.Serialize(data)}");

            logEntry.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(logFile, logEntry.ToString());
        }

        public Task LogInfoAsync(string orderOid, string message, object? data = null)
            => LogAsync("INFO", orderOid, message, data);

        public Task LogErrorAsync(string orderOid, string message, object? data = null)
            => LogAsync("ERROR", orderOid, message, data);

        private void CleanOldLogs()
        {
            var cutoff = DateTime.Now.AddDays(-_retentionDays);
            var oldFiles = Directory.GetFiles(_logDirectory, "*.log")
                .Where(f => File.GetCreationTime(f) < cutoff);

            foreach (var file in oldFiles)
            {
                try { File.Delete(file); } catch { }
            }
        }
    }
}
