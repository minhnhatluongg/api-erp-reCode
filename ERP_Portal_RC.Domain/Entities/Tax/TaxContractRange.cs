namespace ERP_Portal_RC.Domain.Entities.Tax
{
    /// <summary>
    /// Kết quả SP BosOnline..Check_Econtract (kiểm tra phạm vi hợp đồng theo MST + InvcSign + InvcSample).
    /// </summary>
    public class TaxContractRange
    {
        public string? OID { get; set; }
        public string? Crt_User { get; set; }
        public string? Crt_Date { get; set; }
        public string? InvcSample { get; set; }
        public string? InvcSign { get; set; }
        public int? InvcFrm { get; set; }
        public int? InvcEnd { get; set; }
    }
}
