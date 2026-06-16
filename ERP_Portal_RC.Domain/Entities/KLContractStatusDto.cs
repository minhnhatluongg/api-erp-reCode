using System;
using System.Collections.Generic;

namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// (DANH SÁCH) Tóm tắt trạng thái hợp đồng cho Khánh Linh đối soát — kèm số lần gỡ ký / số đề xuất chờ.
    /// </summary>
    public class KLContractStatusDto
    {
        public string OID { get; set; }
        public string CusTax { get; set; }
        public string CusName { get; set; }
        public int SignNumb { get; set; }
        public bool IsUnsigned { get; set; }
        public string StatusText { get; set; }
        public DateTime? SignedAt { get; set; }
        public DateTime? LastUnsignedAt { get; set; }   // mốc gỡ ký gần nhất
        public int UnsignCount { get; set; }            // tổng số lần đã gỡ ký
        public int RequestCount { get; set; }           // tổng số đề xuất gỡ ký
        public int PendingRequestCount { get; set; }    // số đề xuất đang chờ kế toán duyệt
        public DateTime? LastChangedAt { get; set; }
        public int TotalCount { get; set; }             // phục vụ paging
    }

    /// <summary>(CHI TIẾT) 1 hợp đồng: tóm tắt + toàn bộ lịch sử gỡ ký + toàn bộ đề xuất gỡ ký.</summary>
    public class KLContractDetailDto
    {
        public string OID { get; set; }
        public string CusTax { get; set; }
        public string CusName { get; set; }
        public int SignNumb { get; set; }
        public bool IsUnsigned { get; set; }
        public string StatusText { get; set; }
        public DateTime? SignedAt { get; set; }
        public int UnsignCount { get; set; }
        public int RequestCount { get; set; }
        public int PendingRequestCount { get; set; }
        public DateTime? LastChangedAt { get; set; }

        public List<KLUnsignEventDto> UnsignHistory { get; set; } = new List<KLUnsignEventDto>();
        public List<KLUnsignRequestDto> Requests { get; set; } = new List<KLUnsignRequestDto>();
    }

    /// <summary>1 lần gỡ ký (gỡ lần 1, 2, 3...).</summary>
    public class KLUnsignEventDto
    {
        public int Seq { get; set; }                 // lần gỡ thứ mấy (1,2,3...)
        public DateTime? UnsignedAt { get; set; }
        public string UnsignedBy { get; set; }
        public string UnsignedByName { get; set; }
        public string Reason { get; set; }
        public string CorrelationId { get; set; }
    }

    /// <summary>1 đề xuất gỡ ký (sale -> kế toán) và trạng thái duyệt.</summary>
    public class KLUnsignRequestDto
    {
        public long RequestID { get; set; }
        public string Status { get; set; }           // PENDING/APPROVED/REJECTED/CANCELLED/FAILED
        public string Reason { get; set; }
        public string RequestedBy { get; set; }
        public string RequestedByName { get; set; }
        public DateTime? RequestedAt { get; set; }
        public string ReviewedBy { get; set; }
        public string ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNote { get; set; }
        public string UnsignStatus { get; set; }
        public string UnsignMessage { get; set; }
    }
}
