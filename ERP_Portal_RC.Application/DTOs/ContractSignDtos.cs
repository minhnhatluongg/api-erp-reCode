using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.DTOs
{
    // ============================================================
    // Application DTOs — dùng ở Application layer và Controller.
    // Các Domain model (SignContractResult, CheckSignStatusResult,
    // PendingOidItem, SignContractDomainRequest) ở Domain.Entities.
    // ============================================================

    // ───── REQUEST DTOs (API input) ─────────────────────────────

    /// <summary>
    /// Request ký hợp đồng gửi từ Frontend (SERVER / APP / HSM).
    /// </summary>
    public class SignContractRequestDto
    {
        /// <summary>Mã OID hợp đồng. BẮT BUỘC.</summary>
        public string OID { get; set; } = string.Empty;

        /// <summary>Phương thức ký: SERVER | APP | HSM. Default: SERVER.</summary>
        public string SignMethod { get; set; } = "SERVER";

        /// <summary>Người yêu cầu ký. Nếu để trống sẽ lấy từ JWT.</summary>
        public string? ReqUser { get; set; }
    }

    /// <summary>
    /// Callback từ SignApp: đẩy trạng thái xử lý ký về server (API 2.a).
    /// </summary>
    public class SignStatusCallbackRequest
    {
        /// <summary>Key / tên process đang xử lý. BẮT BUỘC.</summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>Trạng thái: PENDING | PROCESSING | SUCCESS | ERROR | FAILED hoặc số nguyên.</summary>
        public string? Status { get; set; }

        /// <summary>Mô tả kết quả từ SignApp.</summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Kiểm tra JWT từ SignApp (API 2.b).
    /// </summary>
    public class ValidJwtRequest
    {
        /// <summary>Chuỗi JWT cần kiểm tra. BẮT BUỘC.</summary>
        public string Jwt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lấy danh sách OID cần ký (API 2.c).
    /// </summary>
    public class GetInvParamRequest
    {
        /// <summary>Key định danh process ký. BẮT BUỘC.</summary>
        public string KeyID { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lấy XML để ký số (API 2.d).
    /// </summary>
    public class GetXmlAllRequest
    {
        /// <summary>OID hợp đồng. BẮT BUỘC.</summary>
        public string Oid { get; set; } = string.Empty;
    }

    /// <summary>
    /// Nộp XML đã ký số lên server (API 2.e).
    /// </summary>
    public class SetSignedXmlRequest
    {
        /// <summary>OID hợp đồng. BẮT BUỘC.</summary>
        public string Oid { get; set; } = string.Empty;

        /// <summary>Nội dung XML đã ký, mã hóa Base64. BẮT BUỘC.</summary>
        public string XmlContentBase64 { get; set; } = string.Empty;

        /// <summary>Phiên bản SignApp.</summary>
        public string? AppSignVersion { get; set; }

        /// <summary>Serial của token / chứng thư số.</summary>
        public string? TokenSerial { get; set; }
    }

    // ───── RESPONSE DTOs (API output) ───────────────────────────

    /// <summary>
    /// Kết quả kiểm tra JWT hợp lệ (API 2.b response).
    /// </summary>
    public class ValidJwtResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public ValidJwtData? Data { get; set; }
        public DateTime ReturnDate { get; set; }
    }

    public class ValidJwtData
    {
        /// <summary>ApiInfo đã mã hóa BosEncrypt.</summary>
        public string ApiInfo { get; set; } = string.Empty;
        /// <summary>JWT gốc được echo lại.</summary>
        public string resJWT { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả GetInvParam (API 2.c response).
    /// Data chứa danh sách PendingOidItem (Domain entity).
    /// </summary>
    public class GetInvParamResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PendingOidItem> Data { get; set; } = new();
        public DateTime ReturnDate { get; set; }
    }

    /// <summary>
    /// Kết quả GetXmlAll (API 2.d response).
    /// </summary>
    public class GetXmlAllResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public GetXmlAllData? Data { get; set; }
        public DateTime ReturnDate { get; set; }
    }

    public class GetXmlAllData
    {
        public string XmlContent { get; set; } = string.Empty;
        public int IsSigned { get; set; }
        public string NodeToStore { get; set; } = string.Empty;
        public string NodeToSign { get; set; } = string.Empty;
        public string NodeSignID { get; set; } = string.Empty;
        public string SignTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả SetSignedXml (API 2.e response).
    /// </summary>
    public class SetSignedXmlResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public SetSignedXmlData? Data { get; set; }
        public DateTime ReturnDate { get; set; }
    }

    public class SetSignedXmlData
    {
        public string MaTD { get; set; } = string.Empty;
    }
}
