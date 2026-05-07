using System.ComponentModel.DataAnnotations;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Payload từ hệ thống invoice gửi về khi đã xuất HĐĐT thành công.
    /// </summary>
    public class InvoiceWebhookRequest
    {
        /// <summary>OID hợp đồng (Variant19 trong zsgn_EContractJobs).</summary>
        [Required(ErrorMessage = "ContractOid là bắt buộc.")]
        public string ContractOid { get; set; } = string.Empty;

        /// <summary>Số hóa đơn đã xuất.</summary>
        public string? InvoiceNo { get; set; }

        /// <summary>Ký hiệu hóa đơn.</summary>
        public string? InvoiceSign { get; set; }

        /// <summary>Ngày xuất hóa đơn (ISO 8601).</summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>Mã CQT (nếu có).</summary>
        public string? GovCode { get; set; }

        /// <summary>Ghi chú thêm từ hệ thống invoice.</summary>
        public string? Note { get; set; }

        /// <summary>Action gốc từ hệ thống invoice (để trace).</summary>
        public string? SourceAction { get; set; }
    }
}
