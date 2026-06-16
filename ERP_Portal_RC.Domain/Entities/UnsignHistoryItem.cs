using System;

namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// 1 dòng lịch sử gỡ ký của 1 người (kế toán) — đọc từ ECtr_ContractTrackingLog
    /// (ActionType = 'UNSIGN'), join EContracts để lấy tên KH/MST và ECtr_UnsignLogs
    /// để lấy kết quả gỡ ký.
    /// </summary>
    public class UnsignHistoryItem
    {
        public long Id { get; set; }
        public string? OID { get; set; }
        public string? CusName { get; set; }
        public string? CusTax { get; set; }
        public string? ActionBy { get; set; }
        public string? ActionByName { get; set; }
        public DateTime ActionDate { get; set; }
        public string? Reason { get; set; }
        public int? PrevSignNumb { get; set; }
        public string? UnsignStatus { get; set; }

        // Phân trang: tổng số bản ghi (COUNT(*) OVER()) lặp trên mỗi dòng.
        public int TotalCount { get; set; }
        public int RowNum { get; set; }
    }
}
