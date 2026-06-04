using System.Threading.Tasks;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;

namespace ERP_Portal_RC.Application.Interfaces
{
    /// <summary>
    /// Service riêng cho luồng tích hợp bên ngoài (Landingpage / Zalo Mini App / Email / Smax.ai / n8n).
    /// Khác EcontractService.ProcessSaveContractAsync ở chỗ:
    ///   - Lightweight input (chỉ những field cần thiết).
    ///   - Server tự fill Bên B từ Check_OwnerContract.
    ///   - Server tự resolve gói cước từ ItemID qua wspProducts_Tool_v25.
    /// </summary>
    public interface IIntegrationContractService
    {
        /// <summary>
        /// Tạo mới hoặc gia hạn đơn hàng từ luồng tích hợp ngoài.
        /// Trả ApiResponse&lt;QuickCreateContractResponse&gt; — Data.OID là OID hợp đồng vừa lưu.
        /// </summary>
        Task<ApiResponse<QuickCreateContractResponse>> QuickCreateAsync(
            QuickCreateContractRequest request,
            string callerUserCode);

        /// <summary>
        /// Tạo "Hợp đồng Ma": hợp đồng bình thường (có SaleEmID thật) nhưng server tự chèn
        /// 2 gói miễn phí (gói hóa đơn + gói truyền nhận, giá 0) để hợp thức hóa dải hóa đơn.
        /// Caller chỉ cần gửi mẫu số / ký hiệu / từ số → đến số; phần còn lại lấy từ config GhostContract.
        /// Trả ApiResponse&lt;GhostContractResponse&gt; — Data.OID là OID hợp đồng vừa lưu.
        /// </summary>
        Task<ApiResponse<GhostContractResponse>> CreateGhostContractAsync(
            GhostContractRequest request,
            string callerUserCode);
    }
}
