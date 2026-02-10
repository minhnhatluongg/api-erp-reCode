using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TechnicalUserController : Controller
    {
        private readonly IRegistrationCodeService _codeService;
        public TechnicalUserController(IRegistrationCodeService codeService)
        {
            _codeService = codeService;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TechnicalLoginRequest request)
        {
            var loginResult = await _codeService.LoginAsync(request);
            if (loginResult == null)
            {
                return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse(
                    "Tên đăng nhập hoặc mật khẩu kỹ thuật không chính xác.", 401));
            }
            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(
                loginResult, "Đăng nhập kỹ thuật thành công."));
        }
        [Authorize(Roles = "Technical")]
        [HttpPost("generate-code")]
        public async Task<IActionResult> GenerateCode()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(ApiResponse.ErrorResponse("Không tìm thấy thông tin định danh người dùng.", 401));
            try
            {
                var code = await _codeService.GenerateAndSaveCodeAsync(int.Parse(userIdClaim));
                return Ok(ApiResponse<object>.SuccessResponse(new { registrationCode = code }, "Tạo mã đăng kí thành công."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.ErrorResponse($"Lỗi khi tạo mã {ex.Message}"));
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TechnicalRegistrationRequest request)
        {
            var success = await _codeService.RegisterAsync(request);

            if (!success)
            {
                return BadRequest(ApiResponse.ErrorResponse("Tên đăng nhập kỹ thuật này đã tồn tại trong hệ thống."));
            }

            return Ok(ApiResponse.SuccessResponse("Đăng ký tài khoản kỹ thuật thành công.", 201));
        }

        [HttpGet("validate-registration-code/{code}")]
        public async Task<IActionResult> ValidateCode(string code)
        {
            var isValid = await _codeService.ValidateCodeAsync(code);

            if (!isValid)
            {
                return BadRequest(ApiResponse.ErrorResponse(
                    "Mã đăng ký không hợp lệ, đã được sử dụng hoặc đã hết hạn."));
            }

            return Ok(ApiResponse.SuccessResponse("Mã đăng ký hợp lệ. Bạn có thể tiếp tục đăng ký."));
        }
    }
}
