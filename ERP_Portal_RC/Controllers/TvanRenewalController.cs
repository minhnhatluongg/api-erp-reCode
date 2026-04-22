using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/tvan-renewals")]
    public class TvanRenewalController : ControllerBase
    {
        private readonly ITvanRenewalService _service;

        public TvanRenewalController(ITvanRenewalService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách hợp đồng TVAN sắp hết hạn (có phân trang + tìm kiếm).
        /// </summary>
        /// <remarks>
        /// Dùng để nhắc Sale theo dõi và liên hệ khách gia hạn phí duy trì TVAN.
        /// ### Quy ước Status / RangeKey
        /// | RangeKey  | Ý nghĩa                | Điều kiện (DaysRemaining) |
        /// |-----------|------------------------|---------------------------|
        /// | EXPIRED   | Đã hết hạn             | &lt; 0                    |
        /// | D7        | Rất gấp                | 0 – 7                     |
        /// | D15       | Gấp                    | 8 – 15                    |
        /// | D30       | Sắp hết hạn            | 16 – 30                   |
        /// | M3        | Trong 3 tháng          | 31 – 90                   |
        /// | SAFE      | Còn nhiều              | &gt; 90                   |
        ///
        /// </remarks>
        /// <param name="query">Bộ tham số truy vấn: phân trang, tìm kiếm, lọc theo range.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Trả về danh sách HĐ TVAN sắp hết hạn đã phân trang.</response>
        /// <response code="400">Tham số không hợp lệ (ví dụ: page &lt; 1, size vượt giới hạn).</response>
        /// <response code="500">Lỗi server / DB.</response>
        [HttpGet("expiring-soon")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TvanRenewalItemDto>>), 200)]
        public async Task<IActionResult> GetExpiringSoon(
            [FromQuery] TvanRenewalQueryDto query, CancellationToken ct)
        {
            var data = await _service.GetExpiringSoonAsync(query, ct);

            var response = ApiResponse<PagedResult<TvanRenewalItemDto>>
                .SuccessResponse(data, "Lấy danh sách hợp đồng TVAN sắp hết hạn thành công");

            return Ok(response);
        }
    }
}