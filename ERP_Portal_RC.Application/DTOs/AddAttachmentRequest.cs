using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class AddAttachmentRequest
    {
        public string? OID { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public string? Crt_User { get; set; }
        [Required] public List<string> Files { get; set; } = new List<string>();
    }
}
