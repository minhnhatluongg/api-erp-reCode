using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;

namespace ERP_Portal_RC.Application.Services.Admin
{
    public class AdminEcontractService : IAdminEcontractService
    {
        private readonly IEContractRepository _repo;

        public AdminEcontractService(IEContractRepository repo)
        {
            _repo = repo;
        }

        public Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> BypassCapTaiKhoanAsync(string oid, string crtUser)
            => RunBypass(oid, "JOB_00003", "JB:003", finalSign: 201, crtUser);

        public Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> BypassPhatHanhHoaDonAsync(string oid, string crtUser)
            => RunBypass(oid, "JOB_00002", "JB:004", finalSign: 201, crtUser);

        public Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> BypassXuatHoaDonHDDTAsync(string oid, string crtUser)
            => RunBypass(oid, "JOB_00005", "JB:010", finalSign: 301, crtUser);

        // ── Core logic ───────────────────────────────────────────────────────
        private async Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> RunBypass(
            string oid, string factorId, string entryId, int finalSign, string crtUser)
        {
            try
            {
                var result = await _repo.BypassJobAsync(oid, factorId, entryId, finalSign, crtUser);

                var data = new DeXuatCapTaiKhoanResponseDto
                {
                    OIDJob        = result.OIDJob,
                    ReferenceInfo = result.ReferenceInfo
                };

                string message = result.IsAlreadyExists
                    ? $"Job đã đạt trạng thái cuối (idempotent). Job: {result.OIDJob}"
                    : result.ReferenceInfo;

                return ApiResponse<DeXuatCapTaiKhoanResponseDto>.SuccessResponse(data, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<DeXuatCapTaiKhoanResponseDto>.ErrorResponse(ex.Message, 400);
            }
        }
    }
}
