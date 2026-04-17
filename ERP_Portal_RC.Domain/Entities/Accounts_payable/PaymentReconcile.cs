using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    public class PaymentReconcile : AuditableEntity
    {
        public long ReconcileID { get; set; }

        /// <summary>Mã phiếu hiển thị, VD "RECONCILE-2604-119095".</summary>
        public string ReconcileCode { get; set; } = string.Empty;

        public DateTime ReconcileDate { get; set; } = DateTime.Now;

        // ---- Liên kết hợp đồng gốc ----
        /// <summary>EContracts.OID (hợp đồng đang đối soát).</summary>
        public string? ContractOID { get; set; }

        /// <summary>Số HĐ gần nhất (nếu có).</summary>
        public string? InvoiceNumber { get; set; }

        // ---- Phân loại & Workflow ----
        public int ServiceTypeID { get; set; }
        public int WorkflowID { get; set; }

        /// <summary>State hiện tại (trỏ tới WorkflowState.StateID).</summary>
        public int CurrentStateID { get; set; }

        // ---- Thông tin người nộp / NVKD ----
        public string? PayerName { get; set; }
        public string? PayerPhone { get; set; }
        public string? SaleEmID { get; set; }
        public string? SaleEmName { get; set; }

        // ---- Tiền ----
        /// <summary>Tổng tiền phiếu này.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Đã thanh toán (lũy kế).</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>Còn nợ sau phiếu này.</summary>
        public decimal RemainingAmount { get; set; }

        /// <summary>CASH / BANK / EWALLET — tham chiếu <see cref="Enum.PaymentMethod"/>.</summary>
        public string? PaymentMethod { get; set; }

        /// <summary>Tài khoản / Quỹ, VD "ACB CTY WINTECH".</summary>
        public string? BankAccount { get; set; }

        /// <summary>Lệnh chuyển tiền / mã giao dịch, VD "ACBWINTECH/2026/04/0001".</summary>
        public string? TransferRef { get; set; }

        // ---- Hình ảnh (hệ hiện tại đã có chỗ lưu, chỉ giữ path/url) ----
        /// <summary>Đường dẫn file ảnh lệnh chuyển tiền, VD "tvan.jpg".</summary>
        public string? TransferImagePath { get; set; }

        /// <summary>URL preview ảnh.</summary>
        public string? TransferImageUrl { get; set; }

        // ---- Kiểm tra & ghi chú ----
        /// <summary>"Đã kiểm tra HH, công nợ" (checkbox trên UI).</summary>
        public bool IsGoodsChecked { get; set; }

        public string? Note { get; set; }

        // Navigation (service tự load khi cần)
        public ServiceType? ServiceType { get; set; }
        public Workflow? Workflow { get; set; }
        public WorkflowState? CurrentState { get; set; }
        public ICollection<PaymentReconcileDetail> Details { get; set; } = new List<PaymentReconcileDetail>();
        public ICollection<PaymentStateHistory> History { get; set; } = new List<PaymentStateHistory>();
    }
}
