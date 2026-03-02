using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class InvoiceBuildRequest
    {
        public int TemplateId { get; set; }
        public int XmlDataId { get; set; }
        public CmpnInfo2? Company { get; set; }

        public InvoiceConfigDto? Options { get; set; }
        public string XsltFileName { get; set; } = "";
        public string LogoFileName { get; set; } = "";
        public string BackgroundFileName { get; set; } = "";
    }
}
