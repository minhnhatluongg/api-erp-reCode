using System.Collections.Generic;
using System.Threading.Tasks;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces
{
    /// <summary>
    /// Service cho 2 API hỗ trợ FE khi tạo/gia hạn đơn hàng:
    ///   - get-products : danh sách gói dịch vụ (dropdown sản phẩm).
    ///   - owner-info   : thông tin công ty chủ quản (Bên B) để fill input.
    /// Đã được port từ TVAN_WEB_API.OdooOrdersController, giữ nguyên logic gọi SP.
    /// </summary>
    public interface IOdooOrderService
    {
        Task<List<ProductResponse>> GetProductsAsync(GetProductsRequest request);
        Task<OwnerContract?> GetOwnerInfoAsync(string companyId = "26");
    }
}
