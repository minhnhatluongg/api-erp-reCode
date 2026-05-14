using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;

namespace ERP_Portal_RC.Application.Interfaces.Admin
{
    public interface IAdminEcontractService
    {
        /// <summary>Bypass Cấp tài khoản: JOB_00003/JB:003 → 0→101→201.</summary>
        Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> BypassCapTaiKhoanAsync(string oid, string crtUser);

        /// <summary>Bypass Phát hành hóa đơn: JOB_00002/JB:004 → 0→101→201.</summary>
        Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> BypassPhatHanhHoaDonAsync(string oid, string crtUser);

        /// <summary>Bypass Xuất hóa đơn HĐĐT: JOB_00005/JB:010 → 0→101→301.</summary>
        Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> BypassXuatHoaDonHDDTAsync(string oid, string crtUser);
    }
}
