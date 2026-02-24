namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IMailService
    {
        /// <summary>
        /// Gửi email thông báo trình ký hợp đồng tới kế toán.
        /// Fire-and-forget: không ném exception nếu gửi thất bại (chỉ log).
        /// </summary>
        Task SendProposeSignNotificationAsync(
            string oid,
            string cusTax,
            string cusName,
            string saleFullName,
            string ktName);
    }
}
