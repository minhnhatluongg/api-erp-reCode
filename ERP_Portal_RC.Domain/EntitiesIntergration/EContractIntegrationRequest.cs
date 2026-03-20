using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesIntergration
{
    public class EContractIntegrationRequest
    {
        // --- THÔNG TIN KHÁCH HÀNG (BÊN A) ---
        public string CusTax { get; set; } = string.Empty;
        public string CusName { get; set; } = string.Empty;
        public string? CusAddress { get; set; }
        public string? CusEmail { get; set; }
        public string? CusTel { get; set; }
        public string? CusPeople_Sign { get; set; }
        public string? CusPosition_BySign { get; set; }
        public string? CusBankNumber { get; set; }
        public string? CusBankAddress { get; set; }

        // --- THÔNG TIN CÔNG TY MÌNH (BÊN B) ---
        public string MyCmpnID { get; set; } = "26";
        public string MyCmpnName { get; set; } = string.Empty;
        public string MyCmpnTax { get; set; } = string.Empty;
        public string? MyCmpnAddress { get; set; }
        public string? MyCmpnContactAddress { get; set; }
        public string? MyCmpnMail { get; set; }
        public string? MyCmpnTel { get; set; }
        public string? MyCmpnPeople_Sign { get; set; }
        public string? MyCmpnPosition_Sign { get; set; }
        public string? MyCmpnBankNumber { get; set; }
        public string? MyCmpnBankAddress { get; set; }

        // --- THÔNG TIN ĐƠN HÀNG (MASTER) ---
        public string? OID { get; set; }
        public string FactorID { get; set; } = "EContract";
        public string EntryID { get; set; } = "EC:001";
        public string SaleEmID { get; set; } = string.Empty;

        public decimal PrdcAmnt { get; set; }
        public decimal VAT_Rate { get; set; }
        public decimal VAT_Amnt { get; set; }
        public decimal Sum_Amnt { get; set; }

        public string SampleID { get; set; } = string.Empty;
        public string? Descrip { get; set; }

        public DateTime? ODate { get; set; }
        public DateTime? SignDate { get; set; }
        public string? HtmlContent { get; set; } = "MINI-APP-INCOM";
        public string? OidContract {  get; set; }

        public DateTime? RefeContractDate { get; set; }

        public bool IsCapBu {  get; set; }
        public bool IsGiaHan {  get; set; }
        public bool IsTT78 { get; set; } = true;
        public bool IsOnline {  get; set; } = true;

        // --- CHI TIẾT GÓI ---
        public List<EContractDetailDto_Incom> Details { get; set; } = new List<EContractDetailDto_Incom>();
    }
}
