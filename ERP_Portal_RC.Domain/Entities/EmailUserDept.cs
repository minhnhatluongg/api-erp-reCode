using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EmailUserDept
    {
        public string? CustomerID { get; set; }
        public string? Email { get; set; }
        public string? OperDept { get; set; }
        public string? User_position { get; set; }
        public string? Email_CC { get; set; }
    }
}
