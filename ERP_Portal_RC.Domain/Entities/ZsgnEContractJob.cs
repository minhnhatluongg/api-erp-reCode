using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ZsgnEContractJob
    {
        public string? FactorID { get; set; }
        public string? OID { get; set; }
        public DateTime ODate { get; set; } = DateTime.Now;
        public string? CmpnID { get; set; }
        public string DataTbl { get; set; } = "EContractJobs";
        public int SignNumb { get; set; }
        public string? Crt_User { get; set; }
        public DateTime Crt_Date { get; set; } = DateTime.Now;
        public string? EntryID { get; set; }
        public string? ReferenceID { get; set; }

        public string Variant30 { get; set; } = "1";
        public string AppvMess { get; set; } = "Duyệt yêu cầu";

    }
}
