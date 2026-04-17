using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces.Accounts_payable
{
    public interface IServiceTypeRepository
    {
        // ─────────────────────────────────────────────────────────────────────
        // QUERY
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_ServiceType_GetAll
        /// Trả về toàn bộ loại dịch vụ (active + inactive).
        /// Dùng cho màn hình quản trị, dropdown cấu hình.
        /// </summary>
        Task<IEnumerable<ServiceType>> GetAllAsync();

        /// <summary>
        /// sp_ServiceType_GetAll (filter IsActive = 1)
        /// Chỉ trả về loại dịch vụ đang hoạt động.
        /// Dùng khi tạo hợp đồng mới (người dùng chỉ thấy dịch vụ đang bán).
        /// </summary>
        Task<IEnumerable<ServiceType>> GetAllActiveAsync();

        /// <summary>
        /// sp_ServiceType_GetById
        /// Lấy chi tiết 1 loại dịch vụ theo ID, bao gồm thông tin Workflow liên kết.
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        /// <param name="serviceTypeId">PK của ServiceType</param>
        Task<ServiceType?> GetByIdAsync(int serviceTypeId);

        /// <summary>
        /// Kiểm tra mã dịch vụ đã tồn tại chưa — dùng để validate trước khi Create/Update.
        /// </summary>
        /// <param name="serviceTypeCode">Mã dịch vụ (ví dụ: "HDDT", "PMKT")</param>
        /// <param name="excludeId">Bỏ qua ID này khi check — dùng khi Update</param>
        Task<bool> IsCodeExistsAsync(string serviceTypeCode, int? excludeId = null);

        // ─────────────────────────────────────────────────────────────────────
        // COMMAND
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// sp_ServiceType_Create
        /// Thêm loại dịch vụ mới, trả về ID vừa tạo.
        /// Mặc định IsActive = true, WorkflowID có thể gán sau.
        /// </summary>
        /// <param name="entity">Dữ liệu ServiceType cần tạo</param>
        /// <returns>ServiceTypeID vừa INSERT</returns>
        Task<int> CreateAsync(ServiceType entity);

        /// <summary>
        /// sp_ServiceType_Update
        /// Cập nhật thông tin loại dịch vụ (tên, mã, WorkflowID, IsActive).
        /// </summary>
        /// <param name="entity">Dữ liệu ServiceType đã sửa, phải có ServiceTypeID</param>
        /// <returns>true nếu UPDATE thành công (rowsAffected > 0)</returns>
        Task<bool> UpdateAsync(ServiceType entity);

        /// <summary>
        /// Soft-delete: set IsActive = false thay vì xóa vật lý.
        /// Không xóa hẳn vì ServiceType có thể đang được dùng trong PaymentRecord cũ.
        /// </summary>
        /// <param name="serviceTypeId">ID cần vô hiệu hóa</param>
        Task<bool> DeactivateAsync(int serviceTypeId);

    }
}
