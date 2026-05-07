using System.ComponentModel.DataAnnotations;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Payload từ ERP khi kế toán click "Xem hóa đơn nháp".
    /// </summary>
    public class RequestInvoiceWebhookRequest
    {
        /// <summary>OID hợp đồng cần yêu cầu xuất hóa đơn.</summary>
        [Required(ErrorMessage = "ContractOid là bắt buộc.")]
        public string ContractOid { get; set; } = string.Empty;

        /// <summary>UserCode của kế toán thực hiện thao tác (để ghi log).</summary>
        public string? RequestedBy { get; set; }

        /// <summary>Ghi chú từ ERP (tuỳ chọn).</summary>
        public string? Note { get; set; }
    }
}
