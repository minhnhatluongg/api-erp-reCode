using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Enum
{
    public enum ReconcileStateCode
    {
        /// <summary>Đang bổ sung thông tin</summary>
        DRAFT = 1,

        /// <summary>Chờ duyệt</summary>
        PENDING = 2,

        /// <summary>Từ chối</summary>
        REJECTED = 3,

        /// <summary>Đã xác nhận</summary>
        APPROVED = 4,

        /// <summary>Đã hoàn thành</summary>
        DONE = 5
    }
}
