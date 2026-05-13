namespace ERP_Portal_RC.Domain.Entities
{
    public class JobHistoryEntity
    {
        public string? JobOID       { get; set; }
        public string? ContractOID  { get; set; }
        public string? FactorID     { get; set; }
        public string? EntryID      { get; set; }
        public string? EntryName    { get; set; }  // Tên task: "Kích hoạt tài khoản"...
        public string? SignNumb     { get; set; }  // "101", "201", "100"
        public string? SignStatus   { get; set; }  // "Đang chờ duyệt", "Đã hoàn thành"...
        public DateTime? SignDate   { get; set; }
        public DateTime? Crt_Date   { get; set; }
        public string? Crt_User     { get; set; }
        public string? FullName     { get; set; }
        public string? AppvMess     { get; set; }
    }
}
