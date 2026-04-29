using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// API v26 — dùng SP wspList_EContracts_PagedV26.
    /// Controller riêng, không ảnh hưởng code cũ.
    /// </summary>
    [Authorize]
    [Route("api/Econtract-v26")]
    [ApiController]
    public class EcontractV26Controller : ControllerBase
    {
        private readonly IEContractV26Repository _repo;

        public EcontractV26Controller(IEContractV26Repository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Lấy danh sách hợp đồng điện tử (SP V27) — có phân trang + filter.
        ///
        /// Cách hoạt động:
        ///   - Không truyền SearchKeyword   → lấy tất cả HĐ theo team của CrtUser trong khoảng ngày.
        ///   - Truyền SearchKeyword         → tìm trên CusName / CusTax / OID (LIKE).
        ///   - StatusFilter                 → lọc thêm theo SignNumb (101, 201...).
        ///   - FilterSaleEmID trống/null    → cả team.
        ///   - FilterSaleEmID = "000642"    → chỉ HĐ của nhân viên đó (SaleEmID = '000642').
        ///   - FilterSaleEmID = "me"        → tự động dùng UserCode từ token (HĐ của chính mình).
        ///
        /// UserCode lấy từ JWT token (claim "UserCode").
        /// </summary>
        [HttpGet("list-paged")]
        public async Task<IActionResult> GetAllPaged([FromQuery] EContractV26Request request)
        {
            try
            {
                // Lấy UserCode từ token
                var userCode = User.FindFirst("UserCode")?.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userCode))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Không xác định được UserCode từ token.", 401));

                // Chuẩn hoá ngày
                string frm = string.IsNullOrWhiteSpace(request.FrmDate)
                    ? "2010-01-01"
                    : request.FrmDate.Trim();

                string end = string.IsNullOrWhiteSpace(request.ToDate)
                    ? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                    : request.ToDate.Trim();

                // Chuẩn hoá keyword
                string? keyword = string.IsNullOrWhiteSpace(request.SearchKeyword)
                    ? null
                    : request.SearchKeyword.Trim();

                // "me" → tự map sang userCode đang login
                string? filterSaleEmID = request.FilterSaleEmID?.Trim() switch
                {
                    null or ""  => null,
                    "me"        => userCode,
                    var v       => v
                };

                int page     = Math.Max(1, request.Page);
                int pageSize = request.PageSize is > 0 and <= 200 ? request.PageSize : 20;

                var (data, subEmpl, meta) = await _repo.GetAllPagedAsync(
                    crtUser:         userCode,
                    frmDate:         frm,
                    endDate:         end,
                    search:          keyword,
                    statusFilter:    request.StatusFilter,
                    filterSaleEmID:  filterSaleEmID,
                    page:            page,
                    pageSize:        pageSize);

                var result = new EContractPagedResponsePage
                {
                    Data       = data.ToList(),
                    SubEmpl    = subEmpl.ToList(),
                    TotalCount = meta.TotalCount,
                    Page       = meta.Page,
                    PageSize   = meta.PageSize
                    // TotalPages tính tự động từ computed property
                };

                return Ok(ApiResponse<EContractPagedResponsePage>.SuccessResponse(
                    result,
                    $"Trang {meta.Page}/{meta.TotalPages}. Tổng: {meta.TotalCount} hợp đồng."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    $"Lỗi hệ thống: {ex.Message}", 500));
            }
        }
    }
}
