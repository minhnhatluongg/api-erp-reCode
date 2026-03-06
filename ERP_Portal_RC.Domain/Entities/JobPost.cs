using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class JobPost
    {
        public string? FactorID { get; set; }
        public string? OID { get; set; }
        public string? ODate { get; set; }
        public string? CmpnID { get; set; }
        public string? DataTbl { get; set; }
        public string? SignNumb { get; set; }
        public string? SignDate { get; set; }
        public DateTime? Crt_Date { get; set; }
        public string? Crt_User { get; set; }
        public string? AppvRouteGroup { get; set; }
        public string? AppvRouteGrpTp { get; set; }
        public string? AppvMess { get; set; }
        public string? EntryID { get; set; }
        public string? FNAME { get; set; }
        public string? EntryName { get; set; }
        public string? EmplName { get; set; }
        public DateTime ChgeDate { get; set; }
        public string? InvcSign { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string? invcSample { get; set; }
        public string? DescriptChange { get; set; }
        public string? Descrip { get; set; }
    }
}
