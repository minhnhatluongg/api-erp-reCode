using System;
using System.Threading.Tasks;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// Các API hỗ trợ form Tạo / Gia hạn đơn hàng (đã port từ TVAN_WEB_API.OdooOrdersController).
    /// Hai endpoint được copy sang ERPRC:
    ///   - GET /api/odoo/orders/get-products  → danh sách gói dịch vụ
    ///   - GET /api/odoo/orders/owner-info    → thông tin công ty chủ quản (Bên B)
    /// Các endpoint khác trong TVAN (create-order, createAccount, quick-publish ...) KHÔNG port.
    /// </summary>
    //[Authorize]

    [AllowAnonymous]
    [ApiController]
    [Route("api/odoo/orders")]
    public class OdooOrdersController : ControllerBase
    {
        private readonly IOdooOrderService _service;
        private readonly ILogger<OdooOrdersController> _logger;

        public OdooOrdersController(IOdooOrderService service, ILogger<OdooOrdersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin công ty chủ quản (Bên B) để fill vào form tạo / gia hạn đơn.
        /// Mặc định trả về công ty WinTech Solution (CmpnID = "26").
        /// </summary>
        /// <param name="companyId">ID công ty (mặc định "26")</param>
        [HttpGet("owner-info")]
        [ProducesResponseType(typeof(ApiResponse<OwnerContract>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetOwnerInfo([FromQuery] string companyId = "26")
        {
            var traceId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N");
            Response.Headers["X-Correlation-Id"] = traceId;

            try
            {
                var owner = await _service.GetOwnerInfoAsync(companyId);

                if (owner == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        $"Không tìm thấy thông tin công ty chủ quản ID={companyId}",
                        404));
                }

                return Ok(ApiResponse<OwnerContract>.SuccessResponse(owner, "Lấy thông tin công ty chủ quản thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{TraceId}] Error GetOwnerInfo CmpnID={CmpnID}", traceId, companyId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message, 500));
            }
        }

        /// <summary>
        /// Lấy danh sách sản phẩm/gói dịch vụ cho dropdown trên màn hình tạo / gia hạn đơn.
        /// Toàn bộ tham số đều tuỳ chọn — không truyền sẽ trả về danh sách mặc định.
        /// </summary>
        [HttpGet("get-products")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.List<ProductResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetProducts([FromQuery] GetProductsRequest request)
        {
            var traceId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N");
            Response.Headers["X-Correlation-Id"] = traceId;

            try
            {
                var products = await _service.GetProductsAsync(request ?? new GetProductsRequest());

                var meta = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["totalCount"] = products.Count
                };

                return Ok(ApiResponse<System.Collections.Generic.List<ProductResponse>>.SuccessResponseWithMeta(
                    products,
                    meta,
                    $"Lấy {products.Count} sản phẩm thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{TraceId}] Error GetProducts", traceId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message, 500));
            }
        }
    }
}
