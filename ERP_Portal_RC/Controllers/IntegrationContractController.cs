using System;
using System.Threading.Tasks;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// Controller cho các API tích hợp NGOÀI (Landingpage + n8n + Zalo Mini App + Email + Smax.ai).
    /// Tách riêng khỏi EcontractController để dễ kiểm soát quyền truy cập, rate limit,
    /// monitor riêng cho luồng automation và không phá vỡ API nội bộ /api/Econtract/...
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/integration")]
    public class IntegrationContractController : ControllerBase
    {
        private readonly IIntegrationContractService _service;
        private readonly ILogger<IntegrationContractController> _logger;

        public IntegrationContractController(
            IIntegrationContractService service,
            ILogger<IntegrationContractController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Tạo MỚI hoặc GIA HẠN hợp đồng từ luồng tích hợp.
        ///
        /// Khác /api/Econtract/save-and-approve:
        ///   - Lightweight body: chỉ field cần thiết (MST, tên KH, ItemID gói, ký hiệu/số HĐ, sale).
        ///   - Server tự fill Bên B từ Check_OwnerContract (CmpnID=26).
        ///   - Server tự resolve ItemName/ItemPrice/ItemUnit từ ItemID qua wspProducts_Tool_v25.
        ///   - Một endpoint duy nhất xử lý cả đơn mới và đơn gia hạn (qua field IsGiaHan).
        /// </summary>
        [HttpPost("quick-create")]
        [ProducesResponseType(typeof(ApiResponse<QuickCreateContractResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> QuickCreate([FromBody] QuickCreateContractRequest request)
        {
            var traceId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N");
            Response.Headers["X-Correlation-Id"] = traceId;

            if (request == null)
                return BadRequest(ApiResponse<object>.ErrorResponse("Body request không được rỗng.", 400));

            // UserCode lấy từ token để dùng làm fallback cho SaleEmID và Crt_User (mặc định).
            var callerUserCode = User.FindFirst("UserCode")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(callerUserCode) && string.IsNullOrWhiteSpace(request.SaleEmID))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Không tìm thấy UserCode trong Token và request cũng không truyền SaleEmID.", 401));
            }

            try
            {
                var result = await _service.QuickCreateAsync(request, callerUserCode);
                return result.Success
                    ? Ok(result)
                    : StatusCode(result.StatusCode == 0 ? 400 : result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{TraceId}] QuickCreate UNHANDLED", traceId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message, 500));
            }
        }

        /// <summary>
        /// Tạo "Hợp đồng Ma" — hợp đồng bình thường (có SaleEmID thật) nhưng server tự chèn
        /// 2 gói miễn phí (gói hóa đơn + gói truyền nhận, giá 0) để hợp thức hóa dải hóa đơn
        /// qua hệ thống hóa đơn.
        ///
        /// Caller CHỈ cần gửi: mẫu số (InvSample), ký hiệu (InvSign), từ số (InvFrom), đến số (InvTo).
        /// Sale LOT / ItemID 2 gói / Bên A mặc định lấy từ appsettings → GhostContract
        /// (có thể override qua request).
        /// </summary>
        [HttpPost("ghost-contract")]
        [ProducesResponseType(typeof(ApiResponse<GhostContractResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> CreateGhostContract([FromBody] GhostContractRequest request)
        {
            var traceId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N");
            Response.Headers["X-Correlation-Id"] = traceId;

            if (request == null)
                return BadRequest(ApiResponse<object>.ErrorResponse("Body request không được rỗng.", 400));

            // UserCode từ token — fallback cho SaleEmID nếu request & config đều trống.
            var callerUserCode = User.FindFirst("UserCode")?.Value ?? string.Empty;

            try
            {
                var result = await _service.CreateGhostContractAsync(request, callerUserCode);
                return result.Success
                    ? Ok(result)
                    : StatusCode(result.StatusCode == 0 ? 400 : result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{TraceId}] CreateGhostContract UNHANDLED", traceId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message, 500));
            }
        }
    }
}
