using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class Digital_Moniter
    {
        public string? OID { get; set; }
        public string? LOID { get; set; }
        public DateTime ODate { get; set; }
        public string? ODatefrm { get; set; }
        public string? cusName { get; set; }
        public string? SiteName { get; set; }
        public string? cusAddress { get; set; }
        public string? CmpnID { get; set; }
        public string? EmplName { get; set; }
        public string? cusTax { get; set; }
        public string? descripts { get; set; }
        public string? crt_User { get; set; }
        public DateTime Crt_Date { get; set; }
        public int currSignNumb { get; set; }
        public int currSignNumbJob { get; set; }
        public int currSignNumbJobCancel { get; set; }
        public int currSignNumbJobExHD { get; set; }
        public int currSignNumbJobTT { get; set; }
        public string? devicesAddress { get; set; }
        public string? productType { get; set; }
        public string? Provider { get; set; }
        public string? isContractType { get; set; }
        public string? isContractTypeSale { get; set; }
        public string? DivAddressName { get; set; }
        public string? TT1 { get; set; }
        public string? TT2 { get; set; }
        public string? TT3 { get; set; }
        public string? TT4 { get; set; }
        public string? TT5 { get; set; }
        public string? TT6 { get; set; }
        public string? TT7 { get; set; }
        public DateTime ChgeDate { get; set; }
        public string? ItemName { get; set; }
        public string? CsName { get; set; }
        public string? ClnName { get; set; }
        public string? exeDescrip { get; set; }
        public bool isShowTT2 { get; set; } = false;
        public DateTime Crt_DateJOB { get; set; }
        public decimal Sum_Amnt { get; set; }
        public string? Sum_Amntfrm { get; set; }
        public decimal Sum_Amnt_v { get; set; }
        public string? Sum_Amnt_vfrm { get; set; }
        public decimal cost_Token { get; set; }
        public string? cost_Tokenfrm { get; set; }
        public decimal cost_Token_v { get; set; }
        public decimal MaintainFees { get; set; }
        public decimal MaintainFees_v { get; set; }
        public string? cost_Token_vfrm { get; set; }
        public string? userid { get; set; }
        public bool isNotes { get; set; }
        public bool open { get; set; } = false;
        public bool isCheckedShow { get; set; } = false;
        public string? noteYC { get; set; }
    }
}
