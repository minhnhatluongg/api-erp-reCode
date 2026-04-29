using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IReconcileDetailService
    {
        // ==========================================================
        //  QUERY
        // ==========================================================

        /// <summary>Lấy 1 dòng theo ID. Throw NotFound nếu không có.</summary>
        Task<PaymentReconcileDetail> GetByIdAsync(long detailId, CancellationToken ct = default);

        /// <summary>Danh sách dòng của 1 phiếu.</summary>
        Task<IReadOnlyList<PaymentReconcileDetail>> GetByReconcileIdAsync(
            long reconcileId, CancellationToken ct = default);

        /// <summary>Lịch sử thanh toán của 1 hợp đồng (tất cả phiếu liên quan).</summary>
        Task<IReadOnlyList<PaymentReconcileDetail>> GetByContractAsync(
            string contractOID, int? contractItemNo = null, CancellationToken ct = default);

        /// <summary>Công nợ còn lại của 1 hợp đồng (để autofill khi nhập phiếu mới).</summary>
        Task<decimal> GetRemainingDebtByContractAsync(
            string contractOID, int? contractItemNo = null, CancellationToken ct = default);


        // ==========================================================
        //  COMMAND
        // ==========================================================

        /// <summary>Thêm 1 dòng chi tiết vào phiếu. Sau khi thêm sẽ auto recalc totals header.</summary>
        Task<long> AddAsync(
            long reconcileId,
            CreateReconcileDetailDto dto,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>Bulk add nhiều dòng (dùng TVP). Auto recalc totals header sau khi add.</summary>
        Task<int> BulkAddAsync(
            long reconcileId,
            IEnumerable<CreateReconcileDetailDto> details,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>Cập nhật 1 dòng. Auto recalc totals.</summary>
        Task<bool> UpdateAsync(
            UpdateReconcileDetailDto dto,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>Xoá 1 dòng. Auto recalc totals header.</summary>
        Task<bool> DeleteAsync(long detailId, string currentUser, CancellationToken ct = default);

        /// <summary>Xoá toàn bộ dòng của 1 phiếu (thường gọi trước khi replace-all khi edit chi tiết).</summary>
        Task<int> DeleteByReconcileAsync(
            long reconcileId, string currentUser, CancellationToken ct = default);
    }
}
