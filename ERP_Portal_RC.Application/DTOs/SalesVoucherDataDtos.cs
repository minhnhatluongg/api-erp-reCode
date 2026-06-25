using System.Collections.Generic;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Dữ liệu gốc dựng "Chứng từ bán hàng" bên LOT (GoodsServiceSalesModal) từ
    /// 1 hợp đồng. LOT tự tách giá chưa thuế từ UnitPrice (GỒM VAT) + VatRate.
    /// </summary>
    public class SalesVoucherDataDto
    {
        public string Oid { get; set; } = "";
        public string CustomerCode { get; set; } = "";   // CustomerID (mã KH)
        public string CustomerName { get; set; } = "";   // CusName
        public string TaxCode { get; set; } = "";        // CusTax (MST)
        public string Cccd { get; set; } = "";           // CusCMND_ID (CCCD, ưu tiên)
        public string Address { get; set; } = "";        // CusAddress
        public string Phone { get; set; } = "";          // CusTel
        public string ContactPerson { get; set; } = "";  // CusPeople_Sign
        public string SalesPersonCode { get; set; } = "";// SaleEmID
        public string SalesPersonName { get; set; } = "";// SaleFullName
        public string Description { get; set; } = "";    // Descrip
        public List<SalesVoucherLineDto> Lines { get; set; } = new();
    }

    public class SalesVoucherLineDto
    {
        public string ProductCode { get; set; } = "";    // ItemID
        public string ProductName { get; set; } = "";    // ItemName
        public string Unit { get; set; } = "";           // itemUnitName / ItemUnit
        public decimal Quantity { get; set; }            // ItemQtty
        public decimal UnitPrice { get; set; }           // ItemPrice (GỒM VAT)
        public decimal VatRate { get; set; }             // VAT_Rate (%)
    }
}
