using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    public class ServiceType : AuditableEntity
    {
        public int ServiceTypeID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    }
}
