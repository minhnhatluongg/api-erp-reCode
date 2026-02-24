namespace ERP_Portal_RC.Application.DTOs
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = "WIN TECH ERP";
        /// <summary>Email kế toán nhận thông báo trình ký hợp đồng.</summary>
        public string ReceiverKeToan { get; set; } = "ketoanhoadondientu@win-tech.vn";
        public string PortalBaseUrl { get; set; } = "http://www.winerp.org:8081";
    }
}
