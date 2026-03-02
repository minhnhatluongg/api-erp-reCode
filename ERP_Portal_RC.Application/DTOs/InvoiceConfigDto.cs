using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class InvoiceConfigDto
    {
        //Special Configurations
        public bool cksIsSignServerProcess { get; set; } = true;
        // Các cờ cấu hình từ UI (checkbox)
        public bool TokhaiApproved { get; set; }          // cksToKhai
        public bool IsVCNB { get; set; }                 // Hóa đơn vận chuyển nội bộ
        public bool IsTemVe { get; set; }                // cksTemVe
        public bool IsHDBH { get; set; }                 // cksBH
        public bool IsHDVAT { get; set; }                // cksVAT
        public bool SignAtClient { get; set; }           // cksSignLocal
        public bool IsMultiVat { get; set; }             // ckDTS (deprecated - dùng để enable dropdown)
        public bool GenerateNumberOnSign { get; set; }   // cskNumber
        public bool SendMailAtServer { get; set; }       // cksEmailSV
        public bool PriceBeforeVat { get; set; }         // cksOtherVAT
        public bool HasFee { get; set; }                 // cksIsSignServerProcess
        public bool IsTaxDocument { get; set; }          // Chứng từ thuế khấu trừ (gộp TNCN + CTT)
        public bool isHangGuiDaiLy { get; set; }                 // Hàng gửi đại lý
        public bool UseSampleData { get; set; }          // cksData

        // --- THÊM CẤU HÌNH TÙY CHỈNH TỪ FORM ĐIỀU CHỈNH ---
        public AdjustConfigDto? AdjustConfig { get; set; }
        public CmpnInfo2? Company { get; set; }
        public SampleDataDto? SampleData { get; set; }

        // Thông tin hình ảnh & CSS (Base64 string)
        public string? LogoBase64 { get; set; }
        public string? BackgroundBase64 { get; set; }
        public string? CustomCss { get; set; }
        public string? CustomXsltContent { get; set; }
    }
}
