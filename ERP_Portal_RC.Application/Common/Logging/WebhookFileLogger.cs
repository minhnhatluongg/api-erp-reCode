using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Domain.Common.Logging
{
    /// <summary>
    /// File logger chuyên dùng cho Webhook API (request-invoice + invoice-exported).
    /// Ghi vào: Logs/ExternalApi/Webhook_{yyyy-MM-dd}.log
    /// </summary>
    public class WebhookFileLogger : ExternalApiFileLogger
    {
        public WebhookFileLogger(IConfiguration configuration)
            : base(configuration, "Webhook") { }
    }
}
