using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ListEcontractViewModel
    {
        public List<EContract_Monitor>? lstMonitor { get; set; }
        public List<SubEmpl>? subEmpl { get; set; }
        public bool IsDisiable { get; set; }
    }
}
