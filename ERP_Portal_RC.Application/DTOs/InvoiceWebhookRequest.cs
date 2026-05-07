using System.ComponentModel.DataAnnotations;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Payload từ hệ thống invoice gửi về khi đã xuất HĐĐT thành công.
    /// Chỉ cần ContractOid — các field còn lại tuỳ chọn để ghi log.
    /// </summary>
    public class InvoiceWebhookRequest
    {
        /// <summary>OID hợp đồng — bắt buộc.</summary>
        [Required(ErrorMessage = "ContractOid là bắt buộc.")]
        public string ContractOid { get; set; } = string.Empty;

        /// <summary>Số hóa đơn (tuỳ chọn, dùng để ghi log).</summary>
        public string? InvoiceNo { get; set; }

        /// <summary>Action gốc từ hệ thống invoice (tuỳ chọn, để trace).</summary>
        public string? SourceAction { get; set; }
    }
}
