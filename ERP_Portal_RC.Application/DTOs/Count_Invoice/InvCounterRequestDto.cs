using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.Count_Invoice
{
    public class InvCounterRequestDto
    {
        [Required(ErrorMessage = "MST là bắt buộc.")]
        public string MST { get; set; } = string.Empty;
        
    }
}
