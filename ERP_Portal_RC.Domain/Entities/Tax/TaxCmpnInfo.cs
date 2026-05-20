namespace ERP_Portal_RC.Domain.Entities.Tax
{
    /// <summary>
    /// Thông tin công ty trên server EVAT (BosEVAT..GetInfoByTaxcode_v25).
    /// Đặt tên TaxCmpnInfo để tránh trùng với CmpnInfo2 ở Application.DTOs.
    /// </summary>
    public class TaxCmpnInfo
    {
        public string? SampleID { get; set; }
        public string? LogoBase64 { get; set; }
        public string? Filelogo { get; set; }
        public string? BackgroundBase64 { get; set; }
        public string? FileBackground { get; set; }
        public string? SName { get; set; }
        public string? Tel { get; set; }
        public string? Fax { get; set; }
        public string? Address { get; set; }
        public string? BankInfo { get; set; }
        public string? website { get; set; }
        public string? Email { get; set; }
        public string? BankNumber { get; set; }
        public string? BankAddress { get; set; }
        public string? MerchantID { get; set; }
        public string? PersonOfMerchant { get; set; }
        public string? SaleID { get; set; }
    }
}
