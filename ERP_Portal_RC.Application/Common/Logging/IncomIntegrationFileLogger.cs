using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    /// <summary>
    /// File logger chuyên dùng cho IntegrationIncomEcontract (mini app).
    /// Ghi vào: Logs/ExternalApi/IncomIntegration_{yyyy-MM-dd}.log
    /// Retention: 14 ngày (config ExternalApiLogConfig:RetentionDays).
    /// </summary>
    public class IncomIntegrationFileLogger : ExternalApiFileLogger
    {
        public IncomIntegrationFileLogger(IConfiguration configuration)
            : base(configuration, "IncomIntegration") { }
    }
}
