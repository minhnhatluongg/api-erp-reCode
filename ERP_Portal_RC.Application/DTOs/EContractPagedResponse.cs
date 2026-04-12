using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractPagedResponse
    {
        public string? MoneyToBePaid { get; set; }
        public string? MoneyPaid { get; set; }
        public object? Data { get; set; }
        public int Total { get; set; }
        public bool Disable { get; set; }
    }


}
