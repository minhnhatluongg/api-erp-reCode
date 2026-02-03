using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class DigitalSignaturesDetail
    {
        public int ItemNo { get; set; }
        public string? OID { get; set; }
        public string? ItemID { get; set; }
        public string? ItemName { get; set; }
        public string? ItemUnit { get; set; }
        public Decimal ItemPrice { get; set; }
        public Decimal ItemQtty { get; set; }
        public Decimal ItemAmnt { get; set; }
        public Decimal VAT_Rate { get; set; }
        public Decimal VAT_Amnt { get; set; }
        public Decimal Sum_Amnt { get; set; }
        public string? Descrip { get; set; }
        public string? InvcSign { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string? invcSample { get; set; }
        public string? itemUnitName { get; set; }
        public string? BoxID { get; set; }
        public string? BchCode { get; set; }
        public string? StoreID { get; set; }
        public Decimal ItemPerBox { get; set; }
        public string? PrmtID { get; set; }
        public string? PrmtListItem { get; set; }
        public string? Qc_XaBang { get; set; }
        public Decimal RemnRfQt { get; set; }
        public Decimal StoreQtty { get; set; }
        public Decimal SlStQtty { get; set; }
        public Decimal SlItQtty { get; set; }
        public Decimal PrdcAmnt { get; set; }
        public Decimal DscnMbRt { get; set; }
        public Decimal DscnMbAm { get; set; }
        public Decimal DscnRate { get; set; }
        public Decimal DscnAmnt { get; set; }
        public Decimal SmPdAmnt { get; set; }
        public DateTime PublDate { get; set; }
        public DateTime Use_Date { get; set; }
        public DateTime Crt_Date { get; set; }
        public string? DescriptPL { get; set; }
        public bool isKM { get; set; }
        public bool isKMComplete { get; set; }
        public int sl_KM { get; set; }
        public string? is_Service { get; set; }
        public string? devicesAddress { get; set; }
        public string? Provider { get; set; }
        public string? productType { get; set; }
        public decimal services_cost { get; set; }
        public decimal cost_Token { get; set; }
        public int sum_MonthUse { get; set; }
        public int month_Use { get; set; }
    }
}
