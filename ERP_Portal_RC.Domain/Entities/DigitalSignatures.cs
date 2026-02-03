using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class DigitalSignatures
    {
        public string OID { get; set; } = "";
        public string cusTax { get; set; } = "";
        public string cusName { get; set; } = "";
        public string cusAddress { get; set; } = "";
        public string cusProvince { get; set; } = "";
        public string cusDistrict { get; set; } = "";
        public DateTime cusDate_Cmpn { get; set; } 
        public string cusDirectorName { get; set; } = "";
        public string cusCMND { get; set; } = "";
        public string cus_ContactName { get; set; } = "";
        public string cus_ContactPhone { get; set; } = "";
        public string cus_ContactEmail { get; set; } = "";
        public string descripts { get; set; } = "";
        public bool isContracts { get; set; }
        public string crt_User { get; set; } = "";
        public DateTime crt_Date { get; set; }
        public string change_User { get; set; } = "";
        public DateTime change_Date { get; set; }
        public DateTime ODate { get; set; }
        public string CmpnID { get; set; } = "";
        public string is_Service { get; set; } = "";
        public string isContractType { get; set; } = "";
    }
}
