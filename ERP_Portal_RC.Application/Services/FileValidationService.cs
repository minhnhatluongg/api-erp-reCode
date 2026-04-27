using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class FileValidationService : IFileValidationService
    {
        // Magic bytes của các file nguy hiểm cần block
        private static readonly byte[][] DangerousSignatures =
        [
            [0x4D, 0x5A],             // .exe / .dll (MZ header)
        [0x7F, 0x45, 0x4C, 0x46], // ELF binary (Linux executable)
        [0x23, 0x21],             // Shebang script (#!/)
        [0xCA, 0xFE, 0xBA, 0xBE], // Java .class
        ];

        // Magic bytes hợp lệ map theo extension
        private static readonly Dictionary<string, byte[][]> AllowedSignatures = new()
        {
            [".pdf"] = [[0x25, 0x50, 0x44, 0x46]],                           // %PDF
            [".png"] = [[0x89, 0x50, 0x4E, 0x47]],                           // PNG
            [".jpg"] = [[0xFF, 0xD8, 0xFF]],                                  // JPEG
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],                           // ZIP-based (Office)
            [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
        };
        private readonly FileUploadConfig _config;
        private readonly IVirusScanService _virusScan;
        private readonly ILogger<FileValidationService> _logger;
        public FileValidationService(
        IOptions<FileUploadConfig> config,
        IVirusScanService virusScan,
        ILogger<FileValidationService> logger)
        {
            _config = config.Value;
            _virusScan = virusScan;
            _logger = logger;
        }

        public async Task<(bool IsValid, string Error)> ValidateAsync(IFormFile file, CancellationToken ct = default)
        {
            // 1. Kích thước
            if (file.Length > _config.MaxFileSizeBytes)
                return (false, $"File vượt quá giới hạn {_config.MaxFileSizeBytes / 1_048_576}MB.");

            // 2. Extension whitelist
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_config.AllowedExtensions.Contains(ext))
                return (false, $"Định dạng '{ext}' không được phép.");

            // 3. MIME type whitelist
            if (!_config.AllowedMimeTypes.Contains(file.ContentType.ToLower()))
                return (false, $"Content-Type '{file.ContentType}' không được phép.");

            // 4. Magic bytes — đọc 8 byte đầu
            var header = new byte[8];
            await using var stream = file.OpenReadStream();
            int read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
            stream.Position = 0;

            // Block file nguy hiểm
            foreach (var sig in DangerousSignatures)
                if (MatchesSignature(header, sig))
                    return (false, "File chứa nội dung nguy hiểm.");

            // Xác minh magic bytes khớp extension
            if (AllowedSignatures.TryGetValue(ext, out var validSigs))
            {
                bool matched = validSigs.Any(sig => MatchesSignature(header, sig));
                if (!matched)
                    return (false, "Nội dung file không khớp với định dạng khai báo.");
            }

            // 5. Virus scan (nếu bật)
            if (_config.VirusScan.Enabled)
            {
                stream.Position = 0;
                var (clean, virusName) = await _virusScan.ScanAsync(stream, ct);
                if (!clean)
                    return (false, $"File bị phát hiện chứa mã độc: {virusName}");
            }

            return (true, "");
        }
        private static bool MatchesSignature(byte[] header, byte[] signature)
        => header.Length >= signature.Length
        && header.Take(signature.Length).SequenceEqual(signature);
    }
}
