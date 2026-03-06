using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class Limit
    {
        public decimal GIOIHANCN { get; set; }
        public decimal CONNO { get; set; }
        public decimal THANHTOAN { get; set; }
        public decimal CONGNO { get; set; }
        public string? MANV { get; set; }
        public string? DV { get; set; }
    }
}
