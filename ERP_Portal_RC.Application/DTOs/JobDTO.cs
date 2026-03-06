using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class JobDTO
    {
        public string? OID { get; set; }
        public string? ReferenceID { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public string? InvcSign { get; set; }
        public int? InvcFrm { get; set; }
        public int? InvcEnd { get; set; }
        public string? InvcSample { get; set; }

        public string? Descrip { get; set; }
        public string? DescriptChange { get; set; }

        public string? Crt_User { get; set; }
        public string? CmpnID { get; set; }

        public List<string> FileNames { get; set; } = new List<string>();
        public string? FileUrl { get; set; }
        public string? AttachType { get; set; }
        public string? AttachNote { get; set; }
        public string? FactorIDAtt { get; set; }

        public bool IsAuto_InvcNumb { get; set; }
        public string? ReferenceInfo { get; set; }
    }
}
