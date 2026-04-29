using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    public class WorkflowState
    {
        public int StateID { get; set; }
        public int WorkflowID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>Thứ tự hiển thị trên UI.</summary>
        public int SeqNo { get; set; }

        /// <summary>State khởi tạo (Create phiếu sẽ set sang state này).</summary>
        public bool IsInitial { get; set; }

        /// <summary>State kết thúc (Done / Reject).</summary>
        public bool IsFinal { get; set; }

        /// <summary>Đánh dấu state "Từ chối" — UI render màu đỏ, bắt buộc ghi chú.</summary>
        public bool IsRejected { get; set; }

        /// <summary>Màu tô UI, ví dụ "#9C27B0".</summary>
        public string? ColorHex { get; set; }

        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Workflow? Workflow { get; set; }
    }
}
