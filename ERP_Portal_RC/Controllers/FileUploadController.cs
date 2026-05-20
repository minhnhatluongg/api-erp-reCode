using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileValidationService _validator;
        private readonly IFileStorageService _storage;
        private readonly IChunkUploadService _chunkService;
        private readonly FileUploadConfig _config;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(
            IFileValidationService validator,
            IFileStorageService storage,
            IChunkUploadService chunkService,
            IOptions<FileUploadConfig> config,
            IConfiguration configuration,
            ILogger<FileUploadController> logger)
        {
            _validator = validator;
            _storage = storage;
            _chunkService = chunkService;
            _configuration = configuration;
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// Upload file đính kèm vào hợp đồng.
        /// Yêu cầu đăng nhập (JWT). UserCode được lấy từ token để gắn vào metadata,
        /// đảm bảo user chỉ tải file vào hợp đồng của mình.
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        [DisableRequestSizeLimit]
        [ProducesResponseType(typeof(ApiResponse<List<ContractFileMetadata>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Upload(
            IFormFileCollection files,
            [FromQuery] string oid,
            CancellationToken ct)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrWhiteSpace(userCode))
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Không xác định được UserCode từ token.", 401));

            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            if (files is null || files.Count == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Không có file nào."));

            var uploaded = new List<ContractFileMetadata>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                // Validate bảo mật
                var (isValid, error) = await _validator.ValidateAsync(file, ct);
                if (!isValid)
                {
                    errors.Add($"{file.FileName}: {error}");
                    continue;
                }

                // File lớn hơn ChunkSizeBytes → yêu cầu dùng chunked API
                if (file.Length > _config.ChunkSizeBytes)
                {
                    errors.Add($"{file.FileName}: File quá lớn, vui lòng dùng API upload-chunk.");
                    continue;
                }

                var meta = await _storage.UploadFileAsync(file, oid, userCode, ct);
                if (meta != null)
                {
                    uploaded.Add(meta);
                    _logger.LogInformation(
                        "[Upload] User={U} OID={O} File={F} Size={S}",
                        userCode, oid, meta.OriginalName, meta.SizeBytes);
                }
            }

            if (errors.Count > 0 && uploaded.Count == 0)
                return BadRequest(ApiResponse<List<ContractFileMetadata>>.ErrorResponse(
                    string.Join("; ", errors), statusCode: 400, errors: errors));

            string message = errors.Count > 0
                ? $"Upload một phần thành công ({uploaded.Count}). Lỗi: {string.Join("; ", errors)}"
                : $"Upload thành công {uploaded.Count} file.";

            return Ok(ApiResponse<List<ContractFileMetadata>>.SuccessResponse(uploaded, message));
        }

        // ── Endpoint 2: Upload từng chunk ──────────────────────────────────────
        /// <summary>
        /// Client cắt file thành N chunk, gọi endpoint này cho mỗi chunk.
        /// Required headers: X-Session-Id, X-Chunk-Index, X-Total-Chunks, X-File-Name
        /// </summary>
        [HttpPost("upload-chunk")]
        [Authorize]
        [DisableRequestSizeLimit]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadChunk(
            IFormFile chunk,
            [FromHeader(Name = "X-Session-Id")] string sessionId,
            [FromHeader(Name = "X-Chunk-Index")] int chunkIndex,
            [FromHeader(Name = "X-Total-Chunks")] int totalChunks,
            [FromHeader(Name = "X-File-Name")] string fileName,
            [FromQuery] string oid,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu Session-Id hoặc OID."));

            if (chunk is null || chunk.Length == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Chunk rỗng."));

            await _chunkService.SaveChunkAsync(sessionId, chunkIndex, chunk, ct);

            _logger.LogInformation("[Chunk] Session={S} Chunk={I}/{T}", sessionId, chunkIndex + 1, totalChunks);

            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: $"Chunk {chunkIndex + 1}/{totalChunks} đã nhận."));
        }

        [HttpPost("merge-chunks")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MergeChunks(
            [FromBody] MergeChunksRequest req,
            CancellationToken ct)
        {
            try
            {
                string relativePath = await _chunkService.MergeChunksAsync(
                    req.SessionId, req.FileName, req.Oid, req.TotalChunks, ct);

                string url = BuildUrl(relativePath);
                _logger.LogInformation("[Merge] Session={S} -> {U}", req.SessionId, url);

                return Ok(ApiResponse<string>.SuccessResponse(url, "Ghép file thành công."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Merge] Session={S} lỗi", req.SessionId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Lỗi khi ghép file.", statusCode: 500));
            }
        }

        /// <summary>
        /// Lấy danh sách file đính kèm theo OID hợp đồng.
        /// Ví dụ OID gốc: 000642/260415:104748280
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse<ContractFilesResponse>), StatusCodes.Status200OK)]
        public IActionResult GetFilesByOid(
            [FromQuery] string oid,
            [FromQuery] int year,
            [FromQuery] int month)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            var result = _storage.GetFilesByOid(oid, year, month);
            return Ok(ApiResponse<ContractFilesResponse>.SuccessResponse(result));
        }

        [HttpPost("rebuild-metadata")]
        public async Task<IActionResult> RebuildMetadata(
            [FromQuery] string oid,
            [FromQuery] int year,
            [FromQuery] int month,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            var result = await _storage.RebuildMetadata(oid, year, month, ct);
            return Ok(ApiResponse<object>.SuccessResponse(result, "Rebuild metadata thành công."));
        }

        [HttpGet("listImage")]
        public IActionResult GetFilesByOid([FromQuery] string oid)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            var files = _storage.GetAllFilesByOid(oid);
            var response = new ContractFilesResponse
            {
                Oid = oid,
                TotalFiles = files.Count,
                Files = files
            };
            return Ok(ApiResponse<ContractFilesResponse>.SuccessResponse(response));
        }

        // ── API: User xem TẤT CẢ file của mình (Attachments + KyThuatMau) ──────
        /// <summary>
        /// Lấy toàn bộ file của user đang đăng nhập:
        ///   - Attachments: file đính kèm đã upload trong tất cả tháng
        ///   - KyThuatMau: file mẫu đã upload
        /// Filter theo UserCode lấy từ JWT token (không cần truyền userCode).
        /// Có thể filter thêm theo năm (year), mặc định lấy tất cả.
        /// </summary>
        [HttpGet("my-files")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ContractAllFilesResponse>), StatusCodes.Status200OK)]
        public IActionResult GetMyFiles([FromQuery] int? year = null)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrWhiteSpace(userCode))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không xác định được UserCode từ token.", 401));

            string uploadRoot  = _configuration["FileUpload:PhysicalRootPath"]
                              ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments";
            string kyThuatRoot = _configuration["FileUpload:KyThuatMauPath"]
                              ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\KyThuatMau";
            string baseUrl     = (_configuration["FileUpload:BaseUrl"] ?? "").TrimEnd('/');

            var response = new ContractAllFilesResponse { Oid = userCode };

            // ── Phần 1: Attachments — group theo folder ──────────────────────
            if (Directory.Exists(uploadRoot))
            {
                var yearDirs = Directory.GetDirectories(uploadRoot);
                if (year.HasValue)
                    yearDirs = yearDirs
                        .Where(d => Path.GetFileName(d) == year.Value.ToString())
                        .ToArray();

                foreach (var yearDir in yearDirs)
                    foreach (var monthDir in Directory.GetDirectories(yearDir))
                        foreach (var oidDir in Directory.GetDirectories(monthDir))
                        {
                            var folderName = Path.GetFileName(oidDir);
                            if (!folderName.StartsWith(userCode + "_",
                                StringComparison.OrdinalIgnoreCase)) continue;

                            var metaPath = Path.Combine(oidDir, "metadata.json");
                            if (!System.IO.File.Exists(metaPath)) continue;

                            var list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(
                                System.IO.File.ReadAllText(metaPath)) ?? new();

                            if (!list.Any()) continue;

                            // Group vào 1 folder entry
                            response.Attachments.Add(new ContractFolderGroup
                            {
                                Folder = folderName,
                                Files  = list
                                    .OrderByDescending(f => f.UploadedAt)
                                    .Select(f => new ContractFileItem
                                    {
                                        FileName     = f.FileName,
                                        OriginalName = f.OriginalName,
                                        Url          = f.Url,
                                        Extension    = f.Extension,
                                        SizeBytes    = f.SizeBytes,
                                        UploadedAt   = f.UploadedAt,
                                        Category     = "attach"
                                    }).ToList()
                            });
                        }
            }

            // ── Phần 2: KyThuatMau — group theo folder ───────────────────────
            if (Directory.Exists(kyThuatRoot))
            {
                foreach (var oidDir in Directory.GetDirectories(kyThuatRoot))
                {
                    var folderName = Path.GetFileName(oidDir);
                    if (!folderName.StartsWith(userCode,
                        StringComparison.OrdinalIgnoreCase)) continue;

                    if (year.HasValue)
                    {
                        var folderDate = Directory.GetCreationTime(oidDir);
                        if (folderDate.Year != year.Value) continue;
                    }

                    var templateFiles = new List<ContractFileItem>();

                    foreach (var filePath in Directory.GetFiles(oidDir, "*.*",
                        SearchOption.AllDirectories))
                    {
                        var info = new FileInfo(filePath);
                        if (info.Name.Equals("metadata.json",
                            StringComparison.OrdinalIgnoreCase)) continue;

                        var relFromUploads = Path.GetRelativePath(uploadRoot, filePath)
                            .Replace("\\", "/");
                        var ext = info.Extension.ToLowerInvariant();

                        templateFiles.Add(new ContractFileItem
                        {
                            FileName     = info.Name,
                            OriginalName = info.Name,
                            Url          = $"{baseUrl.TrimEnd('/')}/uploads/{relFromUploads}",
                            Extension    = ext,
                            SizeBytes    = info.Length,
                            UploadedAt   = info.CreationTime,
                            Category     = ext == ".pdf" ? "template-view" : "template-download"
                        });
                    }

                    if (!templateFiles.Any()) continue;

                    response.Templates.Add(new ContractFolderGroup
                    {
                        Folder = folderName,
                        Files  = templateFiles.OrderByDescending(f => f.UploadedAt).ToList()
                    });
                }
            }

            // Sắp xếp nhóm theo folder mới nhất
            response.Attachments = response.Attachments
                .OrderByDescending(g => g.Files.Max(f => f.UploadedAt)).ToList();
            response.Templates   = response.Templates
                .OrderByDescending(g => g.Files.Max(f => f.UploadedAt)).ToList();

            return Ok(ApiResponse<ContractAllFilesResponse>.SuccessResponse(
                response,
                $"User {userCode}: {response.TotalAttachments} file đính kèm trong {response.Attachments.Count} HĐ, " +
                $"{response.TotalTemplates} file mẫu trong {response.Templates.Count} HĐ."));
        }

        // ── API: User xem TỔNG QUAN file đã upload (dashboard) ────────────────
        /// <summary>
        /// Lấy báo cáo tổng hợp về toàn bộ file user đang đăng nhập đã upload:
        ///   - Tổng số file, tổng dung lượng
        ///   - Số hợp đồng có file
        ///   - Group theo hợp đồng (OID), theo extension, theo tháng
        ///   - 10 file upload gần nhất
        /// UserCode lấy từ JWT token. Có thể filter theo năm (year).
        /// </summary>
        [HttpGet("my-files/summary")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserFilesSummaryResponse>), StatusCodes.Status200OK)]
        public IActionResult GetMyFilesSummary([FromQuery] int? year = null)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrWhiteSpace(userCode))
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Không xác định được UserCode từ token.", 401));

            string uploadRoot = _configuration["FileUpload:PhysicalRootPath"]
                              ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments";

            var allFiles = new List<ContractFileMetadata>();

            if (Directory.Exists(uploadRoot))
            {
                var yearDirs = Directory.GetDirectories(uploadRoot);
                if (year.HasValue)
                    yearDirs = yearDirs
                        .Where(d => Path.GetFileName(d) == year.Value.ToString())
                        .ToArray();

                foreach (var yearDir in yearDirs)
                    foreach (var monthDir in Directory.GetDirectories(yearDir))
                        foreach (var oidDir in Directory.GetDirectories(monthDir))
                        {
                            var folderName = Path.GetFileName(oidDir);
                            if (!folderName.StartsWith(userCode + "_",
                                StringComparison.OrdinalIgnoreCase)) continue;

                            var metaPath = Path.Combine(oidDir, "metadata.json");
                            if (!System.IO.File.Exists(metaPath)) continue;

                            try
                            {
                                var list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(
                                    System.IO.File.ReadAllText(metaPath)) ?? new();
                                allFiles.AddRange(list);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex,
                                    "[Summary] Lỗi đọc metadata {Path}", metaPath);
                            }
                        }
            }

            // ── Build response ────────────────────────────────────────────
            var response = new UserFilesSummaryResponse
            {
                UserCode       = userCode,
                TotalFiles     = allFiles.Count,
                TotalSizeBytes = allFiles.Sum(f => f.SizeBytes),
            };

            response.TotalSizeFormatted = FormatBytes(response.TotalSizeBytes);

            response.ByContract = allFiles
                .GroupBy(f => f.Oid)
                .Select(g => new ContractGroup
                {
                    Oid            = g.Key,
                    FileCount      = g.Count(),
                    SizeBytes      = g.Sum(f => f.SizeBytes),
                    LastUploadedAt = g.Max(f => f.UploadedAt),
                })
                .OrderByDescending(c => c.LastUploadedAt)
                .ToList();

            response.TotalContracts = response.ByContract.Count;

            response.ByExtension = allFiles
                .GroupBy(f => string.IsNullOrEmpty(f.Extension) ? "(no-ext)" : f.Extension.ToLowerInvariant())
                .Select(g => new ExtensionGroup
                {
                    Extension = g.Key,
                    FileCount = g.Count(),
                    SizeBytes = g.Sum(f => f.SizeBytes),
                })
                .OrderByDescending(e => e.FileCount)
                .ToList();

            response.ByMonth = allFiles
                .GroupBy(f => f.UploadedAt.ToString("yyyy-MM"))
                .Select(g => new MonthGroup
                {
                    Month     = g.Key,
                    FileCount = g.Count(),
                    SizeBytes = g.Sum(f => f.SizeBytes),
                })
                .OrderByDescending(m => m.Month)
                .ToList();

            response.RecentFiles = allFiles
                .OrderByDescending(f => f.UploadedAt)
                .Take(10)
                .ToList();

            return Ok(ApiResponse<UserFilesSummaryResponse>.SuccessResponse(
                response,
                $"User {userCode}: {response.TotalFiles} file, {response.TotalContracts} hợp đồng, {response.TotalSizeFormatted}."));
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.##} {units[unit]}";
        }

        // ── API 1: User xem file đính kèm hợp đồng ────────────────────────────
        /// <summary>
        /// Lấy toàn bộ file đính kèm của hợp đồng (do user upload).
        /// Scan tất cả năm/tháng — không cần truyền year/month.
        /// Yêu cầu đăng nhập (JWT).
        /// </summary>
        [HttpGet("contract-files")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ContractFilesResponse>), StatusCodes.Status200OK)]
        public IActionResult GetContractFiles([FromQuery] string oid)
        {
            if (string.IsNullOrWhiteSpace(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            string cleanOid  = oid.Replace("/", "_").Replace(":", "_").Trim();
            string uploadRoot = _configuration["FileUpload:PhysicalRootPath"]
                             ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments";
            string baseUrl   = _configuration["FileUpload:BaseUrl"] ?? "";

            var files = new List<ContractFileMetadata>();

            if (Directory.Exists(uploadRoot))
            {
                foreach (var yearDir in Directory.GetDirectories(uploadRoot))
                    foreach (var monthDir in Directory.GetDirectories(yearDir))
                    {
                        var metaPath = Path.Combine(monthDir, cleanOid, "metadata.json");
                        if (!System.IO.File.Exists(metaPath)) continue;

                        var list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(
                            System.IO.File.ReadAllText(metaPath)) ?? new();
                        files.AddRange(list);
                    }
            }

            var result = new ContractFilesResponse
            {
                Oid        = oid,
                TotalFiles = files.Count,
                Files      = files.OrderByDescending(f => f.UploadedAt).ToList()
            };

            return Ok(ApiResponse<ContractFilesResponse>.SuccessResponse(
                result, $"Tìm thấy {files.Count} file đính kèm."));
        }

        // ── API 2: Kỹ thuật xem tất cả file (Attachments + KyThuatMau) ────────
        /// <summary>
        /// Lấy TẤT CẢ file liên quan đến hợp đồng dành cho kỹ thuật:
        ///   - File đính kèm do user upload (Attachments folder)
        ///   - File mẫu kỹ thuật (KyThuatMau folder)
        /// Yêu cầu đăng nhập (JWT).
        /// </summary>
        [HttpGet("technical-files")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ContractAllFilesResponse>), StatusCodes.Status200OK)]
        public IActionResult GetTechnicalFiles([FromQuery] string oid)
        {
            if (string.IsNullOrWhiteSpace(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            string cleanOid      = oid.Replace("/", "_").Replace(":", "_").Trim();
            string uploadRoot    = _configuration["FileUpload:PhysicalRootPath"]
                                ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments";
            string kyThuatRoot   = _configuration["FileUpload:KyThuatMauPath"]
                                ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\KyThuatMau";
            string baseUrl       = (_configuration["FileUpload:BaseUrl"] ?? "").TrimEnd('/');

            var response = new ContractAllFilesResponse { Oid = oid };

            // ── Phần 1: Attachments — group theo folder ──────────────────────
            if (Directory.Exists(uploadRoot))
            {
                foreach (var yearDir in Directory.GetDirectories(uploadRoot))
                    foreach (var monthDir in Directory.GetDirectories(yearDir))
                    {
                        var metaPath = Path.Combine(monthDir, cleanOid, "metadata.json");
                        if (!System.IO.File.Exists(metaPath)) continue;

                        var list = JsonSerializer.Deserialize<List<ContractFileMetadata>>(
                            System.IO.File.ReadAllText(metaPath)) ?? new();

                        if (!list.Any()) continue;

                        response.Attachments.Add(new ContractFolderGroup
                        {
                            Folder = cleanOid,
                            Files  = list.OrderByDescending(f => f.UploadedAt)
                                        .Select(f => new ContractFileItem
                                        {
                                            FileName     = f.FileName,
                                            OriginalName = f.OriginalName,
                                            Url          = f.Url,
                                            Extension    = f.Extension,
                                            SizeBytes    = f.SizeBytes,
                                            UploadedAt   = f.UploadedAt,
                                            Category     = "attach"
                                        }).ToList()
                        });
                    }
            }

            // ── Phần 2: KyThuatMau folder — group theo folder ────────────────
            // KyThuatMau folder name: cleanOid không có dấu _ (xóa / và :)
            string cleanOidKT = oid.Replace("/", "").Replace(":", "").Trim();
            var kyThuatDir    = Path.Combine(kyThuatRoot, cleanOidKT);

            if (Directory.Exists(kyThuatDir))
            {
                var templateFiles = new List<ContractFileItem>();
                foreach (var filePath in Directory.GetFiles(kyThuatDir, "*.*",
                    SearchOption.AllDirectories))
                {
                    var info = new FileInfo(filePath);
                    if (info.Name.Equals("metadata.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var relFromUploads2 = Path.GetRelativePath(uploadRoot, filePath)
                        .Replace("\\", "/");
                    var ext2 = info.Extension.ToLowerInvariant();

                    templateFiles.Add(new ContractFileItem
                    {
                        FileName     = info.Name,
                        OriginalName = info.Name,
                        Url          = $"{baseUrl.TrimEnd('/')}/uploads/{relFromUploads2}",
                        Extension    = ext2,
                        SizeBytes    = info.Length,
                        UploadedAt   = info.CreationTime,
                        Category     = ext2 == ".pdf" ? "template-view" : "template-download"
                    });
                }

                if (templateFiles.Any())
                    response.Templates.Add(new ContractFolderGroup
                    {
                        Folder = cleanOidKT,
                        Files  = templateFiles.OrderByDescending(f => f.UploadedAt).ToList()
                    });
            }

            return Ok(ApiResponse<ContractAllFilesResponse>.SuccessResponse(
                response,
                $"Tìm thấy {response.TotalAttachments} file đính kèm, {response.TotalTemplates} file mẫu."));
        }

        // ── Helper ─────────────────────────────────────────────────────────────
        private string BuildUrl(string relativePath)
        {
            var path = relativePath.Replace("\\", "/").TrimStart('/');
            if (path.StartsWith("uploads/"))
            {
                path = path.Substring("uploads/".Length);
            }

            return $"{_config.BaseUrl.TrimEnd('/')}/uploads/{path}";
        }

    }
}
