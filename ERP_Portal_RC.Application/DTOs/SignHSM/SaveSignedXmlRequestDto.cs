using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.SignHSM
{
    public class SaveSignedXmlRequestDto
    {
        [Required(ErrorMessage = "OID không được để trống.")]
        public string OID { get; set; } = "";
        [Required(ErrorMessage = "SignedXmlBase64 không được để trống.")]
        public string SignedXmlBase64 { get; set; } = "";
    }
}
