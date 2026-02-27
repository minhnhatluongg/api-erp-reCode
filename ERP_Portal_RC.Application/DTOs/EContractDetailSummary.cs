using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractDetailSummary
    {
        public string? ItemID { get; set; }
        public string? ItemName { get; set; }
        public string? InvcSign { get; set; }
        public string? InvcSample { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public decimal Price { get; set; }
        public double Qtty { get; set; }
        public decimal SumAmnt { get; set; }
    }
}
