using ERP_Portal_RC.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        public FileStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ContractFilesResponse GetFilesByOid(string oid, int year, int month)
        {
            string uploadRoot = _configuration["FileUpload:PhysicalRootPath"]
            ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments";
            string baseUrl = _configuration["FileUpload:BaseUrl"] ?? "";

            string cleanOid = oid.Replace("/", "_").Replace(":", "_").Trim();
            string relDir = Path.Combine(year.ToString(), month.ToString("00"), cleanOid);
            string physDir = Path.Combine(uploadRoot, relDir);
            string metaPath = Path.Combine(physDir, "metadata.json");

            var response = new ContractFilesResponse { Oid = oid };

            if (!File.Exists(metaPath))
                return response;

            var json = File.ReadAllText(metaPath);
            var list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(json) ?? new();

            response.Files = list.OrderByDescending(f => f.UploadedAt).ToList();
            response.TotalFiles = list.Count;
            return response;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subFolder, CancellationToken ct)
        {
            if (file == null || file.Length == 0) return "";

            string uploadRoot = _configuration["FileUpload:PhysicalRootPath"]
                ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments";

            var now = DateTime.Now;
            string year = now.Year.ToString();
            string month = now.Month.ToString("00");
            string cleanFolder = subFolder.Replace("/", "_").Replace(":", "_").Trim();
            string relDir = Path.Combine(year, month, cleanFolder);
            string physDir = Path.Combine(uploadRoot, relDir);

            Directory.CreateDirectory(physDir);

            string guid = Guid.NewGuid().ToString();
            string safeName = RemoveDiacritics(Path.GetFileName(file.FileName));
            string fileName = $"{guid}_{safeName}";
            string fullPath = Path.Combine(physDir, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            // Lưu metadata
            await AppendMetadataAsync(physDir, relDir, fileName, file, subFolder, now, ct);

            return Path.Combine(relDir, fileName).Replace("\\", "/");
        }

        //public async Task<string> UploadFileAsync(IFormFile file, string subFolder, CancellationToken ct)
        //{
        //    if (file == null || file.Length == 0) return null;

        //    string uploadRoot = _configuration["FileUpload:PhysicalRootPath"];

        //    string year = DateTime.Now.Year.ToString();
        //    string month = DateTime.Now.Month.ToString("00");

        //    string cleanSubFolder = subFolder.Replace("/", "_").Replace(":", "_").Trim();

        //    string relativePath = Path.Combine(year, month, cleanSubFolder);

        //    string physicalPath = Path.Combine(uploadRoot, relativePath);

        //    if (!Directory.Exists(physicalPath))
        //    {
        //        Directory.CreateDirectory(physicalPath);
        //    }

        //    string fileName = $"{Guid.NewGuid()}_{RemoveDiacritics(file.FileName)}";
        //    string fullPhysicalPath = Path.Combine(physicalPath, fileName);

        //    using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream);
        //    }

        //    return Path.Combine(relativePath, fileName).Replace("\\", "/");
        //}

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        private async Task AppendMetadataAsync(
            string physDir, string relDir, string fileName,
            IFormFile file, string oid, DateTime uploadedAt,
            CancellationToken ct)
        {
            string metaPath = Path.Combine(physDir, "metadata.json");

            var list = new List<ContractFileMetadata>();
            if (File.Exists(metaPath))
            {
                var existing = await File.ReadAllTextAsync(metaPath, ct);
                list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(existing) ?? new();
            }

            string baseUrl = _configuration["FileUpload:BaseUrl"] ?? "";
            list.Add(new ContractFileMetadata
            {
                Oid = oid,
                FileName = fileName,
                OriginalName = file.FileName,
                RelativePath = Path.Combine(relDir, fileName).Replace("\\", "/"),
                Url = $"{baseUrl.TrimEnd('/')}/uploads/{relDir.Replace("\\", "/")}/{fileName}",
                SizeBytes = file.Length,
                Extension = Path.GetExtension(file.FileName).ToLowerInvariant(),
                UploadedAt = uploadedAt,
            });

            await File.WriteAllTextAsync(metaPath,
                JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }), ct);
        }

        public async Task<string> RebuildMetadata(string oid, int year, int month, CancellationToken ct = default)
        {
            string uploadRoot = _configuration["FileUpload:PhysicalRootPath"] ?? "";
            string baseUrl = _configuration["FileUpload:BaseUrl"] ?? "";
            string cleanOid = oid.Replace("/", "_").Replace(":", "_").Trim();
            string relDir = Path.Combine(year.ToString(), month.ToString("00"), cleanOid);
            string physDir = Path.Combine(uploadRoot, relDir);
            string metaPath = Path.Combine(physDir, "metadata.json");

            if (!Directory.Exists(physDir))
                return $"Folder không tồn tại: {physDir}";

            // Đọc metadata cũ nếu có (tránh mất data đã lưu trước đó)
            var existingList = new List<ContractFileMetadata>();
            if (File.Exists(metaPath))
            {
                var existing = await File.ReadAllTextAsync(metaPath, ct);
                existingList = JsonSerializer.Deserialize<List<ContractFileMetadata>>(existing) ?? new();
            }

            // Lấy tên file đã có trong metadata rồi → không rebuild lại
            var existingFileNames = existingList.Select(f => f.FileName).ToHashSet();

            // Scan file thực tế trên disk, bỏ qua metadata.json và file đã có
            var newFiles = Directory.GetFiles(physDir)
                .Where(f => !f.EndsWith("metadata.json") && !existingFileNames.Contains(Path.GetFileName(f)))
                .Select(fullPath =>
                {
                    var info = new FileInfo(fullPath);
                    string rel = Path.Combine(relDir, info.Name).Replace("\\", "/");
                    return new ContractFileMetadata
                    {
                        Oid = oid,
                        FileName = info.Name,
                        OriginalName = info.Name, 
                        RelativePath = rel,
                        Url = $"{baseUrl.TrimEnd('/')}/uploads/{rel}",
                        SizeBytes = info.Length,
                        Extension = info.Extension.ToLowerInvariant(),
                        UploadedAt = info.CreationTime,
                    };
                }).ToList();

            // Gộp cũ + mới
            var finalList = existingList.Concat(newFiles)
                .OrderByDescending(f => f.UploadedAt)
                .ToList();

            await File.WriteAllTextAsync(metaPath,
                JsonSerializer.Serialize(finalList, new JsonSerializerOptions { WriteIndented = true }), ct);

            return $"Rebuild thành công: {newFiles.Count} file mới, {existingList.Count} file cũ giữ nguyên. Tổng: {finalList.Count}.";
        }

        public List<ContractFileMetadata> GetAllFilesByOid(string oid)
        {
            string uploadRoot = _configuration["FileUpload:PhysicalRootPath"] ?? "";
            string cleanOid = oid.Replace("/", "_").Replace(":", "_").Trim();
            var result = new List<ContractFileMetadata>();

            if (!Directory.Exists(uploadRoot)) return result;

            // Scan tất cả năm/tháng tìm folder khớp OID
            foreach (var yearDir in Directory.GetDirectories(uploadRoot))
                foreach (var monthDir in Directory.GetDirectories(yearDir))
                {
                    string oidDir = Path.Combine(monthDir, cleanOid);
                    string metaPath = Path.Combine(oidDir, "metadata.json");

                    if (!File.Exists(metaPath)) continue;

                    var json = File.ReadAllText(metaPath);
                    var list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(json) ?? new();
                    result.AddRange(list);
                }

            return result.OrderByDescending(f => f.UploadedAt).ToList();
        }
    }
}
