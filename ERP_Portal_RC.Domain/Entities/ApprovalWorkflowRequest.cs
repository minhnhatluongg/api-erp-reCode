namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Input chung cho 3 API workflow phê duyệt hợp đồng điện tử.
    /// CmpnID, CusTax, CmpnTax, SampleID được tự động enrich từ DB nếu để trống.
    /// </summary>
    public class ApprovalWorkflowRequest
    {
        /// <summary>Mã định danh hợp đồng / job. BẮT BUỘC.</summary>
        public string OID { get; set; } = string.Empty;

        /// <summary>Ghi chú trình ký / phát hành. Tùy chọn. Default: "Trình ký từ hệ thống Portal".</summary>
        public string? AppvMess { get; set; }

        /// <summary>
        /// Mã mẫu hóa đơn. Tùy chọn - chỉ dùng cho propose-template và issue-invoice (JOB_*).
        /// Nếu để trống sẽ tự lấy từ BosOnline.dbo.EContracts.
        /// </summary>
        public string? SampleID { get; set; }
    }
}
