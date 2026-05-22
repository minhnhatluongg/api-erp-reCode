using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// Endpoint debug — confirm logger ghi được sau khi deploy.
    /// KHÔNG dùng auth (chỉ admin internal gọi qua /api/logger-health/*).
    /// </summary>
    [ApiController]
    [Route("api/logger-health")]
    [Produces("application/json")]
    public class LoggerHealthController : ControllerBase
    {
        private readonly EContractFileLogger _createAccountLogger;
        private readonly EContractFileLogger _jobInsertLogger;

        public LoggerHealthController(
            [FromKeyedServices("CreateAccountLogger")] EContractFileLogger createAccountLogger,
            [FromKeyedServices("JobInsertLogger")] EContractFileLogger jobInsertLogger)
        {
            _createAccountLogger = createAccountLogger;
            _jobInsertLogger = jobInsertLogger;
        }

        /// <summary>
        /// Xem trạng thái logger (path đang dùng, có write được không, file hôm nay đã tồn tại chưa).
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                CreateAccountLogger = _createAccountLogger.GetStatus(),
                JobInsertLogger = _jobInsertLogger.GetStatus()
            }));
        }

        /// <summary>
        /// Ghi 1 dòng test để verify file output (gọi rồi xem file _init_/*.log + dòng test).
        /// </summary>
        [HttpPost("ping")]
        public async Task<IActionResult> Ping()
        {
            var stamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            await _createAccountLogger.LogInfoAsync("PING", $"Ping from /api/logger-health/ping @ {stamp}");
            await _jobInsertLogger.LogInfoAsync("PING", $"Ping from /api/logger-health/ping @ {stamp}");

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Stamp = stamp,
                CreateAccountLogger = _createAccountLogger.GetStatus(),
                JobInsertLogger = _jobInsertLogger.GetStatus()
            }, "Đã ghi log test — kiểm tra file để confirm."));
        }

        /// <summary>
        /// Force ghi marker _init_*.log NGAY — dùng khi admin muốn xác nhận thủ công
        /// logger còn sống mà không cần phát sinh log nghiệp vụ.
        /// Lưu ý: marker được ghi tối đa 1 lần/process/prefix (cờ static), nên gọi
        /// nhiều lần KHÔNG spam file. Restart app mới reset cờ.
        /// </summary>
        [HttpPost("write-init-marker")]
        public IActionResult WriteInitMarker()
        {
            _createAccountLogger.WriteInitMarkerNow();
            _jobInsertLogger.WriteInitMarkerNow();
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                CreateAccountLogger = _createAccountLogger.GetStatus(),
                JobInsertLogger = _jobInsertLogger.GetStatus()
            }, "Đã ghi marker init (nếu chưa có trong process này)."));
        }
    }
}
