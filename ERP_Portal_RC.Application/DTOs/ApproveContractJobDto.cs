using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ApproveContractJobDto
    {
        public string? OID { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public string? CmpnID { get; set; }
        public string? Crt_User { get; set; }
        public string? SaleName { get; set; }
        public int HoldSignNumb { get; set; }
        public int NextSignNumb { get; set; } = 101;
        public string SignTble { get; set; } = "zsgn_EContractJobs";
        public string DataTbl { get; set; } = "EContractJobs";
        public DateTime ODate { get; set; } = DateTime.Now;
    }
}
