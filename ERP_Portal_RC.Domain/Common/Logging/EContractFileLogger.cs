using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    /// <summary>
    /// File-based logger.
    ///
    /// THIẾT KẾ (rev 2):
    ///  - KHÔNG bao giờ throw — mọi exception đều swallow để không bao giờ phá nghiệp vụ chính.
    ///  - KHÔNG tạo file _init_*.log tự động trong constructor nữa — chỉ ghi vào Trace.
    ///    (Trước đó mỗi lần DI resolve đều append marker → file phình to / tạo file rỗng
    ///     khi logger không thực sự được dùng. Đặc biệt khi đăng ký Scoped → mỗi request
    ///     đẻ ra 1 marker.)
    ///  - Marker init giờ chỉ ghi MỘT LẦN/process/prefix, và CHỈ khi có lệnh ghi log
    ///    thực sự đầu tiên (lazy). Nếu admin cần verify ngay, gọi <see cref="WriteInitMarkerNow"/>
    ///    qua /api/loggerhealth.
    ///  - Có fallback path khi config path không write được: AppContext.BaseDirectory/Logs/EContract.
    ///  - Mọi self-error sẽ ra Trace (System.Diagnostics) → DebugView / EventLog.
    ///
    /// Cấu hình appsettings.json:
    ///   "EContractLogConfig": {
    ///     "LogPath": "Logs/EContract",   // tuyệt đối hoặc tương đối; null/rỗng → fallback
    ///     "RetentionDays": 14
    ///   }
    ///
    /// File output: {LogPath}/{filePrefix}_{yyyy-MM-dd}.log (hoặc {yyyy-MM-dd}.log).
    /// </summary>
    public class EContractFileLogger
    {
        private readonly string _logDirectory;
        private readonly int _retentionDays;
        private readonly string _filePrefix;
        private readonly string? _initError;

        private static readonly SemaphoreSlim _fileLock = new(1, 1);
        private static readonly string FallbackRoot =
            Path.Combine(AppContext.BaseDirectory, "Logs", "EContract");

        // Theo dõi marker init đã ghi cho từng (prefix,directory) trong process này chưa.
        // Key = "{prefix}|{dir}" → tránh đụng giữa nhiều instance cùng prefix.
        private static readonly HashSet<string> _initMarkerWritten = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _initMarkerGate = new();

        public EContractFileLogger(IConfiguration configuration, string filePrefix = "")
        {
            _filePrefix = filePrefix ?? string.Empty;

            var configPath = configuration["EContractLogConfig:LogPath"];

            _retentionDays = int.TryParse(
                configuration["EContractLogConfig:RetentionDays"], out var days)
                ? days : 14;

            // 1) Resolve thư mục mong muốn
            string desired = !string.IsNullOrWhiteSpace(configPath)
                ? (Path.IsPathRooted(configPath)
                    ? configPath
                    : Path.Combine(AppContext.BaseDirectory, configPath))
                : FallbackRoot;

            // 2) Probe-write — nếu không tạo được/không write được → fallback
            if (TryEnsureWritable(desired, out var err1))
            {
                _logDirectory = desired;
                _initError = null;
            }
            else if (TryEnsureWritable(FallbackRoot, out var err2))
            {
                _logDirectory = FallbackRoot;
                _initError = $"Primary path '{desired}' không write được ({err1}); fallback → '{FallbackRoot}'";
                Trace.WriteLine($"[EContractFileLogger] {_initError}");
            }
            else
            {
                // Hết cách — gán tạm để class không null, nhưng mọi Append sẽ swallow
                _logDirectory = desired;
                _initError = $"Cả primary ({err1}) lẫn fallback ({err2}) đều không write được. Không có log nào sẽ được ghi.";
                Trace.WriteLine($"[EContractFileLogger][FATAL] {_initError}");
            }

            // 3) KHÔNG ghi file _init nữa. Chỉ Trace cho admin xem qua DebugView nếu cần.
            Trace.WriteLine(
                $"[EContractFileLogger] ready. prefix='{_filePrefix}' dir='{_logDirectory}' " +
                $"retention={_retentionDays} initError={_initError ?? "<none>"}");
        }

        public async Task LogAsync(string level, string orderOid, string message, object? data = null)
        {
            try
            {
                // Lần đầu thực sự log → mới ghi marker init (lazy, 1 lần/process/prefix).
                EnsureInitMarkerWrittenOnce();

                CleanOldLogs();

                var logFile = BuildLogFilePath();

                var entry = new StringBuilder();
                entry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] OID: {orderOid}");
                entry.AppendLine($"  Message : {message}");

                if (data is not null)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            // Tránh fail khi serialize types phức tạp (DataTable, ExpandoObject…)
                        });
                        entry.AppendLine($"  Data    : {json}");
                    }
                    catch (Exception sx)
                    {
                        entry.AppendLine($"  Data    : <serialize failed: {sx.Message}>");
                    }
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
            catch (Exception ex)
            {
                // KHÔNG re-throw — log lỗi self qua Trace + EventLog fallback
                Trace.WriteLine($"[EContractFileLogger][LogAsync] FAILED prefix={_filePrefix} oid={orderOid} -> {ex.GetType().Name}: {ex.Message}");
            }
        }

        public Task LogInfoAsync(string orderOid, string message, object? data = null)
            => LogAsync("INFO", orderOid, message, data);

        public Task LogErrorAsync(string orderOid, string message, object? data = null)
            => LogAsync("ERROR", orderOid, message, data);

        public Task LogWarnAsync(string orderOid, string message, object? data = null)
            => LogAsync("WARN", orderOid, message, data);

        /// <summary>
        /// Trả info trạng thái logger (dùng cho /debug endpoint nếu muốn).
        /// </summary>
        public object GetStatus() => new
        {
            FilePrefix = _filePrefix,
            LogDirectory = _logDirectory,
            RetentionDays = _retentionDays,
            InitError = _initError,
            DirectoryExists = SafeExists(_logDirectory),
            TodayLogFile = BuildLogFilePath(),
            TodayFileExists = SafeFileExists(BuildLogFilePath())
        };

        #region Helpers

        private static bool TryEnsureWritable(string dir, out string? error)
        {
            error = null;
            try
            {
                Directory.CreateDirectory(dir);
                var probe = Path.Combine(dir, $".write-probe-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, "probe");
                File.Delete(probe);
                return true;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Ép ghi marker init NGAY LẬP TỨC (force). Dùng cho /api/loggerhealth khi admin
        /// muốn xác nhận thủ công logger còn sống. Không reset cờ once → vẫn tôn trọng
        /// rule "không spam file _init_*.log".
        /// </summary>
        public void WriteInitMarkerNow() => EnsureInitMarkerWrittenOnce(force: true);

        /// <summary>
        /// Ghi marker init đúng 1 lần/process/prefix/directory. Gọi mỗi lần log;
        /// các lần sau là no-op nhờ HashSet tĩnh.
        /// </summary>
        private void EnsureInitMarkerWrittenOnce(bool force = false)
        {
            var key = $"{_filePrefix}|{_logDirectory}";

            if (!force)
            {
                lock (_initMarkerGate)
                {
                    if (_initMarkerWritten.Contains(key)) return;
                }
            }

            try
            {
                var marker = Path.Combine(_logDirectory,
                    $"_init_{(_filePrefix == "" ? "default" : _filePrefix)}.log");
                var text =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EContractFileLogger initialized." + Environment.NewLine +
                    $"  Prefix         : {_filePrefix}" + Environment.NewLine +
                    $"  LogDirectory   : {_logDirectory}" + Environment.NewLine +
                    $"  RetentionDays  : {_retentionDays}" + Environment.NewLine +
                    $"  InitError      : {_initError ?? "<none>"}" + Environment.NewLine +
                    $"  Process        : {Environment.UserName}@{Environment.MachineName} (PID {Environment.ProcessId})" + Environment.NewLine +
                    new string('-', 80) + Environment.NewLine;
                File.AppendAllText(marker, text);

                lock (_initMarkerGate) { _initMarkerWritten.Add(key); }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EContractFileLogger][InitMarker] {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void CleanOldLogs()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-_retentionDays);
                var pattern = string.IsNullOrWhiteSpace(_filePrefix) ? "*.log" : $"{_filePrefix}_*.log";

                var oldFiles = Directory.GetFiles(_logDirectory, pattern)
                    .Where(f => File.GetCreationTime(f) < cutoff);

                foreach (var file in oldFiles)
                {
                    try { File.Delete(file); }
                    catch { /* swallow */ }
                }
            }
            catch { /* swallow */ }
        }

        private string BuildLogFilePath()
        {
            var datePart = DateTime.Now.ToString("yyyy-MM-dd");
            var fileName = string.IsNullOrWhiteSpace(_filePrefix)
                ? $"{datePart}.log"
                : $"{_filePrefix}_{datePart}.log";
            return Path.Combine(_logDirectory, fileName);
        }

        private static bool SafeExists(string dir)
        {
            try { return Directory.Exists(dir); } catch { return false; }
        }
        private static bool SafeFileExists(string file)
        {
            try { return File.Exists(file); } catch { return false; }
        }

        #endregion
    }
}
