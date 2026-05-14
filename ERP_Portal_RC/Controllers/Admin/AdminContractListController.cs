using API.ERP_Portal_RC.Filters;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers.Admin
{
    /// <summary>Admin — xem danh sách hợp đồng không giới hạn team.</summary>
    [Authorize]
    [TypeFilter(typeof(AdminAuthFilter))]
    [ApiController]
    [Route("api/admin/econtract")]
    [Tags("🔐 Admin")]
    [ApiExplorerSettings(GroupName = "admin")]
    public class AdminContractListController : ControllerBase
    {
        private readonly IAdminContractService _service;

        public AdminContractListController(IAdminContractService service)
        {
            _service = service;
        }

        private string UserCode => User.FindFirst("UserCode")?.Value ?? "";

        /// <summary>Danh sách hợp đồng — Admin xem tất cả (ViewAll = true).</summary>
        /// <remarks>
        /// Admin không bị giới hạn bởi cây ASM, xem được toàn bộ hợp đồng trong khoảng ngày.<br/>
        /// SP: <c>wspList_EContracts_PagedV27</c> với <c>@ViewAll = 1</c>.<br/><br/>
        /// <b>FilterSaleEmID:</b>
        /// <ul>
        ///   <li>Không truyền → lấy <b>tất cả</b> HĐ trong khoảng ngày</li>
        ///   <li>Truyền usercode (VD: <c>000642</c>) → chỉ HĐ của NV đó và cây bên dưới</li>
        ///   <li>Truyền <c>me</c> → HĐ của admin đang login</li>
        /// </ul>
        /// </remarks>
        /// <param name="fromDate">Từ ngày yyyy-MM-dd (mặc định: 2010-01-01)</param>
        /// <param name="toDate">Đến ngày yyyy-MM-dd (mặc định: ngày mai)</param>
        /// <param name="filterSaleEmID">Lọc theo NV cụ thể (tuỳ chọn)</param>
        /// <param name="searchKeyword">Tìm theo CusName / CusTax / OID</param>
        /// <param name="statusFilter">Lọc theo SignNumb (tuỳ chọn)</param>
        /// <param name="page">Trang hiện tại (mặc định: 1)</param>
        /// <param name="pageSize">Số dòng/trang — tối đa 200 (mặc định: 20)</param>
        [HttpGet("list-paged")]
        [ProducesResponseType(typeof(ApiResponse<EContractPagedResponsePage>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPaged(
            [FromQuery] string? fromDate       = null,
            [FromQuery] string? toDate         = null,
            [FromQuery] string? filterSaleEmID = null,
            [FromQuery] string? searchKeyword  = null,
            [FromQuery] int?    statusFilter   = null,
            [FromQuery] int     page           = 1,
            [FromQuery] int     pageSize       = 20)
        {
            string frm = string.IsNullOrWhiteSpace(fromDate) ? "2010-01-01" : fromDate.Trim();
            string end = string.IsNullOrWhiteSpace(toDate)
                ? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                : toDate.Trim();

            // "me" → admin đang login
            string? saleFilter = filterSaleEmID?.Trim() switch
            {
                null or "" => null,
                "me"       => UserCode,
                var v      => v
            };

            int pg  = Math.Max(1, page);
            int pSz = pageSize is > 0 and <= 200 ? pageSize : 20;

            var result = await _service.GetAllContractsAsync(
                adminUserCode:  UserCode,
                filterSaleEmID: saleFilter,
                searchKeyword:  string.IsNullOrWhiteSpace(searchKeyword) ? null : searchKeyword.Trim(),
                fromDate:       frm,
                endDate:        end,
                statusFilter:   statusFilter,
                page:           pg,
                pageSize:       pSz);

            return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
        }
    }
}
