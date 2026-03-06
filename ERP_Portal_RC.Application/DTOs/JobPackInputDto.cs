using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class JobPackInputDto
    {
        public string ItemID { get; set; }      // Mã sản phẩm
        public int ItemNo { get; set; }         // Số thứ tự dòng (Cực kỳ quan trọng)
        public string? InvcSign { get; set; }   // Ký hiệu
        public string? InvcSample { get; set; } // Mẫu số
        public int InvcFrm { get; set; }        // Từ số
        public int InvcEnd { get; set; }        // Đến số
        public string Descrip { get; set; }        
        public DateTime? PublDate { get; set; } // Ngày phát hành
        public DateTime? Use_Date { get; set; } // Ngày sử dụng
    }
}
