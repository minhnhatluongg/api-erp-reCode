using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ISalesHierarchyRepository
    {
        Task<IEnumerable<EmployeeTreeItem>> GetRawSalesTreeAsync(string clnID);
        Task<string> RegisterSaleHierarchyAsync(SaleRegistrationModel request, string hardcodedAdminId);
        Task<string> CreateERPAccountOnlyAsync(string loginName, string password, string fullName, string email, string emplId);
        Task<Dictionary<string, string>> GetLoginNameBatchAsync(IEnumerable<string> userCodes);

        /// <summary>
        /// Gọi API bên ngoài để tạo tài khoản HR trên hệ thống khác.
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> CreateHRAccountAsync(string fullName, string email, string phone, string username, string password, string winId);
    }
}
