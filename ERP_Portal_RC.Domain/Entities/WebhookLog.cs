namespace ERP_Portal_RC.Domain.Entities
{
    public class WebhookLog
    {
        public long    Id           { get; set; }
        public string  EventType    { get; set; } = string.Empty;  // "INVOICE_EXPORTED"
        public string  ContractOid  { get; set; } = string.Empty;
        public string? InvoiceNo    { get; set; }
        public string? InvoiceSign  { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? GovCode      { get; set; }
        public string? SourceAction { get; set; }
        public string? RawPayload   { get; set; }
        public string  ClientIp     { get; set; } = string.Empty;
        public string  Status       { get; set; } = string.Empty;  // SUCCESS / FAILED / DUPLICATE / BLOCKED
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt   { get; set; } = DateTime.Now;
    }
}
