using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IEContractV26Repository
    {
        Task<(IEnumerable<EContract_Monitor> Data, IEnumerable<SubEmpl> SubEmpl, PageMeta Meta)> GetAllPagedAsync(
            string crtUser,
            string frmDate,
            string endDate,
            string? search,
            int? statusFilter,
            string? filterSaleEmID,
            int page,
            int pageSize);
    }
}
