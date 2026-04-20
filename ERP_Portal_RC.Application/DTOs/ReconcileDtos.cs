using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CreateReconcileDto
    {
        public DateTime ReconcileDate { get; set; } = DateTime.Now;
        public string? ContractOID { get; set; }
        public string? InvoiceNumber { get; set; }

        public int ServiceTypeID { get; set; }
        /// <summary>Optional — nếu null sẽ auto dùng workflow default của ServiceType.</summary>
        public int? WorkflowID { get; set; }

        public string? PayerName { get; set; }
        public string? PayerPhone { get; set; }
        public string? SaleEmID { get; set; }
        public string? SaleEmName { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public string? PaymentMethod { get; set; }
        public string? BankAccount { get; set; }
        public string? TransferRef { get; set; }
        public string? TransferImagePath { get; set; }
        public string? TransferImageUrl { get; set; }

        public bool IsGoodsChecked { get; set; }
        public string? Note { get; set; }

        public List<CreateReconcileDetailDto> Details { get; set; } = new();
    }

    /// <summary>
    /// DTO input cho <c>PUT /api/reconciles/{id}</c> — chỉ các field được phép sửa ở header.
    /// Không cho sửa ReconcileCode, ReconcileDate, ServiceType, Workflow, CurrentState.
    /// </summary>
    public class UpdateReconcileHeaderDto
    {
        public string? PayerName { get; set; }
        public string? PayerPhone { get; set; }
        public string? SaleEmID { get; set; }
        public string? SaleEmName { get; set; }

        public decimal TotalAmount { get; set; }

        public string? PaymentMethod { get; set; }
        public string? BankAccount { get; set; }
        public string? TransferRef { get; set; }
        public string? TransferImagePath { get; set; }
        public string? TransferImageUrl { get; set; }

        public bool IsGoodsChecked { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>DTO input cho chuyển trạng thái.</summary>
    public class TransitionStateDto
    {
        public int ToStateID { get; set; }
        public string? Note { get; set; }
    }
}
