using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractServiceResult
    {
        public string MoneyToBePaid { get; set; } = "0";
        public string MoneyPaid { get; set; } = "0";
        public List<EContract_Monitor> Data { get; set; } = new();
        public int Total { get; set; }
        public bool Disable { get; set; }
    }
}
