namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Map từ kết quả SP GetJobStatusByOID.
    /// Các field phải khớp chính xác với tên cột SP trả về (Dapper map by name).
    /// </summary>
    public class JobStatusItem
    {
        // ── Định danh ─────────────────────────────────────────────────────
        public string?   JobOID       { get; set; }   // OID của Job (VD: 000635/...-001)
        public string?   ContractOID  { get; set; }   // ReferenceID = OID hợp đồng
        public string?   FactorID     { get; set; }   // JOB_00001 / JOB_00003 ...
        public string?   EntryID      { get; set; }   // JB:001 / JB:003 ...
        public string?   EntryName    { get; set; }   // "Kích hoạt tài khoản"...

        // ── Trạng thái hiện tại (latest từ zsgn_EContractJobs) ────────────
        public int       CurrSignNumb { get; set; }   // -1 / 101 / 201 / 301 / 100
        public string?   SignStatus   { get; set; }   // "Đang chờ duyệt" / "Đã hoàn thành"...
        public string?   Category     { get; set; }   // done / waiting / returned / pending
        public DateTime? LastSignDate { get; set; }
        public string?   LastActionBy { get; set; }   // FullName người thực hiện cuối
        public string?   LastAppvMess { get; set; }

        // ── Thông tin Job gốc (EContractJobs) ────────────────────────────
        public string?   Descrip      { get; set; }
        public string?   OperDept     { get; set; }
        public DateTime? JobCreatedAt { get; set; }   // Crt_Date của Job
        public string?   InvcSign     { get; set; }
        public int       InvcFrm      { get; set; }
        public int       InvcEnd      { get; set; }
        public string?   InvcSample   { get; set; }
        public string?   FileInvoice  { get; set; }
        public string?   FileOther    { get; set; }
    }
}
