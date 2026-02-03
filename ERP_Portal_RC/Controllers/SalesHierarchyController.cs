using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
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
        [HttpGet("managers/{clnID}")]
        public async Task<ActionResult<ApiResponse<List<ManagerDto>>>> GetManagers(string clnID = "21:000",bool isManager = false)
        {
            var tree = await _salesHierarchyService.GetManagerTreeAsync(clnID, isManager);
            return Ok(new { success = true, data = tree });
        }
    }
}
