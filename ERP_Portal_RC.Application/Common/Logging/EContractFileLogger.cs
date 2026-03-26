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
        private readonly string _filePrefix;

        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public EContractFileLogger(IConfiguration configuration, string filePrefix = "")
        {
            _filePrefix = filePrefix;

            _logDirectory = configuration["EContractLogConfig:LogPath"]
                         ?? Path.Combine(AppContext.BaseDirectory, "Logs", "EContract");

            _retentionDays = int.TryParse(
                configuration["EContractLogConfig:RetentionDays"], out var days)
                ? days : 14;

            Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogAsync(string level, string orderOid, string message, object? data = null)
        {
            CleanOldLogs();

            var logFile = BuildLogFilePath();

            var entry = new StringBuilder();
            entry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] OID: {orderOid}");
            entry.AppendLine($"  Message : {message}");

            if (data is not null)
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                entry.AppendLine($"  Data    : {json}");
            }

            entry.AppendLine(new string('-', 80));

            await _fileLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(logFile, entry.ToString());
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task LogInfoAsync(string orderOid, string message, object? data = null)
        => LogAsync("INFO", orderOid, message, data);
        public Task LogErrorAsync(string orderOid, string message, object? data = null)
            => LogAsync("ERROR", orderOid, message, data);
        public Task LogWarnAsync(string orderOid, string message, object? data = null)
            => LogAsync("WARN", orderOid, message, data);

        private void CleanOldLogs()
        {
            var cutoff = DateTime.Now.AddDays(-_retentionDays);

            var pattern = string.IsNullOrWhiteSpace(_filePrefix)
                ? "*.log"
                : $"{_filePrefix}_*.log";

            var oldFiles = Directory.GetFiles(_logDirectory, pattern)
                .Where(f => File.GetCreationTime(f) < cutoff);

            foreach (var file in oldFiles)
            {
                try { File.Delete(file); }
                catch { /* */ }
            }
        }
        private string BuildLogFilePath()
        {
            var datePart = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = string.IsNullOrWhiteSpace(_filePrefix)
                ? $"{datePart}.log"
                : $"{_filePrefix}_{datePart}.log";

            return Path.Combine(_logDirectory, fileName);
        }
    }
}
