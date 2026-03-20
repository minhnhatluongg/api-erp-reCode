using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CreateAccountRequestDto
    {
        [Required(ErrorMessage = "Mã số thuế không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã số thuế tối đa 20 ký tự.")]
        public string MaSoThue { get; set; } = "";
        public string CMND_CCCD { get; set; } = "";

        [Required(ErrorMessage = "Tên công ty không được để trống.")]
        public string TenCongTy { get; set; } = "";
        public string DiaChi { get; set; } = "";
        public string SoTaiKhoanNH { get; set; } = "";
        public string TenNganHang { get; set; } = "";
        public string SoDienThoai { get; set; } = "";

        ///Người ủy quyền hoặc Fax
        public string UyQuyen { get; set; } = "";
        public string Email { get; set; } = "";
        public string Website { get; set; } = "";

        /// Cho phép cập nhật nếu tài khoản đã tồn tại trên WebApp (1 = có, 0 = không)
        public string AllowUpdate { get; set; } = "0";
    }
}
