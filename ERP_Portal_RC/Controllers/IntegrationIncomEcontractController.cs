using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IntegrationResultResponse = ERP_Portal_RC.Domain.Common.ApiResponse<ERP_Portal_RC.Application.DTOs.Integration_Incom.IntegrationResult>;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/integration/incom")]
    public class IntegrationIncomEcontractController : Controller
    {
        private readonly IIntegrationService        _integrationIncomService;
        private readonly ILogger<IntegrationIncomEcontractController> _logger;
        private readonly IncomIntegrationFileLogger _fileLogger;

        public IntegrationIncomEcontractController(
            IIntegrationService integrationIncomService,
            ILogger<IntegrationIncomEcontractController> logger,
            IncomIntegrationFileLogger fileLogger)
        {
            _integrationIncomService = integrationIncomService;
            _logger     = logger;
            _fileLogger = fileLogger;
        }

        /// <summary>
        /// Tích hợp đơn hàng EContract từ hệ thống bên ngoài (mini app INCOM).
        /// </summary>
        [HttpPost("econtract")]
        [Authorize]
        public async Task<IActionResult> ProcessEContractIntegration(
            [FromBody] EContractIntegrationRequestDto model)
        {
            var crtUser  = User.FindFirst("UserCode")?.Value ?? "unknown";
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var oid      = model?.OrderOID ?? "unknown";

            // ── Log REQUEST ──────────────────────────────────────────────────
            await _fileLogger.LogInboundAsync(
                correlationId: oid,
                endpoint:      "POST /api/integration/incom/econtract",
                status:        "RECEIVED",
                clientIp:      clientIp,
                payload: new
                {
                    OrderOID    = model?.OrderOID,
                    CusTax      = model?.CusTax,
                    CusName     = model?.CusName,
                    SampleID    = model?.SampleID,
                    IsCapBu     = model?.IsCapBu,
                    IsGiaHan    = model?.IsGiaHan,
                    DetailCount = model?.Details?.Count ?? 0,
                    CrtUser     = crtUser
                });

            _logger.LogInformation(
                "[Incom] RECEIVED — OID={OID} | CusTax={Tax} | User={User} | IP={Ip}",
                oid, model?.CusTax, crtUser, clientIp);

            // ── Xử lý ────────────────────────────────────────────────────────
            IntegrationResultResponse result;
            try
            {
                result = await _integrationIncomService.ProcessEContractIntegrationAsync(model, crtUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Incom] EXCEPTION — OID={OID}", oid);

                await _fileLogger.LogErrorAsync(
                    correlationId: oid,
                    endpoint:      "POST /api/integration/incom/econtract",
                    errorMessage:  ex.Message,
                    payload:       new { model?.OrderOID, model?.CusTax, crtUser });

                return StatusCode(500, IntegrationResultResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }

            // ── Log RESPONSE ─────────────────────────────────────────────────
            string logStatus = result.StatusCode == 200 ? "SUCCESS" : "FAILED";

            await _fileLogger.LogInboundAsync(
                correlationId: oid,
                endpoint:      "POST /api/integration/incom/econtract",
                status:        logStatus,
                clientIp:      clientIp,
                message:       $"HTTP {result.StatusCode} — {result.Message}");

            _logger.LogInformation(
                "[Incom] {Status} — OID={OID} | HTTP {Code}",
                logStatus, oid, result.StatusCode);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                409 => Conflict(result),
                _   => StatusCode(500, result)
            };
        }
    }
}
