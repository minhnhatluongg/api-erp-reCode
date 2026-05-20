namespace ERP_Portal_RC.Application.DTOs.InvoiceTemplate
{
    /// <summary>
    /// Phần tử cho combobox /api/invoice/templates/all.
    /// </summary>
    public class InvoiceTemplateListItemDto
    {
        public int TemplateID { get; set; }
        public string? TemplateCode { get; set; }
        public string? TemplateName { get; set; }
    }
}
