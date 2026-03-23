using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ProposeCreateAccount
    {
        public string OIDContract { get; set; } = "";
        public string CmpnID { get; set; } = "26";
        public string CrtUser { get; set; } = "";
        public string MailAcc { get; set; } = "ketoan.hoadonso@gmail.com";
    }
    public class DeXuatCapTaiKhoanResult
    {
        public string OIDJob { get; set; } = "";
        public string ReferenceInfo { get; set; } = "";
        public bool IsAlreadyExists { get; set; }
    }
}
