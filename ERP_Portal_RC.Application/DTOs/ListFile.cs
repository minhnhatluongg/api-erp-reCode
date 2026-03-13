using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ListFile
    {
        public string? AttachNote { get; set; }
        public string? AttachFile { get; set; }
        public string? ConvertFile { get; set; }
        public string? OID { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public string? DocSource { get; set; }
        public string? DocSourceDateField { get; set; }
        public string? DocSourceDateField_Value { get; set; }
        public string? AttachDate { get; set; }
        public string? AttachType { get; set; }
        public string? Crt_User { get; set; }
        public string? name { get; set; }
        public string? type { get; set; }
        public string? url { get; set; }
        public string? EntryName { get; set; }
        public DateTime Crt_Date { get; set; }
        public string? LinkFile { get; set; }
        public string? FolderName { get; set; }
        public string? LinkFonder { get; set; }
        public bool IsConfirm { get; set; }
        public int AttachID { get; set; }
        public bool isdisable { get; set; }
    }
}
