namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Tracking vòng đời chỉnh sửa hợp đồng sau khi gỡ ký.
    /// Map từ ECtr_ContractTrackingLog (BosControlEVAT).
    /// </summary>
    public class ContractTrackingEntity
    {
        public long     Id           { get; set; }
        public string?  ContractOID  { get; set; }
        public string?  ActionType   { get; set; }   // UNSIGN | EDIT | RESUBMIT
        public string?  ActionLabel  { get; set; }   // Text tiếng Việt
        public string?  ActionBy     { get; set; }
        public string?  ActionByName { get; set; }
        public string?  Role         { get; set; }
        public DateTime ActionDate   { get; set; }
        public string?  Reason       { get; set; }
        public string?  Notes        { get; set; }
        public string?  CorrelationId { get; set; }
        public int?     PrevSignNumb { get; set; }
    }
}
