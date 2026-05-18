using API.ERP_Portal_RC.Filters;
using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers.Admin
{
    /// <summary>Admin — đọc file log trên server, không cần remote.</summary>
    [Authorize]
    [TypeFilter(typeof(AdminAuthFilter))]
    [ApiController]
    [Route("api/admin/logs")]
    [Tags("🔐 Admin")]
    [ApiExplorerSettings(GroupName = "admin")]
    public class AdminLogController : ControllerBase
    {
        private readonly IAdminLogService _logService;

        public AdminLogController(IAdminLogService logService)
        {
            _logService = logService;
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Liệt kê file log trên server.</summary>
        /// <remarks>
        /// <b>Category hợp lệ:</b>
        /// <ul>
        ///   <li><c>econtract</c>   — Logs/EContract/ (EContract file logger)</li>
        ///   <li><c>externalapi</c> — Logs/ExternalApi/ (Webhook, IncomIntegration, ContractSign...)</li>
        ///   <li><c>stdout</c>      — logs/ (IIS stdout log)</li>
        /// </ul>
        /// </remarks>
        /// <param name="category">econtract | externalapi | stdout (tuỳ chọn, bỏ trống = tất cả)</param>
        /// <param name="date">Lọc theo ngày yyyy-MM-dd (tuỳ chọn)</param>
        [HttpGet("files")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult ListFiles(
            [FromQuery] string? category = null,
            [FromQuery] string? date     = null)
        {
            var files = _logService.ListFiles(category, date);
            return Ok(ApiResponse<object>.SuccessResponse(
                files,
                $"Tìm thấy {files.Count()} file log."));
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Đọc nội dung file log — phân trang theo dòng.</summary>
        /// <param name="category">econtract | externalapi | stdout</param>
        /// <param name="fileName">Tên file (VD: Webhook_2026-05-14.log)</param>
        /// <param name="page">Trang (mặc định: 1) — đọc từ cuối dùng trang cao nhất</param>
        /// <param name="pageSize">Số dòng/trang (mặc định: 100, tối đa 500)</param>
        [HttpGet("read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReadFile(
            [FromQuery] string category,
            [FromQuery] string fileName,
            [FromQuery] int    page     = 1,
            [FromQuery] int    pageSize = 100)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest(ApiResponse<object>.ErrorResponse("category và fileName là bắt buộc."));

            pageSize = Math.Min(Math.Max(pageSize, 1), 500);
            page     = Math.Max(page, 1);

            var result = await _logService.ReadFileAsync(category, fileName, page, pageSize);
            if (result == null)
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Không tìm thấy file '{fileName}' trong category '{category}'."));

            return Ok(ApiResponse<object>.SuccessResponse(result,
                $"Trang {result.Page}/{result.TotalPages} — {result.TotalLines} dòng tổng cộng."));
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Tìm kiếm từ khoá trong file log.</summary>
        /// <param name="category">econtract | externalapi | stdout</param>
        /// <param name="fileName">Tên file log</param>
        /// <param name="keyword">Từ khoá cần tìm (VD: OID, ERROR, userCode)</param>
        /// <param name="maxLines">Số dòng kết quả tối đa (mặc định 200)</param>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] string category,
            [FromQuery] string fileName,
            [FromQuery] string keyword,
            [FromQuery] int    maxLines = 200)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(ApiResponse<object>.ErrorResponse("keyword là bắt buộc."));

            maxLines = Math.Min(Math.Max(maxLines, 1), 1000);

            var lines = await _logService.SearchAsync(category, fileName, keyword, maxLines);
            var result = lines.ToList();

            return Ok(ApiResponse<object>.SuccessResponse(
                new { keyword, matchCount = result.Count, lines = result },
                $"Tìm thấy {result.Count} dòng chứa '{keyword}'."));
        }
    }
}
