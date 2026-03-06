using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ApproveJobRequestDto
    {
        public string? Oid { get; set; }
        public string? CmpnId { get; set; }
    }
    public class CreditLimitDto
    {
        public decimal CONNO { get; set; }
        public decimal GIOIHANCN { get; set; }
    }
}
