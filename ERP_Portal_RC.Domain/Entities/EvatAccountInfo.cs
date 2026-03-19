using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EvatAccountInfo
    {
        public bool HasAccount { get; set; }
        public string? Mst { get; set; }
        public string? Cccd { get; set; }
        public string? CmpnName { get; set; }
        public string? MerchantId { get; set; }
        public string? ServerIp { get; set; }
        public string? ServerName { get; set; }
        public string Status => HasAccount ? "EXISTING" : "NEW";
    }
}
