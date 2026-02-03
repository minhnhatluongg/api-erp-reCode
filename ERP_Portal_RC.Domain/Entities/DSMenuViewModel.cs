using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class DSMenuViewModel
    {
        public ApplicationUser? ApplicationUser { get; set; }
        public List<bosMenuRight?> bosMenuRight { get; set; }
        public int mode { get; set; }
    }
}
