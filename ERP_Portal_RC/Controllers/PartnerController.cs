using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using ERP_Portal_RC.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// API dành riêng cho đối tác bên ngoài.
    /// Bảo vệ bằng header X-KL-Key — không dùng JWT.
    /// managerCode ẩn trong appsettings, đối tác chỉ truyền ngày.
    /// </summary>
    [ApiController]
    [Route("api/partner")]
    public class PartnerController : ControllerBase
    {
        private readonly IPartnerRepository         _partnerRepo;
        private readonly IConfiguration             _config;
        private readonly ILogger<PartnerController> _logger;

        public PartnerController(
            IPartnerRepository partnerRepo,
            IConfiguration config,
            ILogger<PartnerController> logger)
        {
            _partnerRepo = partnerRepo;
            _config      = config;
            _logger      = logger;
        }

        private bool ValidateKLKey()
        {
            var expected = _config["Partner:KLKey"];
            var provided = Request.Headers["X-KL-Key"].FirstOrDefault();
            return !string.IsNullOrEmpty(expected) && provided == expected;
        }

        /// <summary>Đồng bộ danh sách hợp đồng điện tử theo khoảng ngày.</summary>
        /// <remarks>
        /// <b>Xác thực:</b> Truyền key vào header <c>X-KL-Key</c> — không dùng JWT.
        /// <br/><br/>
        /// <b>Query params (tuỳ chọn):</b>
        /// <ul>
        ///   <li><b>fromDate</b> — Từ ngày (yyyy-MM-dd), mặc định 30 ngày trước.</li>
        ///   <li><b>toDate</b>   — Đến ngày (yyyy-MM-dd), mặc định hôm nay.</li>
        /// </ul>
        /// <b>Thông tin trả về mỗi hợp đồng:</b>
        /// <ul>
        ///   <li><b>currSignNumb</b> — Trạng thái ký: 0=Nháp | 101=Đang trình ký | 301=Đã ký | 501=Hoàn tất ( Khách Ký )</li>
        ///   <li><b>TT2</b> — Trạng thái tạo mẫu </li>
        ///   <li><b>TT3</b> — Trạng thái cấp tài khoản </li>
        ///   <li><b>TT4</b> — Trạng thái phát hành hóa đơn </li>
        ///   <li><b>TT8</b> — Trạng thái xuất HĐĐT </li>
        /// </ul>
        /// </remarks>
        /// <param name="fromDate">Từ ngày (yyyy-MM-dd). Mặc định: 30 ngày trước.</param>
        /// <param name="toDate">Đến ngày (yyyy-MM-dd). Mặc định: hôm nay.</param>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("econtract/sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SyncContracts(
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate   = null)
        {
            if (!ValidateKLKey())
            {
                _logger.LogWarning("[Partner] BLOCKED — IP={IP}",
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new { success = false, message = "Unauthorized." });
            }

            string managerCode = _config["Partner:ManagerCode"] ?? "";
            int    pageSize    = int.TryParse(_config["Partner:PageSize"], out var ps)
                                    ? Math.Min(ps, 500) : 200;

            if (string.IsNullOrWhiteSpace(managerCode))
            {
                _logger.LogError("[Partner] ManagerCode chưa được cấu hình.");
                return StatusCode(500, new { success = false, message = "Lỗi cấu hình server." });
            }

            string frm = string.IsNullOrWhiteSpace(fromDate)
                ? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd")
                : fromDate.Trim();

            string end = string.IsNullOrWhiteSpace(toDate)
                ? DateTime.Now.ToString("yyyy-MM-dd")
                : toDate.Trim();

            _logger.LogInformation("[Partner] SYNC — {From}→{To}", frm, end);

            try
            {
                var allData    = new List<EContract_Monitor_Refactor>();
                int totalCount = 0;
                int page       = 1;

                while (true)
                {
                    var rows = await _partnerRepo.GetContractsByDateAsync(
                        managerCode, frm, end, page, pageSize);

                    if (!rows.Any()) break;

                    if (page == 1)
                        totalCount = rows[0].TotalCount;

                    allData.AddRange(rows);

                    if (allData.Count >= totalCount) break;
                    page++;
                }

                return Ok(new
                {
                    success    = true,
                    fromDate   = frm,
                    toDate     = end,
                    totalCount = allData.Count,
                    data       = allData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Partner] ERROR");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống." });
            }
        }

        /// <summary>Health check — không cần key.</summary>
        [HttpGet("health")]
        public IActionResult Health()
            => Ok(new { status = "ok", timestamp = DateTime.UtcNow });

        /// <summary>Lấy danh sách hợp đồng điện tử theo khoảng ngày, có phân trang.</summary>
        /// <remarks>
        /// <b>Xác thực:</b> Truyền key vào header <c>X-KL-Key</c> — không dùng JWT.
        /// <br/><br/>
        /// <b>Query params:</b>
        /// <ul>
        ///   <li><b>fromDate</b>    — Từ ngày (yyyy-MM-dd). Mặc định: 30 ngày trước.</li>
        ///   <li><b>toDate</b>      — Đến ngày (yyyy-MM-dd). Mặc định: hôm nay.</li>
        ///   <li><b>strSearch</b>   — Tìm kiếm theo tên KH / mã số thuế / mã HĐ (tuỳ chọn).</li>
        ///   <li><b>statusFilter</b>— Lọc theo trạng thái ký currSignNumb (tuỳ chọn).</li>
        ///   <li><b>page</b>        — Số trang, mặc định 1.</li>
        ///   <li><b>pageSize</b>    — Số dòng mỗi trang, mặc định 20, tối đa theo cấu hình server.</li>
        /// </ul>
        /// <b>Thông tin trả về:</b>
        /// <ul>
        ///   <li><b>totalCount</b>  — Tổng số hợp đồng khớp điều kiện (dùng để tính tổng trang).</li>
        ///   <li><b>page / pageSize</b> — Trang hiện tại và kích thước trang thực tế.</li>
        ///   <li><b>data[]</b>      — Danh sách hợp đồng trang hiện tại.</li>
        /// </ul>
        /// <b>Trường trạng thái trong mỗi hợp đồng:</b>
        /// <ul>
        ///   <li><b>currSignNumb</b> — Trạng thái ký: 0=Nháp | 101=Đang trình ký | 301=Đã ký | 501=Hoàn tất (Khách ký).</li>
        ///   <li><b>TT2</b> — Trạng thái tạo mẫu hợp đồng.</li>
        ///   <li><b>TT3</b> — Trạng thái cấp tài khoản hệ thống.</li>
        ///   <li><b>TT4</b> — Trạng thái phát hành hóa đơn.</li>
        ///   <li><b>TT8</b> — Trạng thái xuất hóa đơn điện tử (HĐĐT).</li>
        /// </ul>
        /// <b>Trạng thái ký (từ ECtr_PublicInfo):</b>
        /// <ul>
        ///   <li><b>isSign</b> — true nếu HĐ đã có bản ghi ký trong ECtr_PublicInfo.</li>
        ///   <li><b>public_InvcCode</b> — InvcCode (chỉ có khi isSign = true).</li>
        ///   <li><b>sign_Crt_Date</b> — ngày + giờ ký (null nếu chưa ký).</li>
        ///   <li><b>is_Party_A_Sign / is_Party_B_Sign</b> — Bên A / Bên B đã ký hay chưa.</li>
        /// </ul>
        /// </remarks>
        /// <param name="fromDate">Từ ngày (yyyy-MM-dd). Mặc định: 30 ngày trước.</param>
        /// <param name="toDate">Đến ngày (yyyy-MM-dd). Mặc định: hôm nay.</param>
        /// <param name="strSearch">Tìm theo tên KH, mã số thuế hoặc mã hợp đồng.</param>
        /// <param name="statusFilter">Lọc theo currSignNumb (0 / 101 / 301 / 501).</param>
        /// <param name="page">Số trang, bắt đầu từ 1.</param>
        /// <param name="pageSize">Số dòng mỗi trang.</param>
        [HttpGet("econtracts/by-date")]
        [ProducesResponseType(typeof(PagedEContractByDateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedEContractByDateResult>> GetByDate(
                [FromQuery] string? fromDate = null,
                [FromQuery] string? toDate = null,
                [FromQuery] string? strSearch = null,
                [FromQuery] int? statusFilter = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                CancellationToken ct = default)
        {
            if (!ValidateKLKey())
            {
                _logger.LogWarning("[Partner] BLOCKED — IP={IP}",
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new { success = false, message = "Unauthorized." });
            }

            int maxPageSize = int.TryParse(_config["Partner:PageSize"], out var ps)
                                ? Math.Min(ps, 500) : 200;
            pageSize = Math.Clamp(pageSize, 1, maxPageSize);

            string frm = string.IsNullOrWhiteSpace(fromDate)
                ? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd")
                : fromDate.Trim();

            string end = string.IsNullOrWhiteSpace(toDate)
                ? DateTime.Now.ToString("yyyy-MM-dd")
                : toDate.Trim();

            _logger.LogInformation("[Partner] BY-DATE — {From}→{To} page={Page} size={Size}",
                frm, end, page, pageSize);

            try
            {
                var result = await _partnerRepo.GetContractsByDateOnlyAsync(
                    frm, end, strSearch, statusFilter, page, pageSize, ct);

                return Ok(new
                {
                    success = true,
                    fromDate = frm,
                    toDate = end,
                    totalCount = result.TotalCount,
                    page = result.Page,
                    pageSize = result.PageSize,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Partner] BY-DATE ERROR");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống." });
            }
        }
    }
}
