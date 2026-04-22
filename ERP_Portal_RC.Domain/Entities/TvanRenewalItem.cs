using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class TvanRenewalItem
    {
        public string TaxNumber { get; set; } = string.Empty;

        // Thông tin KHÁCH HÀNG
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerContactAddress { get; set; }

        public string? PhieuDangKy { get; set; }
        public DateTime? NgayDangKy { get; set; }
        public DateTime? NgayApDung { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }

        public string? RangeKey { get; set; }   // EXPIRED / D7 / D15 / D30 / M3 / SAFE
        public string? Status { get; set; }     // Label hiển thị

        public string? ContractOID { get; set; }
        public DateTime? ContractDate { get; set; }

        public string? SaleCode { get; set; }
        public string? SaleUserCode { get; set; }
        public string? SaleFullName { get; set; }
        public string? SaleLoginName { get; set; }
        public string? SaleEmail { get; set; }
        public string? SaleDepartment { get; set; }
    }
}
