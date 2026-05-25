using System;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// DTO cho phản hồi danh sách sản phẩm/gói dịch vụ — dùng cho dropdown chọn gói
    /// khi tạo / gia hạn đơn hàng.
    /// (Copy nguyên schema từ TVAN_WEB_API.ProductResponse — giữ nguyên kiểu/tên field.)
    /// </summary>
    public class ProductResponse
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
    /// Tham số truy vấn cho /api/odoo/orders/get-products.
    /// Bind từ query-string. Tất cả đều optional.
    /// </summary>
    public class GetProductsRequest
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
