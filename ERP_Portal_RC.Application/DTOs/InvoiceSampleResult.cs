using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class InvoiceSampleResult
    {
        public string? FinalXmlData { get; set; }

        public string? RawXsltContent { get; set; }

        public string? ConfiguredXslt { get; set; }

        public string? FinalHtmlOutput { get; set; }

        public string? XsltFileName { get; set; }
        public string? XmlFileName { get; set; }
        public string? LogoFileName { get; set; }
        public string? BackgroundFileName { get; set; }
    }
}
