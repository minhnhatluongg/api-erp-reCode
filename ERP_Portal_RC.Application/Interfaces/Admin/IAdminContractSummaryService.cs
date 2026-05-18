using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces.Admin
{
    public interface IAdminContractSummaryService
    {
        /// <summary>Snapshot toàn bộ trạng thái 1 hợp đồng trong 1 call.</summary>
        Task<ApiResponse<ContractSummaryResponse>> GetSummaryAsync(string oid);
    }
}
