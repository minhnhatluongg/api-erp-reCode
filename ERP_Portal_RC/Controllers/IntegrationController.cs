using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// API ĐỐI SOÁT cho đối tác KHÁNH LINH (KL) — READ-ONLY, KL tự POLL.
    /// Một chiều: bên mình là nguồn sự thật; KL chỉ đọc trạng thái hợp đồng (đã ký / đã gỡ ký)
    /// để đối soát công nợ. Bảo vệ bằng API key riêng (header X-KL-Key), KHÔNG dùng JWT.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/integration/kl")]
    public class IntegrationController : ControllerBase
    {
        private readonly IEcontractService _econtractService;
        private readonly IConfiguration _config;

        public IntegrationController(IEcontractService econtractService, IConfiguration config)
        {
            _econtractService = econtractService;
            _config = config;
        }

        private bool ValidKey(out IActionResult error)
        {
            error = null;
            var expected = _config["Integration:KLApiKey"];
            var provided = Request.Headers["X-KL-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(expected) || provided != expected)
            {
                error = Unauthorized(new { message = "Invalid or missing X-KL-Key." });
                return false;
            }
            return true;
        }

        /// <summary>
        /// Danh sách trạng thái hợp đồng (đối soát). KL poll định kỳ.
        /// Lọc theo khoảng ngày thay đổi (fromDate/toDate, yyyy-MM-dd) + phân trang.
        /// Header: X-KL-Key
        /// </summary>
        [HttpGet("contracts")]
        public async Task<IActionResult> GetContracts(
            [FromQuery] string fromDate = null,
            [FromQuery] string toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            if (!ValidKey(out var err)) return err;
            var result = await _econtractService.GetKLContractStatusListAsync(fromDate, toDate, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Tra cứu trạng thái 1 hợp đồng theo OID (truyền qua query để tránh ký tự '/').
        /// Header: X-KL-Key
        /// </summary>
        [HttpGet("contract")]
        public async Task<IActionResult> GetContract([FromQuery] string oid)
        {
            if (!ValidKey(out var err)) return err;
            var result = await _econtractService.GetKLContractStatusByOidAsync(oid);
            return result.Success ? Ok(result) : StatusCode(result.StatusCode == 0 ? 400 : result.StatusCode, result);
        }
    }
}
