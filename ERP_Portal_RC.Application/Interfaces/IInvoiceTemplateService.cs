using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.InvoiceTemplate;
using ERP_Portal_RC.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IInvoiceTemplateService
    {
        /// <summary>
        /// Lấy danh sách mẫu đang active (combobox).
        /// </summary>
        Task<IEnumerable<InvoiceTemplateListItemDto>> GetAllTemplatesAsync();

        /// <summary>
        /// Lấy nội dung XSLT thô (đã decode gzip+base64) theo TemplateID.
        /// </summary>
        Task<string?> GetRawXsltAsync(int templateId);

        /// <summary>
        /// Lấy XSLT theo TemplateID kèm cấu hình ẩn/hiện + viền dò được từ file mẫu (cho FE tick checkbox).
        /// </summary>
        Task<InvoiceTemplateXsltDto?> GetTemplateXsltAsync(int templateId);

        /// <summary>
        /// Dò cấu hình ẩn/hiện + viền từ một nội dung XSLT bất kỳ (vd file người dùng tự upload).
        /// </summary>
        AdjustConfigDto DetectConfig(string? rawXslt);

        /// <summary>
        /// Lấy thông tin mẫu kèm XSLT đã decode theo TemplateCode.
        /// </summary>
        Task<InvoiceTemplateXsltDto?> GetTemplateByCodeAsync(string templateCode);

        /// <summary>
        /// Lưu mẫu mới (encode trước khi insert).
        /// </summary>
        Task<bool> SaveTemplateAsync(InvoiceTemplate model);

        /// <summary>
        /// Cập nhật InvoiceContent theo TemplateCode.
        /// </summary>
        Task<bool> UpdateTemplateContentAsync(string templateCode, string xsltContent);

        /// <summary>
        /// Áp các rule (bật/tắt theo InvoiceConfigDto) vào XSLT.
        /// </summary>
        Task<string?> BuildXsltWithRulesAsync(string xsltRaw, InvoiceConfigDto options);

        /// <summary>
        /// Lấy mẫu theo ID + apply rule theo config.
        /// </summary>
        Task<string?> ConvertXsltAsync(int templateId, InvoiceConfigDto config);
    }
}
