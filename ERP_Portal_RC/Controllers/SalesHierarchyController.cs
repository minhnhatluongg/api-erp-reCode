using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.AccountKeToan;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesHierarchyController : Controller
    {
        private readonly ISalesHierarchyService _salesHierarchyService;
        public SalesHierarchyController(ISalesHierarchyService salesHierarchyService)
        {
            _salesHierarchyService = salesHierarchyService;
        }
        /// <summary>
        /// Show Cây ASM của ERP 
        /// </summary>
        /// <param name="clnID"></param>
        /// <param name="isManager"></param>
        /// <returns></returns>
        [HttpGet("managers/{clnID}")]
        public async Task<ActionResult<ApiResponse<List<ManagerDto>>>> GetManagers(string clnID = "21:000",bool isManager = false)
        {
            var tree = await _salesHierarchyService.GetManagerTreeAsync(clnID, isManager);
            return Ok(new { success = true, data = tree });
        }

        /// <summary>
        /// Đăng ký nhân sự Sale (LOT / ERP) và tuỳ chọn tạo tài khoản đăng nhập.
        /// </summary>
        /// <remarks>
        /// Cách điền input:
        /// <list type="bullet">
        ///   <item><c>FullName</c>, <c>Email</c>, <c>ManagerEmplID</c>, <c>SoCMND</c>: BẮT BUỘC.</item>
        ///   <item><c>ManagerEmplID</c>: mã quản lý trực tiếp (để gắn vào cây ASM, thiếu thì không thấy hợp đồng).</item>
        ///   <item><c>IsCreateAccount = false</c>: chỉ thêm nhân sự, không cần <c>LoginName</c>/<c>Password</c>.</item>
        ///   <item><c>IsCreateAccount = true</c>: phải có <c>LoginName</c> (≥5 ký tự) và <c>Password</c> (≥6 ký tự)
        ///         để tạo tài khoản đăng nhập.</item>
        ///   <item><c>PsID</c>, <c>Phone</c>: tuỳ chọn.</item>
        /// </list>
        /// </remarks>
        /// <param name="request">Thông tin đăng ký nhân sự Sale.</param>
        /// <returns>Mã nhân sự mới + tài khoản (nếu có) trong <see cref="RegistrationResultDto"/>.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterSale([FromBody] SaleRegistrationModel request)
        {
            try
            {
                var result = await _salesHierarchyService.HandleSaleRegistrationAsync(request);
                return Ok(ApiResponse<RegistrationResultDto>.SuccessResponse(result, "Đăng ký nhân sự thành công."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<RegistrationResultDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<RegistrationResultDto>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// API Đăng Kí Account Kế Toán - LOT ERP
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("accounting/register")]
        public async Task<IActionResult> RegisterAccounting(
            [FromBody] AccountingRegistrationRequestDto request)
        {
            try
            {
                var result = await _salesHierarchyService.HandleAccountingRegistrationAsync(request);
                return Ok(ApiResponse<AccountingRegistrationResultDto>.SuccessResponse(
                    result, "Tạo tài khoản kế toán thành công."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<AccountingRegistrationResultDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AccountingRegistrationResultDto>.ErrorResponse(ex.Message));
            }
        }
    }
}
