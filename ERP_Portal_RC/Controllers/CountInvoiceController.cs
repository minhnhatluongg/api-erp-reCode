using ERP_Portal_RC.Application.DTOs.Count_Invoice;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class CountInvoiceController : ControllerBase
    {
        private readonly IEcontractService _econtractService;
        public CountInvoiceController(IEcontractService econtractService)
        {
            _econtractService = econtractService;
        }
        [HttpPost("status")]
        [ProducesResponseType(typeof(ApiResponse<InvCounterResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<InvCounterResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<InvCounterResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStatus([FromBody] InvCounterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<InvCounterResponseDto>.ErrorResponse(
                    "Dữ liệu đầu vào không hợp lệ.", 400, errors));
            }

            try
            {
                var response = await _econtractService.GetInvCounterByMSTAsync(request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    500 => StatusCode(500, response),
                    _ => BadRequest(response)
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<InvCounterResponseDto>.ErrorResponse(
                    "Lỗi máy chủ. Vui lòng thử lại sau.", 500));
            }
        }
    }
}
