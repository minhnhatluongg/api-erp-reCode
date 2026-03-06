using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IMailService
    {
        Task SendProposeSignNotificationAsync(
            string oid,
            string cusTax,
            string cusName,
            string saleFullName,
            string ktName);

        // Hàm xử lý thông báo duyệt Job (Tạo mẫu / Phát hành)
        Task SendApproveNotificationAsync(
            EmailUserDept dept,
            EContractMaster master,
            string oid,
            string factorId);
    }
}
