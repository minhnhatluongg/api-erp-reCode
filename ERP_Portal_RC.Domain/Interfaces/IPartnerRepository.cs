using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IPartnerRepository
    {
        /// <summary>
        /// Lấy danh sách hợp đồng theo cây ASM (SP: wspList_EContracts_PagedV26ASM_FIX_1).
        /// Dùng cho Partner API — managerCode lấy từ config.
        /// </summary>
        Task<List<EContract_Monitor_Refactor>> GetContractsByDateAsync(
            string managerCode,
            string fromDate,
            string toDate,
            int page,
            int pageSize);
        /// <summary>
        /// Lấy danh sách hợp đồng chỉ filter theo ngày, không cây ASM
        /// (SP: wspList_EContracts_PagedByDate).
        /// Trả kèm TotalCount để phân trang.
        /// </summary>
        Task<PagedEContractByDateResult> GetContractsByDateOnlyAsync(
            string fromDate,
            string toDate,
            string? strSearch = null,
            int? statusFilter = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);
    }
}
