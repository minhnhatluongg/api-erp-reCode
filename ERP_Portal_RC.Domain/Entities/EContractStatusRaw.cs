using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractStatusRaw
    {
        public EContractMasterSummary? Master { get; set; }
        public List<EContractDetailSummary>? Details { get; set; }
        public ContractPublicInfoSummary? SignedData { get; set; }
    }
}
