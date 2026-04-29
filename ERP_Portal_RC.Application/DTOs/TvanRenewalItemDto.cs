using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class TvanRenewalItemDto
    {
        public string TaxNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerContactAddress { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }

        public string? RangeKey { get; set; }
        public string? Status { get; set; }

        public string? ContractOID { get; set; }
        public DateTime? ContractDate { get; set; }

        public string? SaleCode { get; set; }
        public string? SaleUserCode { get; set; }
        public string? SaleFullName { get; set; }
        public string? SaleEmail { get; set; }
        public string? SaleDepartment { get; set; }
    }
}
