using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CapTaiKhoanController : ControllerBase
    {
        private readonly ICapTaiKhoanService _serviceCapTaiKhoan;
        private readonly ILogger<CapTaiKhoanController> _logger;

        public CapTaiKhoanController(
            ICapTaiKhoanService service,
            ILogger<CapTaiKhoanController> logger)
        {
            _serviceCapTaiKhoan = service;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới tài khoản hệ thống cho khách hàng theo MST.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("cap-tai-khoan")]
        [ProducesResponseType(typeof(CreateAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CreateAccountResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CapTaiKhoan([FromBody] CreateAccountRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            _logger.LogInformation(
                "[API][CapTK] POST cap-tai-khoan — MST={MST}", request.MaSoThue);
            try
            {
                var result = await _serviceCapTaiKhoan.CapTaiKhoanAsync(request);

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API][CapTK] Unhandled exception — MST={MST}", request.MaSoThue);
                return StatusCode(StatusCodes.Status500InternalServerError, new CreateAccountResponseDto
                {
                    IsSuccess = false,
                    Message = "Lỗi hệ thống.",
                    ErrorDetail = ex.Message
                });
            }
        }
        /// <summary>
        /// Kiểm tra trạng thái server / tài khoản theo MST và CCCD.
        /// </summary>
        /// <param name="mst"></param>
        /// <param name="cccd"></param>
        /// <returns></returns>
        [HttpGet("check-server/{mst}")]
        [ProducesResponseType(typeof(CheckServerResponseDto), StatusCodes.Status200OK)]
        public IActionResult CheckServer(string mst, [FromQuery] string cccd = "")
        {
            var result = _serviceCapTaiKhoan.CheckServer(mst, cccd);
            return Ok(result);
        }
    }
}