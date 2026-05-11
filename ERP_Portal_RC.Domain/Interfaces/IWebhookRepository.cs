using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IWebhookRepository
    {
        /// <summary>
        /// Nâng SignNumb 101 → 201 cho JOB_00005/JB:010.
        /// </summary>
        Task<(bool Success, string Message)> AdvanceInvoiceExportedAsync(
            string contractOid,
            string userId = "WEBHOOK");

        /// <summary>
        /// Kế toán xem hóa đơn nháp → đảm bảo job JOB_00005/JB:010 tồn tại và ở SignNumb = 101.
        /// </summary>
        Task<(bool Success, string Message)> RequestInvoiceAsync(
            string contractOid,
            string userId = "WEBHOOK");

        /// <summary>
        /// App đẩy hóa đơn đã xuất → tự động hoàn thành toàn bộ luồng:
        ///   - Chưa có job          → tạo job + 0→101 + 101→301 (1 lần gọi)
        ///   - Có job, SignNumb=0   → 0→101 → 101→301
        ///   - Có job, SignNumb=101 → 101→301 trực tiếp
        ///   - SignNumb=301         → idempotent
        /// </summary>
        Task<(bool Success, string Message)> AdvanceInvoiceExportedFullAsync(
            string contractOid,
            string userId = "WEBHOOK");
    }
}
