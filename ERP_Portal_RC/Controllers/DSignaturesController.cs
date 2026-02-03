using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DSignaturesController : Controller
    {
        private readonly IDSignaturesService _dSignaturesService;
        public DSignaturesController(IDSignaturesService dSignaturesService)
        {
            _dSignaturesService = dSignaturesService;
        }
        [HttpGet("CountDigitalSignatures")]
        public async Task<ActionResult<ApiResponse<DigitalSignaturesDashboardDto>>> CountDigitalSignatures([FromQuery] bool ismanager)
        {
            var userCode = User.FindFirst("UserCode")?.Value; 
            var groupList = User.FindFirst("Grp_List")?.Value;
            var loginName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userCode))
                return Unauthorized(ApiResponse<DigitalSignaturesDashboardDto>.ErrorResponse("Unauthorized"));

            var result = await _dSignaturesService.GetCountDigitalSignaturesAsync(loginName, userCode, groupList, ismanager);

            return Ok(ApiResponse<DigitalSignaturesDashboardDto>.SuccessResponse(result));
        }
    }
}
