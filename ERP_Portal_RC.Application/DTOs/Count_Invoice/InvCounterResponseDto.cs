using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.Count_Invoice
{
    public class InvCounterResponseDto
    {
        public string MST { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public int Used { get; set; }  // Đã dùng
        public int Total { get; set; }  // Tổng
        public int Remaining { get; set; }  // Còn lại
    }
}
