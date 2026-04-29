using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class ChunkUploadService : IChunkUploadService
    {
        private readonly FileUploadConfig _config;
        public ChunkUploadService(IOptions<FileUploadConfig> config)
            => _config = config.Value;

        private string GetSessionDir(string sessionId)
            => Path.Combine(_config.PhysicalRootPath, "_chunks", sessionId);

        public void CleanupSession(string sessionId)
        {
            var dir = GetSessionDir(sessionId);
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            return new string(normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray())
                .Normalize(NormalizationForm.FormC);
        }

        public async Task<string> MergeChunksAsync(string sessionId, string fileName, string oid, int totalChunks, CancellationToken ct = default)
        {
            var sessionDir = GetSessionDir(sessionId);
            var parts = Directory.GetFiles(sessionDir, "*.part").OrderBy(f => f).ToList();

            if (parts.Count != totalChunks)
                throw new InvalidOperationException(
                    $"Thiếu chunk: nhận {parts.Count}/{totalChunks}");

            // Thư mục đích
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString("00");
            string cleanId = oid.Replace("/", "_").Replace(":", "_").Trim();
            string relDir = Path.Combine(year, month, cleanId);
            string physDir = Path.Combine(_config.PhysicalRootPath, relDir);
            Directory.CreateDirectory(physDir);

            string finalName = $"{Guid.NewGuid()}_{RemoveDiacritics(fileName)}";
            string finalPath = Path.Combine(physDir, finalName);

            // Ghép file
            await using var output = new FileStream(finalPath, FileMode.Create);
            foreach (var part in parts)
            {
                await using var input = new FileStream(part, FileMode.Open);
                await input.CopyToAsync(output, ct);
            }

            CleanupSession(sessionId);

            return Path.Combine(relDir, finalName).Replace("\\", "/");
        }

        public async Task<string> SaveChunkAsync(string sessionId, int chunkIndex, IFormFile chunk, CancellationToken ct = default)
        {
            var dir = GetSessionDir(sessionId);
            Directory.CreateDirectory(dir);

            var chunkPath = Path.Combine(dir, $"{chunkIndex:D6}.part");
            await using var fs = new FileStream(chunkPath, FileMode.Create);
            await chunk.CopyToAsync(fs, ct);

            return chunkPath;
        }
    }
}
