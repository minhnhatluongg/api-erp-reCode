using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CreateAccountRequestDto : IValidatableObject
    {
        // MST hoặc CCCD — cần ÍT NHẤT MỘT (cá nhân/hộ KD có thể chỉ có CCCD). Xem Validate().
        [StringLength(20, ErrorMessage = "Mã số thuế tối đa 20 ký tự.")]
        public string MaSoThue { get; set; } = "";

        [StringLength(20, ErrorMessage = "CCCD/CMND tối đa 20 ký tự.")]
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(MaSoThue) && string.IsNullOrWhiteSpace(CMND_CCCD))
                yield return new ValidationResult(
                    "Cần nhập Mã số thuế hoặc CCCD/CMND.",
                    new[] { nameof(MaSoThue), nameof(CMND_CCCD) });
        }
    }
}
