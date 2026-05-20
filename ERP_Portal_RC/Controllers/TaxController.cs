using ERP_Portal_RC.Application.DTOs.Tax;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities.Tax;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// API tra cứu thông tin thuế / hợp đồng theo MST hoặc OID.
    /// Dời từ TVAN_WEB_API/TaxController sang đây, đóng vai trò orchestrator gọi xuống ITaxService.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class TaxController : ControllerBase
    {
        private readonly ITaxService _taxService;
        private readonly ILogger<TaxController> _logger;

        public TaxController(ITaxService taxService, ILogger<TaxController> logger)
        {
            _taxService = taxService;
            _logger = logger;
        }

        private string GetTraceId() => HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        /// <summary>
        /// Lấy toàn bộ thông tin hợp đồng + công ty theo MST.
        /// </summary>
        [HttpGet("get-full-info-by-mst")]
        [ProducesResponseType(typeof(ApiResponse<TaxFullInfoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetFullInfoByMst([FromQuery] string mst, [FromQuery] int loaiCap = 0)
        {
            var traceId = GetTraceId();
            try
            {
                if (string.IsNullOrWhiteSpace(mst))
                {
                    return BadRequest(ApiResponse.ErrorResponse("MST không được để trống"));
                }

                var data = await _taxService.GetFullInfoByMstAsync(mst, loaiCap);

                if (data == null)
                {
                    return Ok(ApiResponse<TaxFullInfoDto>.ErrorResponse(
                        "Không tìm thấy hợp đồng trong BosOnline.", 404));
                }

                return Ok(ApiResponse<TaxFullInfoDto>.SuccessResponse(data, "Lấy thông tin thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tax] GetFullInfoByMst lỗi MST={MST}, TraceId={TraceId}", mst, traceId);
                return StatusCode(500, ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Lấy danh sách hợp đồng (OID) theo MST/CCCD.
        /// </summary>
        [HttpGet("get-oid-list-by-mst")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ContractSummaryRow>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetOidList([FromQuery] string mst)
        {
            var traceId = GetTraceId();
            try
            {
                if (string.IsNullOrWhiteSpace(mst))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Thiếu MST/CCCD."));
                }

                var data = await _taxService.GetOidListByMstAsync(mst);
                return Ok(ApiResponse<IEnumerable<ContractSummaryRow>>.SuccessResponse(
                    data, "Lấy danh sách hợp đồng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tax] GetOidList lỗi MST={MST}, TraceId={TraceId}", mst, traceId);
                return StatusCode(500, ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Lấy chi tiết hợp đồng theo OID (kèm sản phẩm, mẫu TT78).
        /// </summary>
        [HttpGet("get-full-info-by-oid")]
        [ProducesResponseType(typeof(ApiResponse<TaxFullInfoByOidDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetFullInfoByOid([FromQuery] string oid)
        {
            var traceId = GetTraceId();
            try
            {
                if (string.IsNullOrWhiteSpace(oid))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Thiếu mã OID."));
                }

                var data = await _taxService.GetFullInfoByOidAsync(oid);
                if (data == null)
                {
                    return Ok(ApiResponse<TaxFullInfoByOidDto>.ErrorResponse(
                        "Không tìm thấy hợp đồng theo OID này.", 404));
                }

                return Ok(ApiResponse<TaxFullInfoByOidDto>.SuccessResponse(data, "Lấy thông tin thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tax] GetFullInfoByOid lỗi OID={OID}, TraceId={TraceId}", oid, traceId);
                return StatusCode(500, ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500));
            }
        }
    }
}
