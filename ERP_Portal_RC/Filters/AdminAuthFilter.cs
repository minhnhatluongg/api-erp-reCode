using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.ERP_Portal_RC.Filters
{
    /// <summary>
    /// Chặn request nếu UserCode trong JWT không nằm trong danh sách Admin.AllowedUserCodes.
    /// Dùng [TypeFilter(typeof(AdminAuthFilter))] trên Controller hoặc Action.
    /// </summary>
    public class AdminAuthFilter : IAuthorizationFilter
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdminAuthFilter> _logger;

        public AdminAuthFilter(IConfiguration config, ILogger<AdminAuthFilter> logger)
        {
            _config  = config;
            _logger  = logger;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userCode = context.HttpContext.User.FindFirst("UserCode")?.Value;

            if (string.IsNullOrEmpty(userCode))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Phiên đăng nhập hết hạn hoặc không hợp lệ."
                });
                return;
            }

            var allowed = _config.GetSection("Admin:AllowedUserCodes")
                                 .Get<string[]>() ?? Array.Empty<string>();

            if (!allowed.Contains(userCode, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[Admin] FORBIDDEN — UserCode={U}", userCode);
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "Bạn không có quyền truy cập chức năng này."
                })
                { StatusCode = StatusCodes.Status403Forbidden };
            }
        }
    }
}
