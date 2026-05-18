using ERP_Portal_RC.Application.Interfaces.Admin;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;

namespace ERP_Portal_RC.Application.Services.Admin
{
    public class AdminContractSummaryService : IAdminContractSummaryService
    {
        private readonly IAdminContractSummaryRepository _repo;

        public AdminContractSummaryService(IAdminContractSummaryRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiResponse<ContractSummaryResponse>> GetSummaryAsync(string oid)
        {
            string cleanOid = System.Net.WebUtility.UrlDecode(oid.Trim());

            try
            {
                var summary = await _repo.GetSummaryAsync(cleanOid);

                if (summary == null)
                    return ApiResponse<ContractSummaryResponse>.ErrorResponse(
                        $"Không tìm thấy hợp đồng OID = '{cleanOid}'.", 404);

                var contract = summary.Contract!;

                return ApiResponse<ContractSummaryResponse>.SuccessResponse(
                    summary,
                    $"Snapshot '{cleanOid}' — SignNumb={contract.CurrSignNumb}, " +
                    $"{summary.Jobs.Count} job, {summary.SignHistory.Count} bước ký, " +
                    $"{summary.Tracking.Count} thay đổi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<ContractSummaryResponse>.ErrorResponse(
                    $"Lỗi hệ thống: {ex.Message}", 500);
            }
        }
    }
}
