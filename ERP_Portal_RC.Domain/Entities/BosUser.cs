using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class BosUser
    {
        public string? UserCode { get; set; }
        public string? LoginName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PasswordEx { get; set; }
        public string? DName { get; set; }
        public string? FullName { get; set; }
        public bool IsAcctive { get; set; }
        public bool RsDonChangePass { get; set; }
        public int PermissionLevels { get; set; }
        public DateTime? DeActiveDate { get; set; }
        public bool IsDelete { get; set; }
    }
}
