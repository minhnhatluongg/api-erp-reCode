using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ContractPreviewRequest
    {
        // Thông tin định danh & hệ thống
        public string OrderCode { get; set; }     
        public string FactorID { get; set; }
        public string SampleID { get; set; }
        public string EntryID { get; set; } = "EC:001";
        public DateTime ODate { get; set; } = DateTime.Now;
        public DateTime SignDate { get; set; } = DateTime.Now;
        public string SaleEmID { get; set; }

        // Thông tin Bên B (Người bán)
        public string CmpnID { get; set; } = "26";
        public DateTime ReferenceDate  { get; set; } = DateTime.Now;
        public string CmpnName { get; set; }
        public string CmpnAddress { get; set; }
        public string CmpnContactAddress { get; set; }
        public string CmpnTax { get; set; }
        public string CmpnTel { get; set; }
        public string CmpnMail { get; set; }
        public string CmpnPeople_Sign { get; set; }
        public string CmpnPosition_Sign { get; set; } = "Giám Đốc";
        public string CmpnBankAddress { get; set; }
        public string CmpnBankNumber { get; set; }


        // Thông tin Bên A (Khách hàng)
        public string PartnerName { get; set; }    // Map CusName
        public string PartnerVat { get; set; }     // Map CusTax
        public string PartnerAddress { get; set; } // Map CusAddress
        public string PartnerPhone { get; set; }   // Map CusTel
        public string PartnerEmail { get; set; }   // Map CusEmail
        public string PartnerBankNo { get; set; }  // Map CusBankNumber
        public string PartnerBankAddress { get; set; } // Map CusBankAddress
        public string PartnerWebsite { get; set; } // Map CusWebsite

        // Thông tin người đại diện/liên hệ
        public string PartnerContactName { get; set; } // Map CusPeople_Sign
        public string PartnerContactJob { get; set; }  // Map CusPosition_BySign

        // Các trường nghiệp vụ bổ sung (Rất quan trọng)
        public DateTime? Date_BusLicence { get; set; } // Ngày cấp GPKD
        public string OIDContract { get; set; }     // Số hợp đồng tham chiếu
        public DateTime? RefeContractDate { get; set; } // Ngày hợp đồng tham chiếu

        public string HTMLContent { get; set; }
        public string Descrip { get; set; }
        // Flags điều khiển logic
        public bool IsCapBu { get; set; } = false;
        public bool IsGiaHan { get; set; } = false;
        public bool IsTT78 { get; set; } = true;
        public bool IsOnline { get; set; } = true;

        // Danh sách chi tiết
        public List<EContractDetailItem> Details { get; set; }
    }

    public class EContractDetailItem
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal Qtty { get; set; }
        public decimal Price { get; set; }
        // Thông tin diễn giải bổ sung (nếu cần cho bảng kê)
        public string InvcSample { get; set; }
        public string InvcSign { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string VAT_Rate { get; set; }
    }
}
