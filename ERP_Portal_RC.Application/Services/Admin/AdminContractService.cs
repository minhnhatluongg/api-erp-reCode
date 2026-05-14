using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;

namespace ERP_Portal_RC.Application.Services.Admin
{
    public class AdminContractService : IAdminContractService
    {
        private readonly IEContractV26Repository _repo;

        public AdminContractService(IEContractV26Repository repo)
        {
            _repo = repo;
        }

        public async Task<ApiResponse<EContractPagedResponsePage>> GetAllContractsAsync(
            string  adminUserCode,
            string? filterSaleEmID,
            string? searchKeyword,
            string  fromDate,
            string  endDate,
            int?    statusFilter,
            int     page,
            int     pageSize)
        {
            try
            {
                // Admin luôn dùng ViewAll = true → không bị giới hạn bởi cây ASM
                // CrtUser dùng adminUserCode để build SubEmpl_Root hiển thị sidebar
                // FilterSaleEmID = null → tất cả | 'xxx' → lọc theo NV cụ thể
                var (data, subEmpl, meta) = await _repo.GetAllPagedAsync(
                    crtUser:        adminUserCode,
                    frmDate:        fromDate,
                    endDate:        endDate,
                    search:         searchKeyword,
                    statusFilter:   statusFilter,
                    filterSaleEmID: filterSaleEmID,
                    viewAll:        true,   // ← Admin luôn xem tất cả
                    page:           page,
                    pageSize:       pageSize);

                var result = new EContractPagedResponsePage
                {
                    Data       = data.ToList(),
                    SubEmpl    = subEmpl.ToList(),
                    TotalCount = meta.TotalCount,
                    Page       = meta.Page,
                    PageSize   = meta.PageSize
                };

                return ApiResponse<EContractPagedResponsePage>.SuccessResponse(
                    result,
                    $"[Admin] Trang {meta.Page}/{meta.TotalPages}. Tổng: {meta.TotalCount} hợp đồng.");
            }
            catch (Exception ex)
            {
                return ApiResponse<EContractPagedResponsePage>.ErrorResponse(
                    $"Lỗi hệ thống: {ex.Message}", 500);
            }
        }
    }
}
