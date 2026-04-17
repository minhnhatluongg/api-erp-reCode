using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    public class WorkflowTransition
    {
        public int TransitionID { get; set; }
        public int WorkflowID { get; set; }
        public int FromStateID { get; set; }
        public int ToStateID { get; set; }

        /// <summary>Role được phép thực hiện chuyển (sale/accountant/bod...). Null = ai cũng được.</summary>
        public string? RequireRole { get; set; }

        /// <summary>Bắt buộc ghi chú khi chuyển (VD: khi Từ chối).</summary>
        public bool RequireNote { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public Workflow? Workflow { get; set; }
        public WorkflowState? FromState { get; set; }
        public WorkflowState? ToState { get; set; }
    }
}
