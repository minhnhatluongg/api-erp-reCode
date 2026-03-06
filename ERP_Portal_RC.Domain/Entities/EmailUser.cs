using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EmailUser
    {
        public string? EmplName { get; set; }
        public string? Email { get; set; }
        public string? EmplID { get; set; }
        public DateTime Crt_Date { get; set; }
    }
}
