using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.AccountKeToan;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ISalesHierarchyService
    {
        Task<List<ManagerDto>> GetManagerTreeAsync(string clnID, bool isManager);
        Task<RegistrationResultDto> HandleSaleRegistrationAsync(SaleRegistrationModel request);
        Task<AccountingRegistrationResultDto> HandleAccountingRegistrationAsync(
            AccountingRegistrationRequestDto request);
        /// <summary>Tạo lại TK hệ thống ngoài (LOT ERP) cho nhân viên đã tồn tại (retry khi đồng bộ lỗi).</summary>
        Task<ApiResponse<object>> RetryCreateHrAccountAsync(CreateHrAccountRequest request);
    }
}
