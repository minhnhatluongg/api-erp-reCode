using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// Webhook nhận callback từ hệ thống WinInvoice khi xuất HĐĐT thành công.
    /// Bảo vệ bằng InternalKey + Rate Limiting.
    /// </summary>
    [ApiController]
    [Route("api/webhook")]
    [EnableRateLimiting("webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookRepository _repo;
        private readonly ILogger<WebhookController> _logger;
        private readonly IConfiguration _config;

        public WebhookController(
            IWebhookRepository repo,
            ILogger<WebhookController> logger,
            IConfiguration config)
        {
            _repo   = repo;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Nhận callback khi hóa đơn điện tử đã xuất thành công.
        /// Cập nhật SignNumb = 201 cho JOB_00005/JB:010.
        ///
        /// Header yêu cầu: X-Internal-Key: {key}
        /// </summary>
        [HttpPost("invoice-exported")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> InvoiceExported([FromBody] InvoiceWebhookRequest request)
        {
            var clientIp  = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rawPayload = JsonSerializer.Serialize(request);

            // ── 1. Validate InternalKey
            var expectedKey = _config["Webhook:InternalKey"];
            var providedKey = Request.Headers["X-Internal-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("[Webhook] UNAUTHORIZED — IP={Ip}, OID={Oid}", clientIp, request.ContractOid);

                await _repo.WriteLogAsync(new WebhookLog
                {
                    EventType    = "INVOICE_EXPORTED",
                    ContractOid  = request.ContractOid ?? "",
                    ClientIp     = clientIp,
                    RawPayload   = rawPayload,
                    Status       = "BLOCKED",
                    ErrorMessage = "Invalid or missing X-Internal-Key",
                    CreatedAt    = DateTime.Now
                });

                return Unauthorized(new { message = "Invalid or missing X-Internal-Key." });
            }

            // ── 2. Validate request 
            if (string.IsNullOrWhiteSpace(request.ContractOid))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("ContractOid là bắt buộc.", 400));
            }

            string oid = request.ContractOid.Trim();

            _logger.LogInformation(
                "[Webhook] RECEIVED — OID={Oid}, InvoiceNo={No}, IP={Ip}",
                oid, request.InvoiceNo, clientIp);

            // ── 3. Xử lý: nâng SignNumb 101 → 201 
            (bool success, string message) result;
            try
            {
                result = await _repo.AdvanceInvoiceExportedAsync(oid, userId: "WEBHOOK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Webhook] EXCEPTION — OID={Oid}", oid);

                await _repo.WriteLogAsync(new WebhookLog
                {
                    EventType    = "INVOICE_EXPORTED",
                    ContractOid  = oid,
                    InvoiceNo    = request.InvoiceNo,
                    InvoiceSign  = request.InvoiceSign,
                    InvoiceDate  = request.InvoiceDate,
                    GovCode      = request.GovCode,
                    SourceAction = request.SourceAction,
                    RawPayload   = rawPayload,
                    ClientIp     = clientIp,
                    Status       = "FAILED",
                    ErrorMessage = ex.Message,
                    CreatedAt    = DateTime.Now
                });

                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }

            // ── 4. Ghi log kết quả 
            string status = result.success ? "SUCCESS"
                          : result.message.Contains("201") ? "DUPLICATE"
                          : "FAILED";

            await _repo.WriteLogAsync(new WebhookLog
            {
                EventType    = "INVOICE_EXPORTED",
                ContractOid  = oid,
                InvoiceNo    = request.InvoiceNo,
                InvoiceSign  = request.InvoiceSign,
                InvoiceDate  = request.InvoiceDate,
                GovCode      = request.GovCode,
                SourceAction = request.SourceAction,
                RawPayload   = rawPayload,
                ClientIp     = clientIp,
                Status       = status,
                ErrorMessage = result.success ? null : result.message,
                CreatedAt    = DateTime.Now
            });

            _logger.LogInformation(
                "[Webhook] {Status} — OID={Oid} | {Message}",
                status, oid, result.message);

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
            var clientIp   = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rawPayload = System.Text.Json.JsonSerializer.Serialize(request);

            // ── 1. Validate InternalKey ──────────────────────────────────────
            var expectedKey = _config["Webhook:InternalKey"];
            var providedKey = Request.Headers["X-Internal-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("[Webhook:RequestInvoice] UNAUTHORIZED — IP={Ip}, OID={Oid}",
                    clientIp, request.ContractOid);

                await _repo.WriteLogAsync(new WebhookLog
                {
                    EventType    = "REQUEST_INVOICE",
                    ContractOid  = request.ContractOid ?? "",
                    SourceAction = request.Note,
                    RawPayload   = rawPayload,
                    ClientIp     = clientIp,
                    Status       = "BLOCKED",
                    ErrorMessage = "Invalid or missing X-Internal-Key",
                    CreatedAt    = DateTime.Now
                });

                return Unauthorized(new { message = "Invalid or missing X-Internal-Key." });
            }

            // ── 2. Validate request ──────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.ContractOid))
                return BadRequest(ApiResponse<object>.ErrorResponse("ContractOid là bắt buộc.", 400));

            string oid    = request.ContractOid.Trim();
            string userId = string.IsNullOrWhiteSpace(request.RequestedBy)
                ? "WEBHOOK"
                : request.RequestedBy.Trim();

            _logger.LogInformation(
                "[Webhook:RequestInvoice] RECEIVED — OID={Oid}, RequestedBy={User}, IP={Ip}",
                oid, userId, clientIp);

            // ── 3. Xử lý ────────────────────────────────────────────────────
            (bool success, string message) result;
            try
            {
                result = await _repo.RequestInvoiceAsync(oid, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Webhook:RequestInvoice] EXCEPTION — OID={Oid}", oid);

                await _repo.WriteLogAsync(new WebhookLog
                {
                    EventType    = "REQUEST_INVOICE",
                    ContractOid  = oid,
                    SourceAction = request.Note,
                    RawPayload   = rawPayload,
                    ClientIp     = clientIp,
                    Status       = "FAILED",
                    ErrorMessage = ex.Message,
                    CreatedAt    = DateTime.Now
                });

                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }

            // ── 4. Ghi log ───────────────────────────────────────────────────
            string status = result.success ? "SUCCESS" : "FAILED";

            await _repo.WriteLogAsync(new WebhookLog
            {
                EventType    = "REQUEST_INVOICE",
                ContractOid  = oid,
                SourceAction = request.Note,
                RawPayload   = rawPayload,
                ClientIp     = clientIp,
                Status       = status,
                ErrorMessage = result.success ? null : result.message,
                CreatedAt    = DateTime.Now
            });

            _logger.LogInformation(
                "[Webhook:RequestInvoice] {Status} — OID={Oid} | {Message}",
                status, oid, result.message);

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
