using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractAttachmentController : Controller
    {
        private readonly IEcontractService _econtractService;

        public ContractAttachmentController(IEcontractService econtractService)
        {
            _econtractService = econtractService;
        }

        /// <summary>
        /// Lấy danh sách file đính kèm theo OID (Của Job hoặc Hợp đồng)
        /// </summary>
        /// <param name="oid">Mã định danh OID</param>
        [HttpGet("list/{oid}")]
        public async Task<IActionResult> GetList(string oid)
        {
            string decodedOid = System.Net.WebUtility.UrlDecode(oid);
            var response = await _econtractService.GetAttachmentsByOidAsync(decodedOid);
            return Ok(response);
        }
        
    }
}
