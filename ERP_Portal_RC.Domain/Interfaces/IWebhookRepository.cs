using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IWebhookRepository
    {
        /// <summary>Ghi log webhook vào DB.</summary>
        Task WriteLogAsync(WebhookLog log);

        /// <summary>
        /// Nâng SignNumb 101 → 201 cho JOB_00005/JB:010.
        /// </summary>
        Task<(bool Success, string Message)> AdvanceInvoiceExportedAsync(
            string contractOid,
            string userId = "WEBHOOK");

        /// <summary>
        /// Kế toán xem hóa đơn nháp → đảm bảo job JOB_00005/JB:010 tồn tại và ở SignNumb = 101.
        /// - Nếu job chưa có → tạo mới (từ dữ liệu EContracts + EContractDetails) rồi đẩy 0→101.
        /// - Nếu job đã có ở SignNumb = 0 → đẩy 0→101.
        /// - Nếu đã ở 101 hoặc 201 → idempotent, trả success.
        /// </summary>
        Task<(bool Success, string Message)> RequestInvoiceAsync(
            string contractOid,
            string userId = "WEBHOOK");
    }
}
