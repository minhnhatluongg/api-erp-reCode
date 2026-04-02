using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractWaiting
    {
        public string? OID { get; set; }
        public DateTime? ODate { get; set; }
        public string? CustomerID { get; set; }
        public string? CusName { get; set; }
        public string? CusAddress { get; set; }
        public string? EmplName { get; set; }
        public string? Descrip { get; set; }
        public string? SaleEmID { get; set; }
        public DateTime? Crt_Date { get; set; }
        public string? Crt_User { get; set; }
        public string? CusTax { get; set; }
        public string? CmpnName { get; set; }
        public string? SiteName { get; set; }
        public int CurrSignNumb { get; set; }
    }
}
