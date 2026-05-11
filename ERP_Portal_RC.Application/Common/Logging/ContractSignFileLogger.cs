using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    /// <summary>
    /// File logger cho ContractSign API (save-signed-xml, ...).
    /// Ghi vào: Logs/ExternalApi/ContractSign_{yyyy-MM-dd}.log
    /// </summary>
    public class ContractSignFileLogger : ExternalApiFileLogger
    {
        public ContractSignFileLogger(IConfiguration configuration)
            : base(configuration, "ContractSign") { }
    }
}
