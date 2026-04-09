using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.ChangePassword;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP_Portal_RC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng nhập, trả về JWT token và thông tin phiên.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.LoginName) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "LoginName và Password không được để trống", 400));
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.LoginAsync(request, ipAddress, userAgent);

                if (result == null)
                {
                    return Unauthorized(ApiResponse.ErrorResponse(
                        "Tên đăng nhập hoặc mật khẩu không đúng", 401));
                }

                _logger.LogInformation("User {LoginName} logged in successfully from {IpAddress}", 
                    request.LoginName, ipAddress);

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                    result, 
                    "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {LoginName}", request.LoginName);
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi đăng nhập", 500));
            }
        }

        /// <summary>
        /// Refresh access token sử dụng refresh token
        /// </summary>
        /// <param name="request">RefreshTokenRequestDto</param>
        /// <returns>AuthResponseDto với AccessToken và RefreshToken mới</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccessToken) || 
                    string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "AccessToken và RefreshToken không được để trống", 400));
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.RefreshTokenAsync(request, ipAddress, userAgent);

                if (result == null)
                {
                    return Unauthorized(ApiResponse.ErrorResponse(
                        "Token không hợp lệ hoặc đã hết hạn", 401));
                }

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                    result, 
                    "Refresh token thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi refresh token", 500));
            }
        }

        /// <summary>
        /// Đăng xuất - Logout (revoke refresh token của device hiện tại)
        /// </summary>
        /// <param name="request">RevokeTokenRequestDto với RefreshToken</param>
        /// <returns>Status message</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "RefreshToken không được để trống", 400));
                }

                var result = await _authService.RevokeTokenAsync(request.RefreshToken);

                if (!result)
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "Token không tồn tại hoặc đã bị revoke", 400));
                }

                var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                _logger.LogInformation("User {UserName} logged out successfully", userName);

                return Ok(ApiResponse.SuccessResponse("Đăng xuất thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi đăng xuất", 500));
            }
        }

        /// <summary>
        /// Đăng xuất tất cả devices - Logout All
        /// </summary>
        /// <returns>Status message</returns>
        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> LogoutAll()
        {
            try
            {
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse.ErrorResponse(
                        "Không tìm thấy thông tin user", 401));
                }

                var result = await _authService.RevokeAllUserTokensAsync(userId);

                if (!result)
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "Không có token nào để revoke", 400));
                }

                var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                _logger.LogInformation("User {UserName} logged out from all devices", userName);

                return Ok(ApiResponse.SuccessResponse("Đã đăng xuất khỏi tất cả thiết bị"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout all");
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi đăng xuất tất cả thiết bị", 500));
            }
        }

        /// <summary>
        /// Revoke một refresh token cụ thể (Admin hoặc user tự revoke)
        /// </summary>
        /// <param name="request">RevokeTokenRequestDto</param>
        /// <returns>Status message</returns>
        [HttpPost("revoke-token")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "RefreshToken không được để trống", 400));
                }

                var result = await _authService.RevokeTokenAsync(request.RefreshToken);

                if (!result)
                {
                    return BadRequest(ApiResponse.ErrorResponse(
                        "Token không tồn tại hoặc đã bị revoke", 400));
                }

                return Ok(ApiResponse.SuccessResponse("Revoke token thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token revocation");
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi revoke token", 500));
            }
        }

        /// <summary>
        /// Validate access token (kiểm tra token có hợp lệ không)
        /// </summary>
        /// <param name="accessToken">Access token cần validate</param>
        /// <returns>Boolean result</returns>
        [HttpGet("validate-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> ValidateToken([FromQuery] string accessToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse(
                        "AccessToken không được để trống", 400));
                }

                var isValid = await _authService.ValidateAccessTokenAsync(accessToken);

                return Ok(ApiResponse<bool>.SuccessResponse(
                    isValid, 
                    isValid ? "Token hợp lệ" : "Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    "Lỗi server khi validate token", 500));
            }
        }

        /// <summary>
        /// Lấy thông tin user từ token hiện tại
        /// </summary>
        /// <returns>UserDto</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var loginName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                
                if (string.IsNullOrEmpty(loginName))
                {
                    return Unauthorized(ApiResponse.ErrorResponse(
                        "Token không hợp lệ", 401));
                }

                var userDto = new UserDto
                {
                    Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                    LoginName = loginName,
                    UserName = loginName,
                    Email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                    FullName = HttpContext.User.FindFirst("FullName")?.Value ?? string.Empty,
                    UserCode = HttpContext.User.FindFirst("UserCode")?.Value ?? string.Empty,
                    Grp_List = HttpContext.User.FindFirst("Grp_List")?.Value ?? string.Empty
                };

                return Ok(ApiResponse<UserDto>.SuccessResponse(
                    userDto, 
                    "Lấy thông tin user thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return StatusCode(500, ApiResponse.ErrorResponse(
                    "Lỗi server khi lấy thông tin user", 500));
            }
        }
        /// <summary>
        /// Kiểm tra loginName đã tồn tại trên hệ thống chưa. Trả về bool.
        /// </summary>
        /// <param name="loginName"></param>
        /// <returns></returns>
        [HttpGet("check-exists")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckExists([FromQuery] string loginName)
        {
            if (string.IsNullOrEmpty(loginName))
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse("Login name không được để trống", 400));
            }

            try
            {
                bool isExist = await _authService.IsCheckUserExistsAsync(loginName);

                string message = isExist
                    ? $"Tài khoản '{loginName}' đã tồn tại trên hệ thống."
                    : $"Tài khoản '{loginName}' chưa được sử dụng.";

                return Ok(ApiResponse<bool>.SuccessResponse(isExist, message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    $"Lỗi hệ thống khi kiểm tra tài khoản: {ex.Message}",
                    500));
            }
        }

        /// <summary>
        /// Đổi mật khẩu. Trả 401 nếu sai MK cũ, 403 nếu TK vô hiệu hoá, 404 nếu không tìm thấy user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ChangePasswordResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ChangePasswordResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = string.Join(" | ", errors)
                });
            }

            _logger.LogInformation("ChangePassword request cho user: {LoginName}", request.LoginName);

            var response = await _authService.ChangePasswordAsync(request);

            if (!response.Success)
            {
                return response.Message switch
                {
                    var m when m.Contains("không tồn tại") => NotFound(response),
                    var m when m.Contains("vô hiệu hóa") => StatusCode(StatusCodes.Status403Forbidden, response),
                    var m when m.Contains("không đúng") => Unauthorized(response),
                    _ => BadRequest(response)
                };
            }
            return Ok(response);
        }
    }
}
