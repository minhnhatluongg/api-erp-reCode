namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Record lưu thông tin process ký (bảng EVAT_AppSign_Process).
    /// </summary>
    public class AppSignProcess
    {
        public string KeyUID { get; set; } = string.Empty;
        public string OID { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? StatusMessage { get; set; }
        public string? PayloadDataJson { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    // ─── Domain Models cho IContractSignRepository ──────────────────────────────
    // Rule: IContractSignRepository nằm ở Domain → các type tham số/return
    // của nó PHẢI là Domain entities, KHÔNG ĐƯỢC là Application DTOs.

    /// <summary>
    /// Đầu vào cho ký server: OID + phương thức + người yêu cầu.
    /// </summary>
    public class SignContractDomainRequest
    {
        public string OID { get; set; } = string.Empty;
        public string SignMethod { get; set; } = "SERVER";
        public string? ReqUser { get; set; }
    }

    /// <summary>
    /// Kết quả trả về sau khi ký hợp đồng.
    /// </summary>
    public class SignContractResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Kết quả kiểm tra trạng thái ký.
    /// </summary>
    public class CheckSignStatusResult
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Một OID đang chờ ký (dùng cho GetPendingOidsByKeyAsync / GetInvParam).
    /// </summary>
    public class PendingOidItem
    {
        /// <summary>OID hợp đồng cần ký.</summary>
        public string InvOID { get; set; } = string.Empty;
    }
}
