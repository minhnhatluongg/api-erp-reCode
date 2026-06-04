using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.InvoiceTemplate;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// API mẫu hóa đơn (XSLT).
    /// Dời từ TVAN_WEB_API/InvoiceTemplateController sang ERP_Portal_RC.
    /// </summary>
    [ApiController]
    [Route("api/invoice/templates")]
    [Produces("application/json")]
    public class InvoiceTemplateController : ControllerBase
    {
        private readonly ILogger<InvoiceTemplateController> _logger;
        private readonly IInvoiceTemplateService _templateService;

        public InvoiceTemplateController(
            ILogger<InvoiceTemplateController> logger,
            IInvoiceTemplateService templateService)
        {
            _logger = logger;
            _templateService = templateService;
        }

        private string GetTraceId() => HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        /// <summary>
        /// Lấy toàn bộ mẫu hóa đơn đang active (cho combobox).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceTemplateListItemDto>>), 200)]
        public async Task<IActionResult> GetAllTemplates()
        {
            var traceId = GetTraceId();
            Response.Headers["X-Correlation-Id"] = traceId;

            try
            {
                var data = await _templateService.GetAllTemplatesAsync();

                _logger.LogInformation("[InvoiceTemplate] GetAll OK, traceId={TraceId}", traceId);

                return Ok(ApiResponse<IEnumerable<InvoiceTemplateListItemDto>>.SuccessResponse(
                    data, "Lấy danh sách mẫu hóa đơn thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InvoiceTemplate] GetAll error, traceId={TraceId}", traceId);
                return StatusCode(500, ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Lấy nội dung XSLT thô theo TemplateID.
        /// </summary>
        [HttpGet("{templateId:int}")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceTemplateXsltDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetTemplateById(int templateId)
        {
            var traceId = GetTraceId();
            Response.Headers["X-Correlation-Id"] = traceId;

            try
            {
                var dto = await _templateService.GetTemplateXsltAsync(templateId);
                if (dto == null)
                {
                    _logger.LogWarning("[InvoiceTemplate] Not found ID={Id}", templateId);
                    return NotFound(ApiResponse.ErrorResponse("Không tìm thấy mẫu hóa đơn với ID này.", 404));
                }

                return Ok(ApiResponse<InvoiceTemplateXsltDto>.SuccessResponse(dto, "Lấy nội dung mẫu thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InvoiceTemplate] GetById={Id} error, traceId={TraceId}", templateId, traceId);
                return StatusCode(500, ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Lấy mẫu hóa đơn (kèm XSLT) theo TemplateCode.
        /// </summary>
        [HttpGet("bycode/{templateCode}")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceTemplateXsltDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetTemplateByCode(string templateCode)
        {
            var traceId = GetTraceId();
            Response.Headers["X-Correlation-Id"] = traceId;

            if (string.IsNullOrWhiteSpace(templateCode))
            {
                return BadRequest(ApiResponse.ErrorResponse("Thiếu mã mẫu (template code)."));
            }

            try
            {
                var dto = await _templateService.GetTemplateByCodeAsync(templateCode);
                if (dto == null)
                {
                    _logger.LogWarning("[InvoiceTemplate] Not found Code={Code}", templateCode);
                    return NotFound(ApiResponse.ErrorResponse("Không tìm thấy mẫu hóa đơn với Code này.", 404));
                }

                return Ok(ApiResponse<InvoiceTemplateXsltDto>.SuccessResponse(dto, "Lấy nội dung mẫu thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InvoiceTemplate] GetByCode={Code} error, traceId={TraceId}", templateCode, traceId);
                return StatusCode(500, ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Dò cấu hình ẩn/hiện + viền từ một file XSLT người dùng tự upload (để FE tick checkbox đúng 100%).
        /// </summary>
        [HttpPost("detect-config")]
        [ProducesResponseType(typeof(ApiResponse<AdjustConfigDto>), 200)]
        public IActionResult DetectConfig([FromBody] DetectTemplateConfigRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.RawXslt))
                return BadRequest(ApiResponse.ErrorResponse("Thiếu nội dung XSLT (rawXslt)."));

            var cfg = _templateService.DetectConfig(body.RawXslt);
            return Ok(ApiResponse<AdjustConfigDto>.SuccessResponse(cfg, "Dò cấu hình mẫu thành công."));
        }
    }
}
