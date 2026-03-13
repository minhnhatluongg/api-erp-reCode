using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class HistoryList
    {
        public string? OID { get; set; }
        public DateTime currSignDate { get; set; }
        public string? currSignNum { get; set; }
        public string? FullName { get; set; }
        public string? appvMess { get; set; }
        public string? cancelDescript { get; set; }
    }
}
