using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractDTO
    {
        public string? OID { get; set; }
        public string? CmpnName { get; set; }
        public string? CusName { get; set; }
        public string? CusTax { get; set; }
        public string? CusEmail { get; set; }
        public string? CusAddress { get; set; }
        public string? PositionName { get; set; }
        public string? BankInfo { get; set; }
        public int CurrSignNumb { get; set; }
        public bool IsTT78 { get; set; }
        public bool IsshowYCCS { get; set; }
        public bool IsCheckXHD { get; set; }
        public bool IsShowCheckXHD { get; set; }
    }
}
