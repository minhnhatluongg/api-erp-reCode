using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities.Accounts_payable
{
    public class Workflow : AuditableEntity
    {
        public int WorkflowID { get; set; }
        public int ServiceTypeID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ServiceType? ServiceType { get; set; }
        public ICollection<WorkflowState> States { get; set; } = new List<WorkflowState>();
        public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
    }
}
