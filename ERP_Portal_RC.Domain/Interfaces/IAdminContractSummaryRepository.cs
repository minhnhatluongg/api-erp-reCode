using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IAdminContractSummaryRepository
    {
        /// <summary>
        /// Gọi SP sp_GetEContract_Summary — trả 5 resultsets.
        /// Null nếu không tìm thấy hợp đồng.
        /// </summary>
        Task<ContractSummaryResponse?> GetSummaryAsync(string oid);
    }
}
