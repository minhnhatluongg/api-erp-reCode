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
    }
}
