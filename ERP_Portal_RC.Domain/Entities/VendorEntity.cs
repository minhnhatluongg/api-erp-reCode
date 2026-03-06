using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class VendorEntity
    {
        public string? ParentID { get; set; }
        public string? ParentSite { get; set; }
        public string? cmpnID { get; set; }
        public string? sName { get; set; }
        public string? vName { get; set; }
        public string? eName { get; set; }
        public bool IsSite { get; set; }
        public string? SiteName { get; set; }
        public string? Director { get; set; }
        public string? Address { get; set; }
        public string? TaxCode { get; set; }
        public string? BankInfo { get; set; }
        public string? Tel { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? AppvSite { get; set; }
        public string? AppvHost { get; set; }
        public string? Cfg_Host { get; set; }
        public string? LogoPath { get; set; }
        public string? BgrdPath { get; set; }
        public string? SignPath { get; set; }
        public string? CmpnType { get; set; }
        public string? CmpnBrief { get; set; }
        public string? CmpnKey { get; set; }
        public string? DsplItUn { get; set; }
        public string? BaseItUn { get; set; }
        public bool IsSystem { get; set; }
        public bool IsAcctive { get; set; }
        public bool UseSystemDate { get; set; }
        public bool ShowToolBar { get; set; }
        public bool ShowMenuBar { get; set; }
        public bool ShowCustPanel { get; set; }
        public bool MutiModule { get; set; }
        public bool HasLoyalty { get; set; }
        public bool HasPackCode { get; set; }
        public int DataRemaindByMonth { get; set; }
        public string? FinancialYear_Frm { get; set; }
        public string? FinancialYear_End { get; set; }
        public string? PeriodsType { get; set; }
        public string? OrgnType { get; set; }
        public int SignNumb { get; set; }
        public DateTime SignDate { get; set; }
        public string? Crt_User { get; set; }
        public DateTime Crt_Date { get; set; }
        public string? ChgeUser { get; set; }
        public string? ChgeDate { get; set; }
        public bool IsGroup { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumericFormat { get; set; }
        public int DigitOfDecimal { get; set; }
        public string? CurrencyFormat { get; set; }
        public string? LinkAPI { get; set; }
        public string? APIService { get; set; }
        public string? APISecrets { get; set; }
        public bool AutoOfBarcodePrefix { get; set; }
        public int LenghOfBarcodePrefix { get; set; }
        public bool InputExpiry { get; set; }
        public string? AuthenUser_APIAcc { get; set; }
        public string? authenPassword_APIAcc { get; set; }
        public string? baseUrl_APIAcc { get; set; }
        public string? authenUser_APIInv { get; set; }
        public string? authenPassword_APIInv { get; set; }
        public string? baseUrl_APIInv { get; set; }
        public string? SaleID { get; set; }
        public decimal QttyInv { get; set; }
        public bool IsCheckQttyInv { get; set; }
        public string? CmpID_Sign { get; set; }
        public string? CmpName_Sign { get; set; }
        public string? AppID { get; set; }
        public string? PeriodBilling { get; set; }
        public string? BusinessField { get; set; }
        public string? SampleID { get; set; }
        public string? SampleName { get; set; }
        public string? PositionName { get; set; }

        public string id
        {
            get
            {
                return this.cmpnID;
            }
        }
        public string text
        {
            get
            {
                return this.sName;
            }
        }
    }
}
