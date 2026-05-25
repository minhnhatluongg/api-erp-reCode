namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Entity ánh xạ 1-1 với output của SP BosOnline..wspProducts_Tool_v25.
    /// Dùng làm input mapping cho DTO ProductResponse ở tầng Application.
    /// (Port từ TVAN_WEB_API.ProductResponse.)
    /// </summary>
    public class ProductCatalogItem
    {
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemUnit { get; set; }
        public string ItemUnitName { get; set; }
        public int ItemPerBox { get; set; }
        public decimal ItemPrice { get; set; }
        public string VAT_Rate { get; set; }
        public string VAT_Name { get; set; }
        public string ItemType { get; set; }
        public int IsRepaire { get; set; }
    }

    /// <summary>
    /// Tham số filter cho SP wspProducts_Tool_v25.
    /// </summary>
    public class ProductCatalogQuery
    {
        public string ClnID { get; set; } = string.Empty;
        public string ZoneID { get; set; } = string.Empty;
        public string RegionID { get; set; } = string.Empty;
        public string ASM { get; set; } = string.Empty;
        public string SUB { get; set; } = string.Empty;
        public string TEAM { get; set; } = string.Empty;
        public string CustomerID { get; set; } = string.Empty;
        public string MembType { get; set; } = string.Empty;
        public int onlyTVAN { get; set; } = 0;
    }
}
