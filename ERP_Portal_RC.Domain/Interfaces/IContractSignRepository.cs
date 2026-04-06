using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    /// <summary>
    /// Repository contract cho nghiệp vụ ký số hợp đồng điện tử.
    /// Tách biệt với IEContractRepository (quản lý hợp đồng) để đúng Single Responsibility.
    ///
    /// Clean Architecture rule: Domain interface CHỈ dùng Domain entities làm
    /// tham số / kiểu trả về — KHÔNG được phụ thuộc Application.DTOs.
    /// </summary>
    public interface IContractSignRepository
    {
        // ─── IsSigned ───────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra hợp đồng đã được ký số chưa (tra cứu ECtr_PublicInfo).
        /// </summary>
        Task<(bool IsSigned, string Message)> IsSignedAsync(string oid);

        // ─── AppSign Process ────────────────────────────────────────────────────

        /// <summary>
        /// Cập nhật trạng thái process ký từ SignApp callback (theo ProcessName / KeyUID).
        /// </summary>
        Task<bool> UpdateSignStatusAsync(string processName, int status, string message);

        /// <summary>
        /// Lấy danh sách OID đang chờ ký theo KeyUID (dùng cho GetInvParam API 2.c).
        /// </summary>
        Task<List<PendingOidItem>> GetPendingOidsByKeyAsync(string keyId);

        /// <summary>
        /// Lấy PayloadDataJson của process theo OID (dùng để build XML hợp đồng).
        /// </summary>
        Task<string?> GetPayloadByOidAsync(string oid);

        /// <summary>
        /// Lưu XML đã ký vào DB bằng SP Ins_ContractContent_SignedByOdoo_origin.
        /// Tham số là primitive types — không phụ thuộc Application layer.
        /// </summary>
        Task<bool> SaveSignedXmlAsync(
            string oid,
            string signedXmlBase64,
            DateTime orderDate,
            string partnerSoCCCD,
            string partnerVat,
            string partnerName,
            string companyTax,
            string companyName);

        /// <summary>
        /// Cập nhật trạng thái AppSign process theo OID (sau khi lưu XML thành công).
        /// </summary>
        Task UpdateAppSignStatusByOidAsync(string oid, int status, string message);

        // ─── Server Sign ─────────────────────────────────────────────────────────

        /// <summary>
        /// Thực hiện ký số phía Server (kiểm tra tiền điều kiện trước khi ký).
        /// </summary>
        Task<SignContractResult> SignContractServerAsync(SignContractDomainRequest request);

        /// <summary>
        /// Kiểm tra trạng thái ký phía Server (tra cứu ECtr_PublicInfo).
        /// </summary>
        Task<CheckSignStatusResult> CheckSignStatusServerAsync(string oid);
    }
}
