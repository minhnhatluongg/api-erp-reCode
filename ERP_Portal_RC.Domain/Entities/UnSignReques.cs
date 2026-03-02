using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class UnSignRequest
    {
        [Required(ErrorMessage = "Thiếu OID.")]
        public string? OID { get; set; }

        public string? RequestedBy { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }

        [Required(ErrorMessage = "Lý do gỡ ký là bắt buộc.")]
        [MinLength(10, ErrorMessage = "Lý do gỡ ký phải có ít nhất 10 ký tự.")]
        public string? Reason { get; set; }
    }
}
