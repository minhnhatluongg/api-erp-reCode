using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class CheckContract
    {
        public string? ContractOID { get; set; }
        public string? InvcSample { get; set; }
        public string? InvcSign { get; set; }
        public string? InvcFrom { get; set; }
        public string? InvcEnd { get; set; }
        public string? Crt_User { get; set; }
        public string? CusTax { get; set; }
    }
}
