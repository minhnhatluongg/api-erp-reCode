using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    //Lịch sử chuyển state.
    public class PaymentStateHistory
    {
        public long HistoryID { get; set; }
        public long ReconcileID { get; set; }

        /// <summary>Null nếu là state khởi tạo (lúc create phiếu).</summary>
        public int? FromStateID { get; set; }

        public int ToStateID { get; set; }

        /// <summary>Ai thực hiện chuyển state.</summary>
        public string ActionUser { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;

        /// <summary>Ghi chú / lý do (bắt buộc khi chuyển sang state Rejected).</summary>
        public string? ActionNote { get; set; }

        // Navigation
        public PaymentReconcile? Reconcile { get; set; }
        public WorkflowState? FromState { get; set; }
        public WorkflowState? ToState { get; set; }
    }
}
