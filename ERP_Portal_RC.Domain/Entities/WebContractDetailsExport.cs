using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class WebContractDetailsExport
    {
        public string? OID { get; set; }
        public string? StoreID { get; set; }
        public string? ItemID { get; set; }
        public string? BchCode { get; set; }
        public string? OID_Export { get; set; }
        public string? ItemID_Export { get; set; }
        public string? BoxID { get; set; }
        public string? DlvrNtID { get; set; }
        public string? ItemAttr { get; set; }
        public decimal ItemQtty_Export { get; set; }
        public decimal StoreQtty_Export { get; set; }
        public decimal PrdcAmnt_Export { get; set; }
        public decimal CaseQtty_Export { get; set; }
        public string? DESCRIP { get; set; }
    }
}
