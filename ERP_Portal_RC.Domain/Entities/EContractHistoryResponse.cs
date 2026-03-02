using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractHistoryResponse
    {
        public List<HistoryItemDTO> HistoryList { get; set; } = new();
    }
    public class HistoryItemDTO
    {
        public string? OID { get; set; }
        public string? CurrSignNum { get; set; }
        public string? AppvMess { get; set; }
        public string? FullName { get; set; }
        public DateTime? CurrSignDate { get; set; }
        public string? CancelDescript { get; set; }
    }
}
