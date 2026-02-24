using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IEcontractService
    {
        // Lấy dữ liệu thống kê Dashboard cho EContract
        Task<DashboardStatsDto> GetContractDashboardAsync(
            string userCode,
            string userName,
            string grpList,
            bool isManager,
            ContractSearchRequest request);
        //Lấy danh sách hợp đồng đã qua lọc trạng thái cho Table
        Task<ListEcontractViewModel> GetContractListAsync(
            string userCode,
            string userName,
            string grpList,
            bool isManager,
            ContractSearchRequest request);
        //Get all EContracts with filters
        Task<EContractServiceResult> GetAllEContractsAsync(string userName, EContractFilterRequest request, string userCode, string groupList);

        //Trình kí / Yêu cầu phát hành mẫu / Phát Hành Mẫu
        Task<(bool success, bool emailSent)> ProposeSignContractAsync(ApprovalWorkflowRequest model, string userId, string saleFullName);
        Task<(bool success, string message)> ProposeTemplateAsync(ERP_Portal_RC.Domain.Entities.EContractJobRequest request, string userId);
        Task<(bool success, string message)> IssueInvoiceAsync(ApprovalWorkflowRequest model, string userId);

        //Lấy template 
        Task<Template?> GetTemplateByCodeAsync(string factorId);

        // 3 Phương thức cụ thể bạn yêu cầu (Hardcode logic sẽ nằm trong Implementation)
        Task<Template?> GetOriginalContractAsync();      // factorId: TT78_EContract
        Task<Template?> GetCompensationContractAsync();  // factorId: TT78_EContractExt
        Task<Template?> GetExtensionContractAsync();     // factorId: TT78_EContractExt1
    }
}
