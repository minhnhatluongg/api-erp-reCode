using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CheckServerResponseDto
    {
        public string MST { get; set; } = "";
        public bool IsExistingCustomer { get; set; }

        /// <summary>Server hiện tại của khách (rỗng nếu khách mới)</summary>
        public string SideServer { get; set; } = "";

        /// <summary>Server dùng để cấp TK mới</summary>
        public string INVnew { get; set; } = "";
        public string TVAN { get; set; } = "";
        public string ERP { get; set; } = "";
        public bool Server234_OK { get; set; }

        /// <summary>SP có gọi được không (false = lỗi kết nối bosConfigure)</summary>
        public bool SPReachable { get; set; }

    }
}
