using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class SaveSignedXmlRequest
    {
        public string OID { get; set; }
        public string SignedXmlBase64 { get; set; }
        /// <summary>Ngày tạo hợp đồng (ODate) — dùng cho zsgn_webContracts_NOR.</summary>
        public DateTime OrderDate { get; set; }
        /// <summary>
        /// Ngày ký thực tế — hiển thị "Ký ngày" trên PDF.
        /// Nếu không truyền, tự dùng DateTime.Now khi lưu.
        /// </summary>
        public DateTime? SignDate { get; set; }
        public string? PartnerSoCCCD { get; set; }
        public string PartnerVat { get; set; }
        public string PartnerName { get; set; }
        public string CompanyTax { get; set; } = "0312303803";
        public string CompanyName { get; set; } = "CÔNG TY TNHH WIN TECH SOLUTION";
    }
}
