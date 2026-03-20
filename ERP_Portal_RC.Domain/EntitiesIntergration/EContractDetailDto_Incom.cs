using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesIntergration
{
    public class EContractDetailDto_Incom
    {
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ItemUnit { get; set; }
        public string? ItemUnitName { get; set; }
        public decimal ItemPrice { get; set; }
        public decimal ItemQtty { get; set; }
        public decimal ItemAmnt { get; set; }
        public decimal VAT_Rate { get; set; }
        public decimal VAT_Amnt { get; set; }
        public decimal Sum_Amnt { get; set; }

        public string? InvcSample { get; set; }
        public string? InvcSign { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string? Descrip { get; set; }
    }
}
