using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ITemplateRepository
    {
        Task<InvoiceTemplate> GetByIdAsync(int id);
        Task<IEnumerable<InvoiceTemplate>> GetListAsync(string invoiceType = null);
        Task<InvoiceTemplate> GetByCodeAsync(string templateCode);
        Task<InvoiceTemplate> GetByFileNameAsync(string fileName);
        Task<bool> InsertTemplateAsync(InvoiceTemplate model);

        Task<bool> UpdateTemplateContentAsync(int templateId, string zippedBase64);

        Task<bool> DeleteTemplateAsync(int templateId);
    }
}
