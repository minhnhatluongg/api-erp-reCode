using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.SignHSM
{
    public class SaveSignedXmlResponseDto
    {
        public string OID { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
