using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ContractFileMetadata
    {
        public string Oid { get; set; } = "";
        public string FileName { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string Url { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public long SizeBytes { get; set; }
        public string Extension { get; set; } = "";
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = "";
    }
}
