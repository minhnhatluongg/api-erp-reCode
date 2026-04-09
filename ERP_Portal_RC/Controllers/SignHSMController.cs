using ERP_Portal_RC.Application.DTOs.SignHSM;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SignHSMController : ControllerBase
    {
        private readonly ISignHSMService _service;
        private readonly ILogger<SignHSMController> _logger;

        public SignHSMController(
            ISignHSMService service,
            ILogger<SignHSMController> logger)
        {
            _service = service;
            _logger = logger;
        }
        /// <summary>
        /// Lưu file XML đã ký số vào hệ thống.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("save-signed-xml")]
        [ProducesResponseType(typeof(ApiResponse<SaveSignedXmlResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SaveSignedXmlResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SaveSignedXmlResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveSignedXml([FromBody] SaveSignedXmlRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<SaveSignedXmlResponseDto>.ErrorResponse(
                    "Dữ liệu không hợp lệ.", 400, errors));
            }
            var response = await _service.SaveSignedXmlAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
