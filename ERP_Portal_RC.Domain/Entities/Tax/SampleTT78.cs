namespace ERP_Portal_RC.Domain.Entities.Tax
{
    /// <summary>
    /// 1 mẫu hóa đơn TT78 — kết quả SP BosEVAT..GetSampleIDByTaxCode_TT78_v25.
    /// </summary>
    public sealed class SampleTT78
    {
        public string? SampleID { get; set; }
        public string? SampleCode { get; set; }
        public string? govSampleSign { get; set; }
        public string? govInvcSign { get; set; }
        public int? InvcRemn { get; set; }
    }
}
