using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ServerInfoRow
    {
        public string SideServer { get; set; } = ""; // rỗng = khách mới
        public string SideServerLocal { get; set; } = ""; // rỗng = khách mới
        public string KeyWork { get; set; } = ""; // password đã mã hoá
        public string INVnew { get; set; } = ""; // server cấp TK mới (remote)
        public string INVnewLocal { get; set; } = ""; // server cấp TK mới (LAN)
        public string TVAN { get; set; } = "";
        public string TVANLocal { get; set; } = "";
        public string ERP { get; set; } = "";
        public string ERPLocal { get; set; } = "";
        public bool IsExistingCustomer => !string.IsNullOrEmpty(SideServer);
    }
}
