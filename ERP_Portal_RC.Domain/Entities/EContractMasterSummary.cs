using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractMasterSummary
    {
        public string Oid { get; set; }
        public string CustomerName { get; set; }
        public string CustomerTaxCode { get; set; }
        public string CusAddress { get; set; }
        public string CusWebsite { get; set; }
        public string CusTel { get; set; }
        public string CusEmail { get; set; }
        public string CusPeople_Sign { get; set; }
        public string CusPosition_BySign { get; set; }
        public string CusBankAddress { get; set; }
        public string CusBankNumber { get; set; }
        public string Descrip { get; set; }
        public DateTime Crt_Date { get; set; }
        public string Crt_User { get; set; }
        public DateTime ODate { get; set; }
    }
}
