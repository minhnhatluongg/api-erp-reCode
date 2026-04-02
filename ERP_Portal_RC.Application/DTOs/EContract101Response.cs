using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContract101Response
    {
        public string? ContractId { get; set; } // Map từ OID
        public string? CustomerName { get; set; }
        public string? TaxCode { get; set; }
        public string? Status { get; set; } = "Đã gửi yêu cầu cấp tài khoản (101)";
        public DateTime? CreatedDateFormatted { get; set; }
        public string? Creator { get; set; }
    }
}
