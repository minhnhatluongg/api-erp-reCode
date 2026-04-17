using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common.Filters
{
    public class ReconcileFilter
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;

        /// <summary>Lọc theo state, VD "PENDING", "APPROVED". Null = tất cả.</summary>
        public string? StateCode { get; set; }

        /// <summary>Lọc theo loại dịch vụ. Null = tất cả.</summary>
        public int? ServiceTypeID { get; set; }

        /// <summary>Lọc theo nhân viên kinh doanh.</summary>
        public string? SaleEmID { get; set; }

        /// <summary>Lọc theo hợp đồng (EContracts.OID).</summary>
        public string? ContractOID { get; set; }

        /// <summary>Ngày phiếu từ.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Ngày phiếu đến.</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>Keyword: search theo ReconcileCode, PayerName, CustomerName, InvoiceNumber.</summary>
        public string? Keyword { get; set; }

        /// <summary>Sắp xếp: "ReconcileDate desc", "TotalAmount asc"...</summary>
        public string? OrderBy { get; set; } = "ReconcileDate DESC";
    }
}
