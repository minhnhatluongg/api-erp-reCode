using API.ERP_Portal_RC.Filters;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [Authorize]
    [TypeFilter(typeof(AdminAuthFilter))]
    [ApiController]
    [Route("api/admin/econtract")]
    public class AdminEcontractController : ControllerBase
    {
        private readonly IEContractRepository _repo;

        public AdminEcontractController(IEContractRepository repo)
        {
            _repo = repo;
        }

        private string CrtUser => User.FindFirst("UserCode")?.Value ?? "admin";

        private async Task<IActionResult> RunBypass(
            string oid, string factorId, string entryId, int finalSign)
        {
            if (string.IsNullOrWhiteSpace(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("OID là bắt buộc."));
            try
            {
                var result = await _repo.BypassJobAsync(
                    oid.Trim(), factorId, entryId, finalSign, CrtUser);

                var data = new DeXuatCapTaiKhoanResponseDto
                {
                    OIDJob        = result.OIDJob,
                    ReferenceInfo = result.ReferenceInfo
                };

                return Ok(ApiResponse<DeXuatCapTaiKhoanResponseDto>.SuccessResponse(
                    data, result.ReferenceInfo));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        /// <summary>Bypass Cấp tài khoản (JOB_00003/JB:003) → đẩy lên 201.</summary>
        /// <remarks>
        /// Dùng cho hợp đồng cũ chưa có job cấp tài khoản.<br/>
        /// <b>Logic tự động:</b>
        /// <ul>
        ///   <li>Job chưa tồn tại → Tạo job + 0→101→201</li>
        ///   <li>Job đang ở 101 → Đẩy tiếp 101→201</li>
        ///   <li>Job đã ở 201 → Idempotent, trả success</li>
        /// </ul>
        /// SP: <c>sp_Job_Bypass</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng (VD: 000456%2F260513%3A033156630)</param>
        [HttpPost("bypass/captk")]
        public Task<IActionResult> BypassCapTaiKhoan([FromQuery] string oid)
            => RunBypass(oid, "JOB_00003", "JB:003", finalSign: 201);

        // ─────────────────────────────────────────────────────────────────
        /// <summary>Bypass Phát hành hóa đơn (JOB_00002/JB:004) → đẩy lên 201.</summary>
        /// <remarks>
        /// Dùng cho hợp đồng cũ chưa có job phát hành hóa đơn.<br/>
        /// <b>Logic tự động:</b>
        /// <ul>
        ///   <li>Job chưa tồn tại → Tạo job + 0→101→201</li>
        ///   <li>Job đang ở 101 → Đẩy tiếp 101→201</li>
        ///   <li>Job đã ở 201 → Idempotent, trả success</li>
        /// </ul>
        /// SP: <c>sp_Job_Bypass</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng</param>
        [HttpPost("bypass/phat-hanh-hoa-don")]
        public Task<IActionResult> BypassPhatHanhHoaDon([FromQuery] string oid)
            => RunBypass(oid, "JOB_00002", "JB:004", finalSign: 201);

        // ─────────────────────────────────────────────────────────────────
        /// <summary>Bypass Xuất hóa đơn HĐĐT (JOB_00005/JB:010) → đẩy lên 301.</summary>
        /// <remarks>
        /// Dùng cho hợp đồng cũ chưa có job xuất HĐĐT.<br/>
        /// <b>Logic tự động:</b>
        /// <ul>
        ///   <li>Job chưa tồn tại → Tạo job + 0→101→301</li>
        ///   <li>Job đang ở 101 → Đẩy tiếp 101→301</li>
        ///   <li>Job đã ở 301 → Idempotent, trả success</li>
        /// </ul>
        /// SP: <c>sp_Job_Bypass</c>
        /// </remarks>
        /// <param name="oid">OID hợp đồng</param>
        [HttpPost("bypass/xuat-hoa-don-hddt")]
        public Task<IActionResult> BypassXuatHoaDonHDDT([FromQuery] string oid)
            => RunBypass(oid, "JOB_00005", "JB:010", finalSign: 301);
    }
}
