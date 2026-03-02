using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class SampleDataDto
    {
        public string? Serial { get; set; } 
        public string? Pattern { get; set; } 
        public string Invc_Frm { get; set; } = "1";
        public string? Invc_End { get; set; }
    }
}
