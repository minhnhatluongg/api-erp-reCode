using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ContractPublicInfoSummary
    {
        public DateTime InvcDate { get; set; }
        public bool Party_A_IsSigned { get; set; }
        public bool Party_B_IsSigned { get; set; }
        public byte[] InvcContent { get; set; }        
        public byte[] InvcContent_ByCus { get; set; }
    }
}
