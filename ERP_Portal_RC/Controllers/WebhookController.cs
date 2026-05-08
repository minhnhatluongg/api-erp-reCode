using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// Webhook nhận callback từ hệ thống WinInvoice + ERP khi xuất HĐĐT.
    /// Bảo vệ bằng InternalKey + Rate Limiting.
    /// Log ra file: Logs/ExternalApi/Webhook_{date}.log
    /// </summary>
    [ApiController]
    [Route("api/webhook")]
    [EnableRateLimiting("webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookRepository  _repo;
        private readonly ILogger<WebhookController> _logger;
        private readonly IConfiguration      _config;
        private readonly WebhookFileLogger   _fileLogger;

        public WebhookController(
            IWebhookRepository repo,
            ILogger<WebhookController> logger,
            IConfiguration config,
            WebhookFileLogger fileLogger)
        {
            _repo       = repo;
            _logger     = logger;
            _config     = config;
            _fileLogger = fileLogger;
        }

        /// <summary>
        /// Nhận callback khi hóa đơn điện tử đã xuất thành công.
        /// Cập nhật SignNumb = 301 cho JOB_00005/JB:010.
        ///
        /// Header yêu cầu: X-Internal-Key: {key}
        /// </summary>
        [HttpPost("invoice-exported")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> InvoiceExported([FromBody] InvoiceWebhookRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // ── 1. Validate InternalKey ──────────────────────────────────────
            var expectedKey = _config["Webhook:InternalKey"];
            var providedKey = Request.Headers["X-Internal-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                await _fileLogger.LogInboundAsync(
                    correlationId: request.ContractOid ?? "unknown",
                    endpoint: "invoice-exported",
                    status: "BLOCKED",
                    clientIp: clientIp,
                    payload: request,
                    message: "Invalid or missing X-Internal-Key");

                return Unauthorized(new { message = "Invalid or missing X-Internal-Key." });
            }

            if (string.IsNullOrWhiteSpace(request.ContractOid))
                return BadRequest(ApiResponse<object>.ErrorResponse("ContractOid là bắt buộc.", 400));

            string oid = request.ContractOid.Trim();

            await _fileLogger.LogInboundAsync(
                correlationId: oid,
                endpoint: "invoice-exported",
                status: "RECEIVED",
                clientIp: clientIp,
                payload: request);

            // ── 2. Xử lý: nâng SignNumb 101 → 201 ───────────────────────────
            (bool success, string message) result;
            try
            {
                result = await _repo.AdvanceInvoiceExportedAsync(oid, userId: "WEBHOOK");
            }
            catch (Exception ex)
            {
                await _fileLogger.LogErrorAsync(oid, "invoice-exported", ex.Message, request);
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }

            string status = result.success ? "SUCCESS"
                          : result.message.Contains("201") ? "DUPLICATE"
                          : "FAILED";

            await _fileLogger.LogInboundAsync(
                correlationId: oid,
                endpoint: "invoice-exported",
                status: status,
                clientIp: clientIp,
                message: result.message);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { oid, status, message = result.message },
                result.message));
        }

        /// <summary>
        /// Kế toán click "Xem hóa đơn nháp" trong ERP → đảm bảo job JOB_00005/JB:010
        /// tồn tại và đang ở SignNumb = 101 (Đã có yêu cầu xuất hóa đơn).
        ///
        /// Logic:
        ///   - Job chưa có       → tạo mới (từ dữ liệu EContracts) rồi đẩy 0→101.
        ///   - Job có, SignNumb=0 → đẩy 0→101.
        ///   - Job có, SignNumb=101 hoặc 201 → idempotent, trả 200 luôn.
        ///
        /// Header: X-Internal-Key: {key}
        /// </summary>
        [HttpPost("request-invoice")]
        public async Task<IActionResult> RequestInvoice([FromBody] RequestInvoiceWebhookRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // ── 1. Validate InternalKey ──────────────────────────────────────
            var expectedKey = _config["Webhook:InternalKey"];
            var providedKey = Request.Headers["X-Internal-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                await _fileLogger.LogInboundAsync(
                    correlationId: request.ContractOid ?? "unknown",
                    endpoint: "request-invoice",
                    status: "BLOCKED",
                    clientIp: clientIp,
                    payload: request,
                    message: "Invalid or missing X-Internal-Key");

                return Unauthorized(new { message = "Invalid or missing X-Internal-Key." });
            }

            if (string.IsNullOrWhiteSpace(request.ContractOid))
                return BadRequest(ApiResponse<object>.ErrorResponse("ContractOid là bắt buộc.", 400));

            string oid    = request.ContractOid.Trim();
            string userId = string.IsNullOrWhiteSpace(request.RequestedBy)
                ? "WEBHOOK" : request.RequestedBy.Trim();

            await _fileLogger.LogInboundAsync(
                correlationId: oid,
                endpoint: "request-invoice",
                status: "RECEIVED",
                clientIp: clientIp,
                payload: new { request.ContractOid, request.RequestedBy, request.Note });

            // ── 2. Xử lý ────────────────────────────────────────────────────
            (bool success, string message) result;
            try
            {
                result = await _repo.RequestInvoiceAsync(oid, userId);
            }
            catch (Exception ex)
            {
                await _fileLogger.LogErrorAsync(oid, "request-invoice", ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }

            string status = result.success ? "SUCCESS" : "FAILED";

            await _fileLogger.LogInboundAsync(
                correlationId: oid,
                endpoint: "request-invoice",
                status: status,
                clientIp: clientIp,
                message: result.message);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { oid, status, message = result.message },
                result.message));
        }

        /// <summary>
        /// Health check (không cần key).
        /// </summary>
        [HttpGet("health")]
        [DisableRateLimiting]
        public IActionResult Health()
            => Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }
}
