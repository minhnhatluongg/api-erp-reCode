using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IEContractRepository
    {
        Task<DSMenuViewModel> GetDSMenuByID(string loginName, string grpCode);
        Task<ListEcontractViewModel> Search(string search, string crtUser, string dateStart, string dateEnd);
        Task<ListEcontractViewModel> GetAllList(string crtUser, string dateStart, string dateEnd);
        Task<ListEcontractViewModel> CountList(string crtUser, string dateStart, string dateEnd);
        Task CreateLog(string message, string userCode);
        Task<limitGHCNKD> CheckBCTT(string cmpnID , string saleID, string group);
        Task<List<string>> GetListOIDHasDetails(List<string> oids);

        // Hàm này là hàm Task<ListEcontractViewModel> SelectedCB trong ERP cũ
        Task<ListEcontractViewModel> GetEContractsByHierarchyAsync(string search, string emplChild, string dateStart, string dateEnd, string currentUserCode);

        Task<int> ExecuteApprovalWorkflow(ApprovalWorkflowRequest model, (string Factor, string Entry, int NextStep, string Sp) config, string userId);

        /// <summary>Lấy thông tin hợp đồng (CusTax, CusName) để gửi email.</summary>
        Task<(string CusTax, string CusName)> GetContractInfoForEmailAsync(string oid);

        /// <summary>
        /// Gọi Ins_EContractJobs_RequestByOdoo (trong BosOnline): tạo Job record + gọi zsgn_EContractJobs_NOR nội bộ.
        /// Dùng cho propose-template: contract OID 0 -> 101.
        /// </summary>
        Task<(bool success, string message)> CreateEContractJobAsync(
            ERP_Portal_RC.Domain.Entities.EContractJobRequest request, string userId);

        /// <summary>
        /// Tìm job OID từ contract OID rồi gọi zsgn_EContractJobs_NOR để nâng trạng thái.
        /// Dùng cho issue-invoice: job 101 -> 201.
        /// </summary>
        Task<(bool success, string message)> AdvanceEContractJobSigningAsync(
            string contractOid, string factorId, string entryId,
            string userId, int fromSignNumb, int toSignNumb, string? appvMess = null);

        Task<Template> GetTemplateByCodeAsync(string factorId);

        Task<string> SaveFullContractAsync(EContractMaster master, List<EContractDetails> details);
        Task CreateApprovalFlowAsync(EContractMaster master);
        Task<EContractStatusRaw> GetContractStatusRawAsync(string oid);
        // Nghiệp vụ xóa nháp
        Task<(int Ok, string Message)> DeleteDraftAsync(string oid, string username);
        // Nghiệp vụ hủy ký (Yêu cầu Transaction)
        Task<(bool Success, string Message, object Data)> UnSignAsync(UnSignRequest model, string correlationId);
        //Nghiệp vụ lịch sử Job 
        Task<EContractHistoryRaw> GetFullHistoryDataAsync(string oid);
        // Check job có yêu cầu kiểm tra của kd/sales chưa?
        Task<List<JobEntity>> GetJobKTbyOID(string oid);
        //Task<EContractsViewModel> GetByOIDJobKT(string OID = "", string KT = "");
        Task<List<EContractDetails>> GetEContractDetailsNewAsync(string oid);
        //Get job 
        Task<EContractHistoryRaw2> GetEContractRawDataAsync(string oid);
        Task DeleteJob01Async(string oid);
        Task<JobEntity> InsertJobAsync(JobEntity job);
        Task UploadFileAsync(JobEntity job);
        Task<List<DepartmentsEntity>> GetDepartmentsByOidAsync(string did);
        Task<List<EContractDetails>> VerifyJobAsync(string cusTax, string oid);
        Task UpdateJobSaveAsync(JobEntity job, List<JobPackEntity> jobPacks, int sumInvc, int? countChange, string info, string descript);
        Task<Limit> limitcn(string cmpnId, string saleId, string GroupItem);
        Task<List<ListFile>> GetListFilesAsync(string oid);
        Task<Right_EContracts?> GetRightEContractsAsync(int currSign, string grpList);
        Task<byte[]?> GetInvcContentXmlAsync(string oid);
        Task<EmailUserRawData?> GetEmailUserDeptAsync(string oid);
        Task<bool> ApproveContractJobAsync(ZsgnEContractJob entity, int holdSignNumb, int nextSignNumb);
        Task UpdateJobChangeAsync(JobEntity job, int? countChange, string info);
        Task<string> GetByOIDJobChangeAsync(string OID);
        //Task<JobEntity> InsertJobChangeYCAsync(JobEntity job); Code later

    }
}
