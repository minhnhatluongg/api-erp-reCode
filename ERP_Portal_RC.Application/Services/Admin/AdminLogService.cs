using ERP_Portal_RC.Application.Interfaces.Admin;
using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Application.Services.Admin
{
    public class AdminLogService : IAdminLogService
    {
        private readonly Dictionary<string, string> _roots;

        // category key → thư mục vật lý (resolve từ AppContext.BaseDirectory nếu path tương đối)
        public AdminLogService(IConfiguration config)
        {
            _roots = new(StringComparer.OrdinalIgnoreCase)
            {
                ["econtract"]   = Resolve(config["EContractLogConfig:LogPath"]    ?? "Logs/EContract"),
                ["externalapi"] = Resolve(config["ExternalApiLogConfig:LogPath"]  ?? "Logs/ExternalApi"),
                ["stdout"]      = Resolve(config["StdoutLogPath"]                 ?? "logs"), // IIS stdout
            };
        }

        private static string Resolve(string path) =>
            Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

        // ── List files ────────────────────────────────────────────────────────
        public IEnumerable<LogFileInfo> ListFiles(string? category = null, string? date = null)
        {
            var result = new List<LogFileInfo>();

            var cats = category == null
                ? _roots
                : _roots.Where(kv => kv.Key.Equals(category, StringComparison.OrdinalIgnoreCase))
                         .ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var (cat, root) in cats)
            {
                if (!Directory.Exists(root)) continue;

                foreach (var f in Directory.GetFiles(root, "*.log", SearchOption.TopDirectoryOnly)
                                           .OrderByDescending(f => f))
                {
                    var info = new FileInfo(f);

                    // Filter theo ngày nếu có (yyyy-MM-dd trong tên file hoặc LastWriteTime)
                    if (date != null)
                    {
                        bool matchName = info.Name.Contains(date);
                        bool matchDate = info.LastWriteTime.ToString("yyyy-MM-dd") == date;
                        if (!matchName && !matchDate) continue;
                    }

                    result.Add(new LogFileInfo(
                        FileName:     info.Name,
                        FilePath:     $"{cat}/{info.Name}",
                        Category:     cat,
                        SizeBytes:    info.Length,
                        LastModified: info.LastWriteTime));
                }
            }

            return result.OrderByDescending(f => f.LastModified);
        }

        // ── Read file (paginate by line) ──────────────────────────────────────
        public async Task<LogReadResult?> ReadFileAsync(
            string category, string fileName, int page, int pageSize)
        {
            var path = ResolvePath(category, fileName);
            if (path == null || !File.Exists(path)) return null;

            // Đọc file — dùng FileShare.ReadWrite để không block process đang ghi
            string[] allLines;
            await using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
                allLines = (await sr.ReadToEndAsync()).Split('\n');

            long total     = allLines.Length;
            int  totalPages = (int)Math.Ceiling((double)total / pageSize);
            int  skip       = (page - 1) * pageSize;
            var  lines      = allLines.Skip(skip).Take(pageSize);

            var fi = new FileInfo(path);
            return new LogReadResult(
                FileName:     fileName,
                Category:     category,
                LastModified: fi.LastWriteTime,
                TotalLines:   total,
                Page:         page,
                PageSize:     pageSize,
                TotalPages:   totalPages,
                Lines:        lines);
        }

        // ── Search keyword ────────────────────────────────────────────────────
        public async Task<IEnumerable<string>> SearchAsync(
            string category, string fileName, string keyword, int maxLines = 200)
        {
            var path = ResolvePath(category, fileName);
            if (path == null || !File.Exists(path)) return Enumerable.Empty<string>();

            var result = new List<string>();
            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);

            string? line;
            int lineNo = 0;
            while ((line = await sr.ReadLineAsync()) != null && result.Count < maxLines)
            {
                lineNo++;
                if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    result.Add($"[L{lineNo}] {line}");
            }

            return result;
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private string? ResolvePath(string category, string fileName)
        {
            if (!_roots.TryGetValue(category, out var root)) return null;

            // Chặn path traversal
            var clean = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(clean)) return null;

            var full = Path.Combine(root, clean);
            // Đảm bảo file nằm trong root folder
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
    }
}
