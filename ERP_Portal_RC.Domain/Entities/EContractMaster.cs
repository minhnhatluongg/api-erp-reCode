using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractMaster
    {
        public string CmpnID { get; set; } = "26";
        public string OID { get; set; }
        public DateTime ODate { get; set; } = DateTime.Now;
        public DateTime ReferenceDate { get; set; } = DateTime.Now;
        public string ReferenceID { get; set; }
        public string FactorID { get; set; }
        public string EntryID { get; set; }
        public string SaleEmID { get; set; }
        public string CmpnName { get; set; }
        public string CmpnAddress { get; set; }
        public string CmpnContactAddress { get; set; }
        public string CmpnTax { get; set; }
        public string CmpnTel { get; set; }
        public string CmpnMail { get; set; }
        public string CmpnPeople_Sign { get; set; }
        public string CmpnPosition_BySign { get; set; }
        public string CmpnBankNumber { get; set; }
        public string CmpnBankAddress { get; set; }
        public string CustomerID { get; set; }
        public string CusName { get; set; }
        public string RegionID { get; set; }
        public string CusAddress { get; set; }
        public string CusContactAddress { get; set; }
        public string CusTax { get; set; }
        public string CusTel { get; set; }
        public string CusFax { get; set; }
        public string CusEmail { get; set; }
        public string CusPeople_Sign { get; set; }
        public string CusPosition_BySign { get; set; }
        public string CusBankNumber { get; set; }
        public string CusBankAddress { get; set; }
        public decimal PrdcAmnt { get; set; }
        public decimal VAT_Rate { get; set; }
        public decimal VAT_Amnt { get; set; }
        public decimal DscnAmnt { get; set; }
        public decimal Sum_Amnt { get; set; }
        public string SampleID { get; set; }
        public string HTMLContent { get; set; }
        public string Descrip { get; set; }
        public string CmpID_Sign { get; set; }
        public string CmpName_Sign { get; set; }
        public int SignNumb { get; set; }
        public DateTime SignDate { get; set; } = DateTime.Now;
        public string Crt_User { get; set; }
        public string Crt_Date { get; set; }
        public string ChgeUser { get; set; }
        public string ChgeDate { get; set; }
        public string TaxDepartment { get; set; }
        public string CusWebsite { get; set; }
        public string Date_BusLicence { get; set; }
        public string Descript_Cus { get; set; }
        public string OIDContract { get; set; }
        public string RefeContractDate { get; set; }
        public string IsExtensionNoSample { get; set; }
        public string IsPLHD { get; set; }
        public string IsVAT { get; set; }
        public string IsCapBu { get; set; }
        public string IsGiaHan { get; set; }
        public bool IsOnline { get; set; } = true;
        public bool IsTT78 { get; set; } = true;
        public string EcontracSys { get; set; }
        public string Ptk_PL { get; set; }
        public string isContractPaper { get; set; }
    }
}
