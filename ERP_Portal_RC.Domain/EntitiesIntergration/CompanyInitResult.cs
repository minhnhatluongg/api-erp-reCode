using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesIntergration
{
    public class CompanyInitResult
    {
        public string CmpnID { get; set; }
        public string CmpnKey { get; set; }
        public string GrpCode { get; set; }
        public string GrpName { get; set; }
        public string UserCode { get; set; }
        public string Username { get; set; }
        public string TaxCode { get; set; }

        public bool IsDuplicateKey => CmpnKey == "-1";
        public bool IsDuplicateTax => TaxCode == "-1";
    }
}
