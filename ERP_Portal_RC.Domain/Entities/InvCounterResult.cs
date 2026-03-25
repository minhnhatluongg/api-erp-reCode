using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class InvCounterResult
    {
        public int Used { get; set; }   // Số HĐ đã dùng
        public int Total { get; set; }   // Tổng số HĐ
        public int Remaining => Total - Used; // Số HĐ còn lại
    }
}
