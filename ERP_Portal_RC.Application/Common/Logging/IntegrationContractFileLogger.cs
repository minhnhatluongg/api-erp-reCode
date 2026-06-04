using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    /// <summary>
    /// File logger chuyên dùng cho IntegrationContract (quick-create + ghost-contract).
    /// Ghi vào: Logs/ExternalApi/IntegrationContract_{yyyy-MM-dd}.log
    /// Retention: theo config ExternalApiLogConfig:RetentionDays.
    /// </summary>
    public class IntegrationContractFileLogger : ExternalApiFileLogger
    {
        public IntegrationContractFileLogger(IConfiguration configuration)
            : base(configuration, "IntegrationContract") { }
    }
}
