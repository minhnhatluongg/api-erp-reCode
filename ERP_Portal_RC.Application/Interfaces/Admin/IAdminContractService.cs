using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces.Admin
{
    public interface IAdminContractService
    {
        /// <summary>
        /// Admin xem danh sách hợp đồng — ViewAll = true (bỏ qua filter team).
        /// FilterSaleEmID = null → tất cả HĐ trong khoảng ngày.
        /// FilterSaleEmID = 'xxx' → chỉ HĐ của NV đó và cây bên dưới.
        /// </summary>
        Task<ApiResponse<EContractPagedResponsePage>> GetAllContractsAsync(
            string         adminUserCode,
            string?        filterSaleEmID,
            string?        searchKeyword,
            string         fromDate,
            string         endDate,
            int?           statusFilter,
            int            page,
            int            pageSize);
    }
}
