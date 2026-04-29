using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractFilterRequest
    {
        public string? FrmDate { get; set; }
        public string? ToDate { get; set; }
        public string? CusTName { get; set; }
        public string? OIDSearch { get; set; }
        public string? Status { get; set; }
        public string? EmplChild { get; set; }
        public string? StrEmplChild { get; set; }
        public string? IsUser { get; set; }
        public int PageSize { get; set; } = 10;
        public int Page { get; set; } = 1;
    }
}
