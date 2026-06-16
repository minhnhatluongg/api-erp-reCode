namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Sale rút/xóa yêu cầu Job đang chờ duyệt (SignNumb mới nhất = 101).
    /// OID = OID của JOB (vd 000642/260521:140646-004).
    /// CrtUser = win_id người thao tác (phải trùng người tạo job) — LOT-ERP
    /// truyền xuống; nếu null thì controller fallback UserCode token.
    /// </summary>
    public class DeleteJobRequestDto
    {
        public string OID { get; set; } = string.Empty;
        public string? CrtUser { get; set; }
    }
}
