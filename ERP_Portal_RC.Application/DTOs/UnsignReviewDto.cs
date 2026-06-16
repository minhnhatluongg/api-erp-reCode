namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Body cho kế toán duyệt / từ chối đề xuất gỡ ký.
    /// - Approve: ReviewNote là ghi chú (tuỳ chọn).
    /// - Reject:  ReviewNote là lý do từ chối (bắt buộc).
    /// </summary>
    public class UnsignReviewDto
    {
        public string? ReviewNote { get; set; }

        // LOT-ERP truyền win_id + tên người duyệt xuống để ghi nhận đúng người
        // (win_id chính là giá trị lưu ở ECtr_UnsignLogs.RequestedBy /
        // ECtr_ContractTrackingLog.ActionBy). Nếu null thì fallback UserCode token.
        public string? ReviewerCode { get; set; }
        public string? ReviewerName { get; set; }
    }
}
