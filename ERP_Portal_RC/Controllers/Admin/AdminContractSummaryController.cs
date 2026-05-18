using API.ERP_Portal_RC.Filters;
using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers.Admin
{
    /// <summary>Admin — xem snapshot toàn bộ trạng thái 1 hợp đồng.</summary>
    [Authorize]
    [TypeFilter(typeof(AdminAuthFilter))]
    [ApiController]
    [Route("api/admin/econtract")]
    [Tags("🔐 Admin")]
    [ApiExplorerSettings(GroupName = "admin")]
    public class AdminContractSummaryController : ControllerBase
    {
        private readonly IAdminContractSummaryService _service;

        public AdminContractSummaryController(IAdminContractSummaryService service)
        {
            _service = service;
        }

        /// <summary>Snapshot toàn bộ trạng thái 1 hợp đồng trong 1 call.</summary>
        /// <remarks>
        /// Trả về 5 nhóm thông tin:
        /// <ul>
        ///   <li><b>contract</b> — Thông tin cơ bản HĐ (CusTax, CusName, Sale, SignNumb hiện tại...)</li>
        ///   <li><b>signHistory</b> — Lịch sử ký HĐ (101→301→501) từ <c>zsgn_webContracts</c></li>
        ///   <li><b>jobs</b> — Trạng thái mới nhất mỗi Job: cấp TK, phát hành HĐ, xuất HĐĐT...</li>
        ///   <li><b>tracking</b> — Lịch sử gỡ ký / chỉnh sửa / gửi lại từ <c>ECtr_ContractTrackingLog</c></li>
        ///   <li><b>publicInfo</b> — Thông tin HĐ đã ký điện tử từ <c>ECtr_PublicInfo</c> (nếu có)</li>
        /// </ul>
        /// SP: <c>sp_GetEContract_Summary</c><br/>
        /// OID cần encode: dấu <c>/</c> → <c>%2F</c>, dấu <c>:</c> → <c>%3A</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng (VD: 000456%2F260513%3A033156630)</param>
        [HttpGet("summary/{oid}")]
        [ProducesResponseType(typeof(ApiResponse<ContractSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSummary(string oid)
        {
            if (string.IsNullOrWhiteSpace(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("OID là bắt buộc."));

            var result = await _service.GetSummaryAsync(oid);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                _   => StatusCode(result.StatusCode, result)
            };
        }
    }
}
