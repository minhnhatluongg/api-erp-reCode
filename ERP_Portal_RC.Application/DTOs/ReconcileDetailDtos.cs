using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CreateReconcileDetailDto
    {
        public string? ContractOID { get; set; }
        public int? ContractItemNo { get; set; }
        public string? ItemID { get; set; }
        public string? ItemName { get; set; }

        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerTax { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoicingUnit { get; set; }

        public decimal OrderAmount { get; set; }
        public decimal PaidBeforeAmount { get; set; }
        public decimal PayingAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal CommissionAmount { get; set; }

        public int? LineStateID { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO input cho update 1 dòng chi tiết.
    /// </summary>
    public class UpdateReconcileDetailDto
    {
        public long DetailID { get; set; }
        public string? ItemName { get; set; }

        public decimal OrderAmount { get; set; }
        public decimal PaidBeforeAmount { get; set; }
        public decimal PayingAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal CommissionAmount { get; set; }

        public int? LineStateID { get; set; }
        public string? Note { get; set; }
    }
}
