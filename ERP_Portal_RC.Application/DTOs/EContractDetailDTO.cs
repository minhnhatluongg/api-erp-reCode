using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractDetailDTO
    {
        public int ItemNo { get; set; }
        public string? OID { get; set; }
        public string? ItemID { get; set; }
        public string? ItemName { get; set; }
        public string? ItemUnit { get; set; }
        public decimal ItemPrice { get; set; }
        public decimal ItemQtty { get; set; }
        public decimal VAT_Rate { get; set; }
        public decimal Sum_Amnt { get; set; }

        public string? InvcSample { get; set; } // Mẫu số
        public string? InvcSign { get; set; }   // Ký hiệu
        public int InvcFrm { get; set; }       // Từ số
        public int InvcEnd { get; set; }       // Đến số

        public string? UsIN { get; set; }       // Dùng để filter theo Job Factor
        public string? ParentNm_0 { get; set; }
    }
}
