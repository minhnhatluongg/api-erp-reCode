using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ManagerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string SortID { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public List<ManagerDto> Children { get; set; } = new();
    }
}
