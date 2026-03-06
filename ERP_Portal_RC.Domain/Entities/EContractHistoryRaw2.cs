using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractHistoryRaw2
    {
        // Table 1
        public EContractMaster? EContract { get; set; }
        // Table 2
        public List<JobEntity> Jobs { get; set; } = new();
        // Table 3
        public List<JobPost> JobPosts { get; set; } = new();
        // Table 4
        public List<ListFile> ListFiles { get; set; } = new();
        // Table 5
        public List<EContractDetails> EContractDetails { get; set; } = new();
        // Table 6
        public VendorEntity? Vendor { get; set; }
        // Table 7
        public templateEcontract? TemplateEcontract { get; set; }
        // Table 8
        public ECtr_PublicInfo? ECtr_PublicInfo { get; set; }
        // Table 9
        public EmailUser? EmailUser { get; set; }
    }
}
