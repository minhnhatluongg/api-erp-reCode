using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces.Accounts_payable
{
    public interface IReconcileDetailRepository
    {
        // ==========================================================
        //  QUERY
        // ==========================================================

        /// <summary>
        /// Lấy 1 dòng chi tiết theo ID.
        /// SP: <c>sp_ReconcileDetail_GetById</c>
        /// </summary>
        Task<PaymentReconcileDetail?> GetByIdAsync(long detailId, CancellationToken ct = default);

        /// <summary>
        /// List toàn bộ dòng chi tiết của 1 phiếu.
        /// SP: <c>sp_ReconcileDetail_GetByReconcile</c>
        /// </summary>
        Task<IReadOnlyList<PaymentReconcileDetail>> GetByReconcileIdAsync(
            long reconcileId,
            CancellationToken ct = default);

        /// <summary>
        /// List dòng chi tiết liên quan đến 1 hợp đồng (dùng để tra cứu lịch sử thanh toán).
        /// SP: <c>sp_ReconcileDetail_GetByContract</c>
        /// </summary>
        Task<IReadOnlyList<PaymentReconcileDetail>> GetByContractAsync(
            string contractOID,
            int? contractItemNo = null,
            CancellationToken ct = default);


        // ==========================================================
        //  COMMAND
        // ==========================================================

        /// <summary>
        /// Thêm 1 dòng chi tiết vào phiếu.
        /// SP: <c>sp_ReconcileDetail_Add</c>
        /// </summary>
        /// <returns>DetailID vừa insert.</returns>
        Task<long> AddAsync(PaymentReconcileDetail detail, CancellationToken ct = default);

        /// <summary>
        /// Thêm nhiều dòng 1 lần (dùng Table-Valued Parameter).
        /// SP: <c>sp_ReconcileDetail_BulkAdd</c>
        /// </summary>
        /// <returns>Số dòng đã insert.</returns>
        Task<int> BulkAddAsync(
            long reconcileId,
            IEnumerable<PaymentReconcileDetail> details,
            CancellationToken ct = default);

        /// <summary>
        /// Cập nhật 1 dòng chi tiết (PayingAmount, RemainingAmount, Note...).
        /// SP: <c>sp_ReconcileDetail_Update</c>
        /// </summary>
        Task<bool> UpdateAsync(PaymentReconcileDetail detail, CancellationToken ct = default);

        /// <summary>
        /// Xoá 1 dòng chi tiết.
        /// SP: <c>sp_ReconcileDetail_Delete</c>
        /// </summary>
        Task<bool> DeleteAsync(long detailId, CancellationToken ct = default);

        /// <summary>
        /// Xoá toàn bộ dòng của 1 phiếu (dùng khi edit toàn bộ: xoá cũ → thêm mới).
        /// SP: <c>sp_ReconcileDetail_DeleteByReconcile</c>
        /// </summary>
        Task<int> DeleteByReconcileAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>
        /// Tính tổng cộng nợ còn lại của 1 hợp đồng (dùng cho lookup công nợ).
        /// SP: <c>sp_Lookup_ContractDebt</c>
        /// </summary>
        Task<decimal> GetRemainingDebtByContractAsync(
            string contractOID,
            int? contractItemNo = null,
            CancellationToken ct = default);
    }
}
