namespace ERP_Portal_RC.Domain.Entities.Tax
{
    /// <summary>
    /// 1 dòng sản phẩm/dịch vụ trên hợp đồng — kết quả SP BosOnline..GetEcontractDetailByOID.
    /// </summary>
    public class TaxProductRow
    {
        public string? ItemID { get; set; }
        public string? ItemName { get; set; }
        public string? ItemUnit { get; set; }
        public string? ItemUnitName { get; set; }
        public int ItemPerBox { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public decimal ItemPrice { get; set; }
    }
}
