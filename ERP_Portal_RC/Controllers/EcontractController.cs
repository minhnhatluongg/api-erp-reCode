using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EcontractController : Controller
    {
        private readonly IEcontractService _econtractService;
        public EcontractController(IEcontractService econtractService)
        {
            _econtractService = econtractService;
        }

        [HttpGet("countContract")]
        public async Task<ActionResult<ApiResponse<object>>> CountContract(
            [FromQuery] ContractSearchRequest request,
            [FromQuery] bool ismanager)
        {
            try
            {
                var userName = User.Identity?.Name;
                var userCode = User.FindFirst("UserCode")?.Value;
                var grpList = User.FindFirst("Grp_List")?.Value;

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userCode))
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Không tìm thấy thông tin định danh người dùng."));
                }

                var dashboardData = await _econtractService.GetContractDashboardAsync(
                    userCode,
                    userName,
                    grpList ?? "",
                    ismanager,
                    request);

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    econtract = dashboardData
                }, "Lấy dữ liệu Dashboard thành công."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// API lấy danh sách hợp đồng chi tiết (Nếu cần tách riêng với Dashboard)
        [HttpGet("listContract")]
        public async Task<ActionResult<ApiResponse<ListEcontractViewModel>>> GetListContract(
            [FromQuery] ContractSearchRequest request,
            [FromQuery] bool ismanager)
        {
            try
            {
                var userName = User.Identity?.Name;
                var userCode = User.FindFirst("UserCode")?.Value;
                var grpList = User.FindFirst("Grp_List")?.Value;

                var result = await _econtractService.GetContractListAsync(
                    userCode!,
                    userName!,
                    grpList ?? "",
                    ismanager,
                    request);

                return Ok(ApiResponse<ListEcontractViewModel>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ListEcontractViewModel>.ErrorResponse(ex.Message));
            }
        }
    }
}

