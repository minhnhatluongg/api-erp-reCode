using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EcontractDetailViewModel
    {
        public string? OID { get; set; }
        public string? Email { get; set; }
        public List<EContractDetails>? EContractDetails { get; set; }
        public EApprove? ApprModel { get; set; }
    }
}
