using ERP_Portal_RC.Application.DTOs;

namespace ERP_Portal_RC.Application.DTOs.InvoiceTemplate
{
    /// <summary>
    /// Response cho 2 endpoint trả XSLT đã decode:
    ///  - GET /api/invoice/templates/{templateId}
    ///  - GET /api/invoice/templates/bycode/{templateCode}
    /// </summary>
    public class InvoiceTemplateXsltDto
    {
        public int TemplateID { get; set; }
        public string? TemplateCode { get; set; }
        public string? TemplateName { get; set; }
        public string? RawXslt { get; set; }

        /// <summary>
        /// Cấu hình ẩn/hiện + viền dò được TRỰC TIẾP từ file mẫu (để FE tick checkbox đúng 100%).
        /// </summary>
        public AdjustConfigDto? DetectedConfig { get; set; }
    }

    /// <summary>
    /// Body cho POST /api/invoice/templates/detect-config — dò cấu hình từ XSLT người dùng tự upload.
    /// </summary>
    public class DetectTemplateConfigRequest
    {
        public string? RawXslt { get; set; }
    }
}
