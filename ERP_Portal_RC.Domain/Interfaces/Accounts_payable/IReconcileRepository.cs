using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Filters;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces.Accounts_payable
{
    public interface IReconcileRepository
    {
        // ==========================================================
        //  QUERY
        // ==========================================================

        /// <summary>
        /// Tìm kiếm + phân trang danh sách phiếu.
        /// SP: <c>sp_Reconcile_Search</c>
        /// </summary>
        Task<PagedResult<PaymentReconcile>> SearchAsync(
            ReconcileFilter filter,
            CancellationToken ct = default);

        /// <summary>
        /// Lấy phiếu theo ID (chỉ header, không bao gồm details).
        /// SP: <c>sp_Reconcile_GetById</c>
        /// </summary>
        Task<PaymentReconcile?> GetByIdAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>
        /// Lấy phiếu theo mã (VD "RECONCILE-2604-119095").
        /// SP: <c>sp_Reconcile_GetByCode</c>
        /// </summary>
        Task<PaymentReconcile?> GetByCodeAsync(string reconcileCode, CancellationToken ct = default);

        /// <summary>
        /// Lấy full header + details + current state + history (load 1 lần cho form chi tiết).
        /// SP: <c>sp_Reconcile_GetByCode</c> (trả nhiều result set)
        /// </summary>
        Task<PaymentReconcile?> GetFullAsync(string reconcileCode, CancellationToken ct = default);

        /// <summary>
        /// Lấy dữ liệu phục vụ in phiếu (PDF).
        /// SP: <c>sp_Reconcile_GetForPrint</c>
        /// </summary>
        Task<PaymentReconcile?> GetForPrintAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>
        /// Lịch sử chuyển trạng thái của phiếu.
        /// SP: <c>sp_Reconcile_GetHistory</c>
        /// </summary>
        Task<IReadOnlyList<PaymentStateHistory>> GetHistoryAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>
        /// Các state có thể chuyển tới từ state hiện tại (theo <c>WorkflowTransition</c>).
        /// Dùng render button "Tác vụ" động.
        /// SP: <c>sp_Reconcile_GetAvailableActions</c>
        /// </summary>
        Task<IReadOnlyList<WorkflowState>> GetAvailableActionsAsync(
            long reconcileId,
            string? role,
            CancellationToken ct = default);

        /// <summary>
        /// Sinh mã phiếu kế tiếp, VD "RECONCILE-2604-119096".
        /// SP: <c>sp_Reconcile_NextCode</c>
        /// </summary>
        Task<string> GenerateNextCodeAsync(int serviceTypeId, CancellationToken ct = default);


        // ==========================================================
        //  COMMAND
        // ==========================================================

        /// <summary>
        /// Tạo phiếu mới: insert header + details + ghi history (state khởi tạo) — trong 1 transaction.
        /// SP: <c>sp_Reconcile_Create</c>
        /// </summary>
        /// <returns>ReconcileID vừa tạo.</returns>
        Task<long> CreateAsync(
            PaymentReconcile header,
            IEnumerable<PaymentReconcileDetail> details,
            CancellationToken ct = default);

        /// <summary>
        /// Cập nhật thông tin header (Sửa).
        /// SP: <c>sp_Reconcile_UpdateHeader</c>
        /// </summary>
        Task<bool> UpdateHeaderAsync(PaymentReconcile header, CancellationToken ct = default);

        /// <summary>
        /// Hủy phiếu (soft delete — set IsDeleted hoặc chuyển về state CANCELLED).
        /// SP: <c>sp_Reconcile_Delete</c>
        /// </summary>
        Task<bool> DeleteAsync(long reconcileId, string actionUser, CancellationToken ct = default);

        /// <summary>
        /// Nhân bản phiếu (copy header + details, state = Initial).
        /// SP: <c>sp_Reconcile_Duplicate</c>
        /// </summary>
        /// <returns>ReconcileID của phiếu mới.</returns>
        Task<long> DuplicateAsync(long sourceReconcileId, string actionUser, CancellationToken ct = default);

        /// <summary>
        /// Tính lại TotalAmount / PaidAmount / RemainingAmount dựa trên details.
        /// SP: <c>sp_Reconcile_RecalcTotals</c>
        /// </summary>
        Task<bool> RecalcTotalsAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>
        /// Chuyển trạng thái phiếu: validate transition hợp lệ → update CurrentStateID → insert history (1 transaction).
        /// SP: <c>sp_Reconcile_Transition</c>
        /// </summary>
        /// <param name="reconcileId">ID phiếu.</param>
        /// <param name="toStateId">StateID đích.</param>
        /// <param name="actionUser">User thực hiện.</param>
        /// <param name="note">Ghi chú (bắt buộc nếu chuyển sang state IsRejected).</param>
        Task<bool> TransitionStateAsync(
            long reconcileId,
            int toStateId,
            string actionUser,
            string? note,
            CancellationToken ct = default);
    }
}
