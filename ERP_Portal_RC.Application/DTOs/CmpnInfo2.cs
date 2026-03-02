using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CmpnInfo2
    {
        public string SampleID { get; set; }
        public string SampleSerial { get; set; }
        public string LogoBase64 { get; set; } = "";
        public string Filelogo { get; set; } = "";
        public string BackgroundBase64 { get; set; } = "";
        public string FileBackground { get; set; } = "";
        public string SName { get; set; } = "";
        public string Tel { get; set; } = "";
        public string Fax { get; set; } = "";
        public string Address { get; set; } = "";
        public string BankInfo { get; set; } = "";
        public string Website { get; set; } = "";
        public string Email { get; set; } = "";
        public string BankNumber { get; set; } = "";
        public string BankAddress { get; set; } = "";
        public string MerchantID { get; set; } //Thật ra là MST
        public string PersonOfMerchant { get; set; } = "";
        public string SaleID { get; set; } = "";
        public string Description { get; set; } = "";
        public string CMND { get; set; } = "";
    }
}
