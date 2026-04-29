using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;
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

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(
            IFormFileCollection files,
            [FromQuery] string oid,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID."));

            if (files is null || files.Count == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Không có file nào."));

            var fileLinks = new List<string>();
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

                string relativePath = await _storage.UploadFileAsync(file, oid, ct);
                fileLinks.Add(BuildUrl(relativePath));
            }

            if (errors.Count > 0 && fileLinks.Count == 0)
                return BadRequest(ApiResponse<List<string>>.ErrorResponse(
                    string.Join("; ", errors), statusCode: 400, errors: errors));

            string message = errors.Count > 0
                ? $"Upload một phần thành công. Lỗi: {string.Join("; ", errors)}"
                : "Upload thành công.";

            return Ok(ApiResponse<List<string>>.SuccessResponse(fileLinks, message));
        }

        // ── Endpoint 2: Upload từng chunk ──────────────────────────────────────
        /// <summary>
        /// Client cắt file thành N chunk, gọi endpoint này cho mỗi chunk.
        /// Required headers: X-Session-Id, X-Chunk-Index, X-Total-Chunks, X-File-Name
        /// </summary>
        [HttpPost("upload-chunk")]
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
