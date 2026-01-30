using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static ERP_Portal_RC.Application.DTOs.MenuResponseViewDto;

namespace ERP_Portal_RC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly ICustomStore _customStore;
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAccountService accountService,
            ICustomStore customStore,
            ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _customStore = customStore;
            _logger = logger;
        }

        [HttpGet("menu")]
        [ProducesResponseType(typeof(ApiResponse<MenuResponseDto>), 200)]
        public async Task<IActionResult> GetMenu([FromQuery] string? appSite = null)
        {
            try
            {
                // 1. Lấy thông tin từ Token
                var grp_List = HttpContext.User.FindFirst("Grp_List")?.Value;
                var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                var cmpnId = User.FindFirst("CmpnID")?.Value;
                var effectiveAppSite = appSite ?? User.FindFirst("DefaultAppSite")?.Value ?? "Bos";

                if (string.IsNullOrEmpty(grp_List) || string.IsNullOrEmpty(userName))
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Không tìm thấy thông tin xác thực", 401));
                }

                // 2. GỌI SERVICE - Đây là nơi BuildMenuTree thực sự chạy
                var result = await _accountService.GetUserMenuAsync(userName, grp_List, cmpnId, effectiveAppSite);

                // 3. Trả về kết quả đã được Service xử lý "sạch"
                return Ok(ApiResponse<MenuResponseDto>.SuccessResponse(
                        result,
                        $"Lấy menu thành công ({result.TotalMenuItems} items)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting menu for AppSite: {AppSite}", appSite);
                return StatusCode(500, ApiResponse.ErrorResponse("Lỗi server khi lấy menu", 500));
            }
        }


        [HttpGet("current-user")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var fullName = HttpContext.User.FindFirst("FullName")?.Value;
                var userCode = HttpContext.User.FindFirst("UserCode")?.Value;
                var grp_List = HttpContext.User.FindFirst("Grp_List")?.Value;
                var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;

                var user = new
                {
                    Id = userId,
                    UserName = userName,
                    LoginName = userName,
                    FullName = fullName,
                    UserCode = userCode,
                    Grp_List = grp_List,
                    Email = email
                };

                _logger.LogInformation("Current user info retrieved from token: {UserName}", userName);

                return Ok(ApiResponse<object>.SuccessResponse(
                    user,
                    "Lấy thông tin user thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi lấy thông tin user", 500));
            }
        }

        [HttpGet("check-permission/{menuId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> CheckPermission(string menuId, [FromQuery] string? appSite = null)
        {
            try
            {
                var grp_List = HttpContext.User.FindFirst("Grp_List")?.Value;
                var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(grp_List))
                {
                    return Unauthorized(ApiResponse.ErrorResponse(
                        "Không tìm thấy thông tin group", 401));
                }

                // Lấy menu của user
                var userMenus = await _customStore.GetApplicationToolsByGroupAsync(grp_List);

                // Check permission
                var hasPermission = userMenus.Any(m =>
                    m.MenuID != null &&
                    m.MenuID.Equals(menuId, StringComparison.OrdinalIgnoreCase));

                var result = new
                {
                    HasPermission = hasPermission,
                    MenuId = menuId,
                    UserName = userName,
                    Grp_List = grp_List,
                    AppSite = appSite,
                    Message = hasPermission
                        ? "User có quyền truy cập"
                        : "User không có quyền truy cập"
                };

                _logger.LogInformation(
                    "Permission check for MenuID {MenuId}, User {UserName}: {Result}",
                    menuId, userName, hasPermission ? "GRANTED" : "DENIED");

                return Ok(ApiResponse<object>.SuccessResponse(
                    result,
                    result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for MenuID: {MenuId}", menuId);
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi kiểm tra quyền", 500));
            }
        }
    }
}
