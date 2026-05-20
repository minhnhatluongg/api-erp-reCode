using ERP_Portal_RC.Domain.Entities.Tax;

namespace ERP_Portal_RC.Application.DTOs.Tax
{
    /// <summary>
    /// Response của GET /api/Tax/get-full-info-by-mst.
    /// Tổng hợp dữ liệu BosOnline (econtract) + BosEVAT (thông tin DN) + BosTVAN (đã tờ khai chưa).
    /// </summary>
    public class TaxFullInfoDto
    {
        public string? CusTax { get; set; }
        public string? CusCMND_ID { get; set; }
        public string? OID { get; set; }
        public string? CusPeople_Sign { get; set; }
        public string? CusEmail { get; set; }
        public string? CusTel { get; set; }
        public string? SName { get; set; }
        public string? Address { get; set; }
        public bool IsToKhai { get; set; }
        public string? CusWebsite { get; set; }
        public string? CusBankNumber { get; set; }
        public string? CusBankAddress { get; set; }
        public TaxContractRange? ContractRange { get; set; }
    }
}
