using API.ERP_Portal_RC.Filters;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers.Admin
{
    /// <summary>Admin — bypass quy trình Job cho các hợp đồng cũ từ portal cũ.</summary>
    /// <remarks>
    /// Yêu cầu: JWT hợp lệ + UserCode phải nằm trong <c>Admin:AllowedUserCodes</c> (appsettings.json).
    /// </remarks>
    [Authorize]
    [TypeFilter(typeof(AdminAuthFilter))]
    [ApiController]
    [Route("api/admin/econtract")]
    [Tags("🔐 Admin")]
    [ApiExplorerSettings(GroupName = "admin")]
    public class AdminEcontractController : ControllerBase
    {
        private readonly IAdminEcontractService _service;

        public AdminEcontractController(IAdminEcontractService service)
        {
            _service = service;
        }

        private string CrtUser => User.FindFirst("UserCode")?.Value ?? "admin";

        private async Task<IActionResult> Execute(
            string? oid,
            Func<string, string, Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>>> action)
        {
            if (string.IsNullOrWhiteSpace(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("OID là bắt buộc."));

            var result = await action(oid.Trim(), CrtUser);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Bypass Cấp tài khoản (JOB_00003/JB:003) → 0→101→201.</summary>
        /// <remarks>
        /// Dùng cho HĐ cũ chưa có job cấp tài khoản.<br/>
        /// <ul>
        ///   <li>Job chưa có → Tạo job + 0→101→201</li>
        ///   <li>Job đang ở 101 → Đẩy 101→201</li>
        ///   <li>Job đã ở 201 → Idempotent</li>
        /// </ul>
        /// SP: <c>sp_Job_Bypass</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng (encode: / → %2F)</param>
        [HttpPost("bypass/captk")]
        [ProducesResponseType(typeof(ApiResponse<DeXuatCapTaiKhoanResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<IActionResult> BypassCapTaiKhoan([FromQuery] string oid)
            => Execute(oid, _service.BypassCapTaiKhoanAsync);

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Bypass Phát hành hóa đơn (JOB_00002/JB:004) → 0→101→201.</summary>
        /// <remarks>
        /// <ul>
        ///   <li>Job chưa có → Tạo job + 0→101→201</li>
        ///   <li>Job đang ở 101 → Đẩy 101→201</li>
        ///   <li>Job đã ở 201 → Idempotent</li>
        /// </ul>
        /// SP: <c>sp_Job_Bypass</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng</param>
        [HttpPost("bypass/phat-hanh-hoa-don")]
        [ProducesResponseType(typeof(ApiResponse<DeXuatCapTaiKhoanResponseDto>), StatusCodes.Status200OK)]
        public Task<IActionResult> BypassPhatHanhHoaDon([FromQuery] string oid)
            => Execute(oid, _service.BypassPhatHanhHoaDonAsync);

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Bypass Xuất hóa đơn HĐĐT (JOB_00005/JB:010) → 0→101→301.</summary>
        /// <remarks>
        /// <ul>
        ///   <li>Job chưa có → Tạo job + 0→101→301</li>
        ///   <li>Job đang ở 101 → Đẩy 101→301</li>
        ///   <li>Job đã ở 301 → Idempotent</li>
        /// </ul>
        /// SP: <c>sp_Job_Bypass</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng</param>
        [HttpPost("bypass/xuat-hoa-don-hddt")]
        [ProducesResponseType(typeof(ApiResponse<DeXuatCapTaiKhoanResponseDto>), StatusCodes.Status200OK)]
        public Task<IActionResult> BypassXuatHoaDonHDDT([FromQuery] string oid)
            => Execute(oid, _service.BypassXuatHoaDonHDDTAsync);
    }
}
