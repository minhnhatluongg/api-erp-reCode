using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common
{
    public abstract class AuditableEntity
    {
        public string? Crt_User { get; set; }
        public DateTime Crt_Date { get; set; } = DateTime.Now;
        public string? ChgeUser { get; set; }
        public DateTime? ChgeDate { get; set; }
    }
}
