using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ContractSearchRequest
    {
        public string? FrmDate { get; set; }
        public string? ToDate { get; set; }
        public string? CusTName { get; set; }
        public string? CusTTax { get; set; }
        public string? NCC { get; set; }
        public string? Kinhdoanh { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
