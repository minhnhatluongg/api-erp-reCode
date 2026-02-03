using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class zsgnJob
    {
        public string? COID { get; set; }
        public string? OID { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public int SignNumb { get; set; }
        public string? JobStatusDescrip { get; set; }
        public string? JobStatusDescripUn { get; set; }
        public DateTime SignDate { get; set; }
    }
}
