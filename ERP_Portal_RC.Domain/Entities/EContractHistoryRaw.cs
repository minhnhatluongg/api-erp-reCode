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
        /// <summary>Lịch sử ký hợp đồng từ zsgn_webContracts.</summary>
        public List<HistoryListEntity>?      History     { get; set; }
        /// <summary>Lịch sử duyệt Job từ zsgn_EContractJobs.</summary>
        public List<JobHistoryEntity>?       JobHistory  { get; set; }
        /// <summary>Tracking chỉnh sửa: gỡ ký / edit / gửi lại.</summary>
        public List<ContractTrackingEntity>? Tracking    { get; set; }
    }
}
