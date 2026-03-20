using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class OwnerContract
    {
        public string CmpnID { get; set; }
        public string CmpnName { get; set; }
        public string CmpnAddress { get; set; }
        public string CmpnContactAddress { get; set; }
        public string CmpnTax { get; set; }
        public string CmpnTel { get; set; }
        public string CmpnMail { get; set; }
        public string CmpnPeople_Sign { get; set; }
        public string CmpnPosition_BySign { get; set; }
        public string CmpnBankNumber { get; set; }
        public string CmpnBankAddress { get; set; }
    }
}
