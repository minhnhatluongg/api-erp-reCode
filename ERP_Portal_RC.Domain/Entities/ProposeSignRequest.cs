using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public  class ProposeSignRequest
    {
        public string? OID { get; set; }  // Định danh hợp đồng (Bắt buộc)
        public string? Email { get; set; } // Email nhận thông báo 
        public string? AppvMess { get; set; } // Ghi chú (Tùy chọn)
    }
}
