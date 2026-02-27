using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ContractStatusResponse
    {
        public string? Oid { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerTaxCode { get; set; }
        public bool IsSigned { get; set; }
        public string Base64Content { get; set; }
        public EContractMasterSummary? Master { get; set; }
        public List<EContractDetailSummary>? Details { get; set; }
        public PublicInfoSummary? SignedInfo { get; set; }
    }
}
