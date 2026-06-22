using System.ComponentModel.DataAnnotations;

namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Tạo lại / tạo TK hệ thống ngoài (LOT ERP) cho 1 nhân viên ĐÃ TỒN TẠI.
    /// Dùng khi đăng ký sale bị lỗi đồng bộ TK ngoài (vd lỗi SSL) → retry mà không tạo lại nhân viên.
    /// </summary>
    public class CreateHrAccountRequest
    {
        [Required] public string EmplId { get; set; } = string.Empty;   // mã NV (win_id)
        [Required] public string FullName { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        [Required, MinLength(5)] public string LoginName { get; set; } = string.Empty;
        [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    }
}
