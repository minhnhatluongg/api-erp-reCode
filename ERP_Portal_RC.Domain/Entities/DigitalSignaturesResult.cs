using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class DigitalSignaturesResult
    {
        public DigitalSignatures? digitalSignatures { get; set; }
        public List<DigitalSignaturesDetail>? digitalSignaturesDetail { get; set; }
        public DigitalSignaturesDetail? dDetail { get; set; }
        public List<Digital_Moniter>? digital_Moniter { get; set; }
        public string? moneyPaid { get; set; }
        public string? moneyToBePaid { get; set; }
    }
}
