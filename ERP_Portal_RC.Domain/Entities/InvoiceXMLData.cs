using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class InvoiceXMLData
    {
        public int DataID { get; set; }
        public string? DataCode { get; set; }
        public string? Description { get; set; }
        public string? XmlContent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
    }
}
