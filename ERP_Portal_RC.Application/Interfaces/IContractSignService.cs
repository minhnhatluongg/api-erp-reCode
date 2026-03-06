using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces
{
    /// <summary>
    /// Service contract cho nghiệp vụ ký số hợp đồng điện tử.
    /// Application layer: có thể dùng cả Application.DTOs lẫn Domain.Entities.
    /// Xử lý business logic, validate, route theo phương thức ký, tạo XML…
    /// </summary>
    public interface IContractSignService
    {
        // ─── Main Sign ───────────────────────────────────────────────────────────

        /// <summary>
        /// Ký hợp đồng theo phương thức (SERVER / APP / HSM).
        /// Trả về Domain entity SignContractResult.
        /// </summary>
        Task<SignContractResult> SignContractAsync(SignContractRequestDto request, string userName);

        /// <summary>
        /// Kiểm tra nhanh hợp đồng đã được ký chưa (tra cứu ECtr_PublicInfo).
        /// </summary>
        Task<(bool IsSigned, string Message)> IsSignedAsync(string oid);

        /// <summary>
        /// Kiểm tra trạng thái ký chi tiết theo phương thức.
        /// Trả về Domain entity CheckSignStatusResult.
        /// </summary>
        Task<CheckSignStatusResult> CheckSignStatusAsync(string oid, string signMethod);

        // ─── App-Sign / SignApp Callback ─────────────────────────────────────────

        /// <summary>
        /// Tiếp nhận trạng thái xử lý từ SignApp (API 2.a).
        /// </summary>
        Task<(bool IsSuccess, string Message)> ReceiveSignStatusAsync(SignStatusCallbackRequest request);

        /// <summary>
        /// Kiểm tra chuỗi JWT từ SignApp (API 2.b).
        /// Trả về ApiInfo đã mã hóa nếu hợp lệ.
        /// </summary>
        ValidJwtResponse ValidJwt(ValidJwtRequest request);

        /// <summary>
        /// Lấy danh sách OID cần ký (API 2.c – GetInvParam).
        /// </summary>
        Task<GetInvParamResponse> GetInvParamAsync(GetInvParamRequest request);

        /// <summary>
        /// Lấy XML để ký số, đã mã hóa Base64 (API 2.d – GetXmlAll).
        /// </summary>
        Task<GetXmlAllResponse> GetXmlAllAsync(GetXmlAllRequest request);

        /// <summary>
        /// Nộp XML đã ký lên server, lưu vào DB (API 2.e – SetSignedXml).
        /// </summary>
        Task<SetSignedXmlResponse> SetSignedXmlAsync(SetSignedXmlRequest request);
    }
}
