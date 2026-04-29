using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ContractFilesResponse
    {
        public string Oid { get; set; } = "";
        public int TotalFiles { get; set; }
        public List<ContractFileMetadata> Files { get; set; } = new();
    }
}
