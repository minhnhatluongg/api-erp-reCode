using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    public class PaymentReconcileDetail
    {
        public long DetailID { get; set; }
        public long ReconcileID { get; set; }

        // ---- Liên kết EContractDetails ----
        /// <summary>EContracts.OID.</summary>
        public string? ContractOID { get; set; }

        /// <summary>EContractDetails.ItemNo.</summary>
        public int? ContractItemNo { get; set; }

        public string? ItemID { get; set; }

        /// <summary>VD: "Phí duy trì hệ thống và Tvan truyền nhận hoá đơn điện tử...".</summary>
        public string? ItemName { get; set; }

        // ---- Khách hàng / Hoá đơn ----
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerTax { get; set; }

        /// <summary>VD: "HDAP2601/005216".</summary>
        public string? InvoiceNumber { get; set; }

        /// <summary>Đơn vị xuất hoá đơn, VD "WINTECH".</summary>
        public string? InvoicingUnit { get; set; }

        // ---- Số tiền (core của công nợ) ----
        /// <summary>"GT đơn hàng" — tổng giá trị đơn.</summary>
        public decimal OrderAmount { get; set; }

        /// <summary>Đã thanh toán TRƯỚC phiếu này.</summary>
        public decimal PaidBeforeAmount { get; set; }

        /// <summary>"Thanh toán lần này".</summary>
        public decimal PayingAmount { get; set; }

        /// <summary>"Còn nợ" SAU phiếu này.</summary>
        public decimal RemainingAmount { get; set; }

        /// <summary>"Chi HHNV" — hoa hồng nhân viên.</summary>
        public decimal CommissionAmount { get; set; }

        // ---- State riêng cho từng dòng (optional) ----
        /// <summary>Null = dùng theo state của header.</summary>
        public int? LineStateID { get; set; }

        public string? Note { get; set; }

        public DateTime Crt_Date { get; set; } = DateTime.Now;

        // Navigation
        public PaymentReconcile? Reconcile { get; set; }
        public WorkflowState? LineState { get; set; }
    }
}
