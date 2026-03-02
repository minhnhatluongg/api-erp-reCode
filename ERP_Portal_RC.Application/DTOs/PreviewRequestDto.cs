using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class PreviewRequestDto
    {
        public int TemplateId { get; set; } 
        public int XmlDataId { get; set; }  

        public CmpnInfo2? Company { get; set; }     
        public InvoiceConfigDto? Config { get; set; } 
        public ImagesDto? Images { get; set; }        
        public SampleDataDto? SampleData { get; set; } 
        public class ImagesDto
        {
            public string? LogoBase64 { get; set; }       
            public string? BackgroundBase64 { get; set; } 
        }
    }
}
