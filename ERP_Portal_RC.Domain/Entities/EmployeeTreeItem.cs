using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EmployeeTreeItem
    {
        public string? PARENTID { get; set; }
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string LEVEL_VAL { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public string SortID { get; set; } = string.Empty;
        public string ParentIDSortID { get; set; } = string.Empty;
    }
}
