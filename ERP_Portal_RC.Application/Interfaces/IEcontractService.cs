using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.Count_Invoice;
using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Http;
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
        Task<PagedResponse<EContract_Monitor>> GetContractListAsync(string userCode, string userName, string grpList, ContractSearchRequest request);
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

        Task<ApiResponse<string>> GenerateContractPreviewAsync(ContractPreviewRequest request);
        Task<ApiResponse<string>> ProcessSaveContractAsync(ContractPreviewRequest request, string userCode);
        Task<ContractStatusResponse> GetContractReviewDataAsync(string oid);
        Task<ApiResponse<object>> DeleteDraftAsync(DeleteEcontractRequest request, string username);
        Task<ApiResponse<object>> UnSignAsync(UnSignRequest model);
        // Lấy lịch sử Job của hợp đồng
        Task<ApiResponse<EContractHistoryResponse>> GetJobHistoryAsync(string oid);
        //Check yêu cầu kiểm tra của kd/sale.
        Task<List<JobEntity>> GetJobKTbyOID(string oid);
        Task<ApiResponse<List<EContractDetails>>> GetEContractDetailsActionAsync(string oid);
        Task<ApiResponse<EContractDetailsViewModel>> GetJobDetailsAsync(string oid, string kt = "0");
        Task HandleAutomaticJobAsync(EContractDetailsViewModel model, EContractHistoryRaw2 raw, string oid, string kt);
        Task<PagedResponse<DepartmentDTO>> GetDepartmentsPagedAsync(string operDeptList, int pageNumber, int pageSize);
        Task<ApiResponse<List<EContractDetailDTO>>> VerifyJobDetailsAsync(string cusTax, string oid);
        Task<ApiResponse<List<string>>> UploadContractFilesAsync(IFormFileCollection files, string oid);
        Task<ApiResponse<object>> SaveJobAsync(SaveJobRequestDto request, string userCode);
        Task<ApiResponse<object>> ApproveJobNowAsync(ApproveJobRequestDto request, string userCode, string fullName);
        Task<EContractsViewModel> GetContractDetailForDisplayAsync(string oid, string userCode, string grpList, string firstClaimValue);
        Task<bool> CheckIfSubmitted(string oid);
        Task<ApiResponse<IEnumerable<object>>> GetListFilesByOidAsync(string oid);
        Task<ApiResponse<string>> GetNextJobOIDAsync(string mainOid);
        Task<ApiResponse<string>> CreateJobAsync(InsertJobRequest request);
        Task<ApiResponse<JobStatusResponse>> GetJobStatusAsync(string referenceId, string factorId, string entryId);
        Task<ApiResponse<IEnumerable<object>>> GetAttachmentsByOidAsync(string oid);
        //Task<ApiResponse<object>> AddMoreFilesAsync(string oid, string factorId, string entryId, List<string> fileLinks, string currentUser);
        Task<bool> CreateOrderAsync(EContractIntegrationRequestDto model, string merchantId, string orderOid, string crtUser);
        Task<bool> OrderExistsAsync(string orderOid);
        Task<OwnerContract> GetOwnerContractAsync(string companyId = "26");
        Task<bool> CheckOrderBySaleAsync(string cusTax, string saleEmID);
        Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> DeXuatCapTaiKhoanAsync(
            DeXuatCapTaiKhoanRequestDto request);

        Task<ApiResponse<InvCounterResponseDto>> GetInvCounterByMSTAsync(InvCounterRequestDto request);
        Task<IEnumerable<EContract101Response>> GetWaitingContracts(string frmDate, string endDate);
    } 
}
