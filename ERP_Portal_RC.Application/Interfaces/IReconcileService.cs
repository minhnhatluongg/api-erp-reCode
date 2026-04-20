using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Filters;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IReconcileService
    {
        // ==========================================================
        //  QUERY
        // ==========================================================

        /// <summary>Tìm kiếm + phân trang.</summary>
        Task<PagedResult<PaymentReconcile>> SearchAsync(
            ReconcileFilter filter, CancellationToken ct = default);

        /// <summary>Lấy phiếu theo ID. Throw <c>NotFoundException</c> nếu không có.</summary>
        Task<PaymentReconcile> GetByIdAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>Lấy phiếu theo mã. Throw <c>NotFoundException</c> nếu không có.</summary>
        Task<PaymentReconcile> GetByCodeAsync(string reconcileCode, CancellationToken ct = default);

        /// <summary>Lấy full (header + details + history) cho trang chi tiết.</summary>
        Task<PaymentReconcile> GetFullAsync(string reconcileCode, CancellationToken ct = default);

        /// <summary>Lấy dữ liệu render PDF in phiếu.</summary>
        Task<PaymentReconcile> GetForPrintAsync(long reconcileId, CancellationToken ct = default);

        /// <summary>Lịch sử chuyển trạng thái.</summary>
        Task<IReadOnlyList<PaymentStateHistory>> GetHistoryAsync(
            long reconcileId, CancellationToken ct = default);

        /// <summary>Các action có thể thực hiện trên phiếu — dựng động button "Tác vụ".</summary>
        Task<IReadOnlyList<WorkflowState>> GetAvailableActionsAsync(
            long reconcileId, string? role, CancellationToken ct = default);


        // ==========================================================
        //  COMMAND
        // ==========================================================

        /// <summary>
        /// Tạo phiếu mới. Tự động:
        /// - Sinh <c>ReconcileCode</c> (sp_Reconcile_NextCode).
        /// - Pick <c>WorkflowID</c> default nếu DTO không truyền.
        /// - Set state khởi tạo (IsInitial=1).
        /// - Tính <c>RemainingAmount</c> = Total - Paid (nếu client không truyền chuẩn).
        /// - Validate details (tổng tiền ≤ giá trị hợp đồng, PayingAmount &gt; 0 ...).
        /// </summary>
        Task<long> CreateAsync(
            CreateReconcileDto dto,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>Cập nhật thông tin header. Chỉ cho sửa khi state còn <c>IsInitial</c> hoặc DRAFT.</summary>
        Task<bool> UpdateHeaderAsync(
            long reconcileId,
            UpdateReconcileHeaderDto dto,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>Huỷ phiếu (soft delete). Chỉ cho phép khi state chưa phải FINAL.</summary>
        Task<bool> DeleteAsync(
            long reconcileId,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>Nhân bản phiếu — tạo phiếu mới từ phiếu cũ, state = Initial, code mới.</summary>
        Task<long> DuplicateAsync(
            long sourceReconcileId,
            string currentUser,
            CancellationToken ct = default);

        /// <summary>
        /// Chuyển trạng thái phiếu. Validate:
        /// - Transition có trong WorkflowTransition.
        /// - Role được phép chuyển.
        /// - Note bắt buộc nếu state đích IsRejected.
        /// </summary>
        Task<bool> TransitionAsync(
            long reconcileId,
            TransitionStateDto dto,
            string currentUser,
            string? currentRole,
            CancellationToken ct = default);

        /// <summary>Tính lại Total/Paid/Remaining từ details (gọi sau khi edit chi tiết).</summary>
        Task<bool> RecalcTotalsAsync(long reconcileId, CancellationToken ct = default);
    }
}
