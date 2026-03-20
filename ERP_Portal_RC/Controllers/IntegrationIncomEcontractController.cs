using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/integration/incom")]
    public class IntegrationIncomEcontractController : Controller
    {
        private readonly IIntegrationService _integrationIncomService;
        private readonly ILogger<IntegrationIncomEcontractController> _logger;

        public IntegrationIncomEcontractController(
            IIntegrationService integrationIncomService,
            ILogger<IntegrationIncomEcontractController> logger)
        {
            _integrationIncomService = integrationIncomService;
            _logger = logger;
        }

        /// <summary>
        /// Tích hợp đơn hàng EContract từ hệ thống bên ngoài
        /// </summary>
        [HttpPost("econtract")]
        [Authorize]
        public async Task<IActionResult> ProcessEContractIntegration(
            [FromBody] EContractIntegrationRequestDto model)
        {
            var crtUser = User.FindFirst("UserCode")?.Value;

            _logger.LogInformation(
                "[EContract] ProcessIntegration called | OID: {OID} | CusTax: {CusTax} | User: {User}",
                model?.OrderOID, model?.CusTax, crtUser);

            var result = await _integrationIncomService.ProcessEContractIntegrationAsync(model, crtUser);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
