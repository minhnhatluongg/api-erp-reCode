using System;

namespace ERP_Portal_RC.Domain.Entities.Tax
{
    /// <summary>
    /// 1 dòng hợp đồng tóm tắt — kết quả SP BosOnline..sp_GetListContractByTaxCode.
    /// </summary>
    public class ContractSummaryRow
    {
        public string? OID { get; set; }
        public DateTime? NgayTaoHopDong { get; set; }
        public string? SaleEmID { get; set; }
        public string? MA_NV_KDoanh { get; set; }
        public string? MauSo { get; set; }
        public string? KyHieu { get; set; }
        public int? TuSo { get; set; }
        public int? DenSo { get; set; }
    }
}
