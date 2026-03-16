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

        /// <summary>
        /// Bổ sung file đính kèm cho một OID đã tồn tại
        /// </summary>
        [HttpPost("add-more-for-Econtracts")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> AddMore([FromBody] AddAttachmentRequest request)
        {
            string currentUser = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(currentUser))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin định danh người dùng", 401));
            }
            request.Crt_User = currentUser;

            var response = await _econtractService.AddMoreFilesAsync(
                request.OID,
                request.FactorID,
                request.EntryID,
                request.Files,
                currentUser
            );

            return Ok(response);
        }
    }
}
