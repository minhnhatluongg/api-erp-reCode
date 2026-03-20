using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class CreateAccount
    {
        public string MaSoThue { get; set; } = "";
        public string CMND_CCCD { get; set; } = "";
        public string TenCongTy { get; set; } = "";
        public string DiaChi { get; set; } = "";
        public string SoTaiKhoanNH { get; set; } = "";
        public string TenNganHang { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string UyQuyen { get; set; } = ""; // Fax / Người ủy quyền
        public string Email { get; set; } = "";
        public string Website { get; set; } = "";
    }
}
