using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class InvoiceTemplate
    {
        public int TemplateID { get; set; }
        public string? TemplateCode { get; set; }
        public string? FileName { get; set; }
        public string? TemplateName { get; set; }
        public string? InvoiceType { get; set; }
        public string? InvoiceContent { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
