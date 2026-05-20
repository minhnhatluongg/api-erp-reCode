using System.Text.Json.Serialization;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// DTO cho API Quick Publish (/api/InvoicePreview/quick-publish)
    /// Kế thừa PreviewRequestDto + bổ sung các thông tin phát hành mẫu chính thức.
    /// </summary>
    public class FinalConfirmSampleDto : PreviewRequestDto
    {
        public string? OID { get; set; }
        public string? CusPosition_BySign { get; set; }

        /// <summary>
        /// XSLT đã xử lý (Base64 - encodedStr)
        /// </summary>
        public string? ConfiguredXsltBase64 { get; set; }

        /// <summary>
        /// Base64 của Logo
        /// </summary>
        public string? LogoBase64 { get; set; }

        /// <summary>
        /// Base64 của Background
        /// </summary>
        public string? BackgroundBase64 { get; set; }

        /// <summary>
        /// Tên file XSLT (_fileName)
        /// </summary>
        public string? XsltFileName { get; set; }

        /// <summary>
        /// Tên file Logo (txtLogoName.Text)
        /// </summary>
        public string? logoFileName { get; set; }

        /// <summary>
        /// Tên file Background (txtBackground.Text)
        /// </summary>
        public string? backgroundFileName { get; set; }

        [JsonPropertyName("invFrom")]
        public int InvFrom { get; set; }

        [JsonPropertyName("invTo")]
        public int InvTo { get; set; }

        public string? InvSample { get; set; }
        public string? InvSign { get; set; }

        /// <summary>
        /// "new" hoặc "existing"
        /// </summary>
        public string? CustomerType { get; set; }

        /// <summary>
        /// "NEW" hoặc templateId
        /// </summary>
        public string? SampleId { get; set; }

        /// <summary>
        /// "EXPOR_GOODSINVC" hoặc "EXPOR_INVCVCNB"
        /// </summary>
        public string? FactorId { get; set; }
    }
}
