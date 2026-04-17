using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces.Accounts_payable
{
    public interface IWorkflowRepository
    {
        // ─────────────────────────────────────────────────────────────────────
        // WORKFLOW — QUERY
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_Workflow_GetByServiceType
        /// Lấy danh sách workflow theo loại dịch vụ.
        /// Thường 1 ServiceType có 1 workflow, nhưng thiết kế cho phép nhiều version.
        /// </summary>
        /// <param name="serviceTypeId">FK ServiceType</param>
        Task<IEnumerable<Workflow>> GetByServiceTypeAsync(int serviceTypeId);

        /// <summary>
        /// Lấy chi tiết 1 workflow kèm danh sách State và Transition.
        /// Dùng để render toàn bộ quy trình trên UI (tabs tím + mũi tên chuyển state).
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        /// <param name="workflowId">PK của Workflow</param>
        Task<Workflow?> GetByIdWithDetailsAsync(int workflowId);

        // ─────────────────────────────────────────────────────────────────────
        // WORKFLOW — COMMAND
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_Workflow_Create
        /// Tạo workflow mới, trả về WorkflowID vừa INSERT.
        /// Thường được gọi kèm với AddStateAsync để khởi tạo state ban đầu.
        /// </summary>
        /// <param name="entity">Dữ liệu Workflow cần tạo</param>
        /// <returns>WorkflowID vừa INSERT</returns>
        Task<int> CreateWorkflowAsync(Workflow entity);

        /// <summary>
        /// Cập nhật tên / mô tả workflow. Không đổi được WorkflowCode sau khi đã có PaymentRecord dùng.
        /// </summary>
        Task<bool> UpdateWorkflowAsync(Workflow entity);

        // ─────────────────────────────────────────────────────────────────────
        // WORKFLOW STATE — QUERY
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_Workflow_GetStates
        /// Lấy danh sách State theo thứ tự StateOrder — render thành tabs tím trên UI.
        /// Trả về cả state IsActive = false để hiển thị lịch sử.
        /// </summary>
        /// <param name="workflowId">PK của Workflow</param>
        Task<IEnumerable<WorkflowState>> GetStatesAsync(int workflowId);

        /// <summary>
        /// Lấy 1 State theo ID. Dùng khi cần check IsInitial / IsFinal trước khi chuyển state.
        /// </summary>
        Task<WorkflowState?> GetStateByIdAsync(int stateId);

        // ─────────────────────────────────────────────────────────────────────
        // WORKFLOW STATE — COMMAND
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_Workflow_AddState
        /// Thêm state mới vào workflow.
        /// StateOrder tự động gán = max(StateOrder) + 1 nếu không truyền vào.
        /// Chỉ được có 1 IsInitial = true và 1 IsFinal = true trong cùng 1 workflow.
        /// </summary>
        /// <param name="state">Dữ liệu state cần thêm</param>
        /// <returns>StateID vừa INSERT</returns>
        Task<int> AddStateAsync(WorkflowState state);

        /// <summary>
        /// sp_Workflow_UpdateState
        /// Sửa tên, màu sắc, thứ tự của state.
        /// Không đổi StateCode sau khi đã có PaymentRecord ở state này.
        /// </summary>
        Task<bool> UpdateStateAsync(WorkflowState state);

        /// <summary>
        /// Soft-delete state: set IsActive = false.
        /// Không xóa vật lý vì PaymentStateLog tham chiếu StateID.
        /// </summary>
        Task<bool> DeactivateStateAsync(int stateId);

        // ─────────────────────────────────────────────────────────────────────
        // WORKFLOW TRANSITION — QUERY
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_Workflow_GetTransitions
        /// Lấy toàn bộ transition của workflow — dùng để vẽ luồng chuyển state.
        /// Bao gồm cả transition ngược chiều (ví dụ: Từ chối → quay về Đang bổ sung).
        /// </summary>
        /// <param name="workflowId">PK của Workflow</param>
        Task<IEnumerable<WorkflowTransition>> GetTransitionsAsync(int workflowId);

        /// <summary>
        /// Lấy các transition hợp lệ từ 1 state — dùng khi render nút action trên UI.
        /// Ví dụ: đang ở "Chờ duyệt" → cho phép "Duyệt" hoặc "Từ chối".
        /// </summary>
        /// <param name="fromStateId">StateID hiện tại của PaymentRecord</param>
        Task<IEnumerable<WorkflowTransition>> GetAvailableTransitionsAsync(int fromStateId);

        // ─────────────────────────────────────────────────────────────────────
        // WORKFLOW TRANSITION — COMMAND
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_Workflow_AddTransition
        /// Thêm transition (cạnh có hướng) giữa 2 state trong cùng 1 workflow.
        /// Validate: FromStateID và ToStateID phải thuộc cùng WorkflowID.
        /// </summary>
        /// <param name="transition">Dữ liệu transition: FromStateID, ToStateID, ActionName</param>
        /// <returns>TransitionID vừa INSERT</returns>
        Task<int> AddTransitionAsync(WorkflowTransition transition);

        /// <summary>
        /// Xóa transition. Cho phép xóa vật lý vì transition không được tham chiếu bởi bảng log.
        /// Cần kiểm tra không có PaymentRecord đang "chờ" dùng transition này trước khi xóa.
        /// </summary>
        Task<bool> DeleteTransitionAsync(int transitionId);
    }
}
