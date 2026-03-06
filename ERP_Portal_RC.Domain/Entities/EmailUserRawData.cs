using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EmailUserRawData
    {
        public List<JobEntity> Jobs { get; set; } = new();
        public EmailUserDept? EmailUserDept { get; set; }
    }
}
