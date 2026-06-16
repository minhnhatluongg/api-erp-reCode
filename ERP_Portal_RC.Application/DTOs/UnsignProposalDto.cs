using System.ComponentModel.DataAnnotations;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Sale đề xuất GỠ KÝ một hợp đồng đã ký (kèm lý do). Chờ kế toán duyệt.
    /// RequestedBy / RequestedByName lấy từ JWT (UserCode / FullName).
    /// </summary>
    public class UnsignProposalDto
    {
        [Required(ErrorMessage = "Thiếu OID hợp đồng.")]
        public string OID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lý do gỡ ký là bắt buộc.")]
        [MinLength(10, ErrorMessage = "Lý do gỡ ký phải có ít nhất 10 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }
}
