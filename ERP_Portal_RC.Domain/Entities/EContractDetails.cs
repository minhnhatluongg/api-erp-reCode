using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractDetails
    {
        public string ParentID { get; set; }
        public string OID { get; set; }
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemUnit { get; set; }
        public decimal ItemPrice { get; set; }
        public decimal ItemPrice_v { get; set; }
        public decimal ItemQtty { get; set; }
        public decimal ItemAmnt { get; set; }
        public decimal VAT_Rate { get; set; }
        public decimal VAT_Amnt { get; set; }
        public decimal Sum_Amnt { get; set; }
        public decimal Sum_Amnt_v { get; set; }
        public string Descrip { get; set; }
        public string InvcSample { get; set; }
        public string InvcSign { get; set; }
        public decimal StoreQtty { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string itemUnitName { get; set; }
        public string UsIN { get; set; }
        public string ParentNm_0 { get; set; }
        public int ItemNo { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime PublDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Use_Date { get; set; }
        public string Use_DateFormat { get; set; }
        public string PublDateFormat { get; set; }
        public int ItemPerBox { get; set; }
        public string InvTypeName { get; set; }
        public int InvcCountEnd { get; set; }
        public string DescriptPL { get; set; }
        public bool isKM { get; set; } = false;
        public bool isKMComplete { get; set; } = false;
        public int sl_KM { get; set; }
        public string is_Service { get; set; }
        public string devicesAddress { get; set; }
        public string Provider { get; set; }
        public string productType { get; set; }
        public decimal services_cost { get; set; }
        public decimal cost_Token { get; set; }
        public decimal services_cost_v { get; set; }
        public decimal cost_Token_v { get; set; }
        public string MaintainFees_v { get; set; }
        public string sum_MonthUse { get; set; }
        public string month_Use { get; set; }
        public string MaintainFees { get; set; }
        public string DivAddressName { get; set; }
        public string ROOM { get; set; }
        public string ClnName { get; set; }
        public string CsName { get; set; }
        public decimal price_Other { get; set; }
        public decimal price_Other_v { get; set; }
    }
}
