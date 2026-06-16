using System;

namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// 1 dòng đề xuất gỡ ký trong hàng đợi ECtr_UnsignRequests (kế toán xem & duyệt).
    /// Map trực tiếp từ resultset của wsp_ECtr_UnsignRequest_List.
    /// </summary>
    public class UnsignRequestItem
    {
        public long RequestID { get; set; }
        public Guid CorrelationId { get; set; }
        public string? OID { get; set; }
        public string? CmpnID { get; set; }
        public string? CusName { get; set; }
        public string? CusTax { get; set; }
        public int? PrevSignNumb { get; set; }
        public string? Reason { get; set; }

        /// <summary>PENDING | APPROVED | REJECTED | CANCELLED | FAILED</summary>
        public string? Status { get; set; }

        public string? RequestedBy { get; set; }
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }

        public string? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }

        public string? UnsignStatus { get; set; }
        public string? UnsignMessage { get; set; }
    }
}
