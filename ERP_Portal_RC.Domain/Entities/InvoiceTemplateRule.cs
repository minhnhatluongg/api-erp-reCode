using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class InvoiceTemplateRule
    {
        public int RuleID { get; set; }
        public string? RuleCode { get; set; }
        public string? RuleName { get; set; }
        public string? RuleContent { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
