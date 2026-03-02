using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ERP_Portal_RC.Domain.Enum.PublicEnum;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractHistoryRaw
    {
        public List<HistoryListEntity>? History { get; set; }
        public List<JobEntity>? Jobs { get; set; }
    }
}
