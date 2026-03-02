using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IRuleRepository
    {
        Task<IEnumerable<InvoiceTemplateRule>> GetListAsync();
        Task<InvoiceTemplateRule> GetByIdAsync(int id);
        Task<InvoiceTemplateRule> GetByCodeAsync(string code);
        Task<Dictionary<string, string>> GetAllActiveRulesAsync();
        Task<bool> InsertAsync(string ruleCode, string ruleName, string contentBase64Gzip);
        Task<bool> UpdateAsync(string ruleCode, string contentBase64Gzip);
        Task<bool> DeleteAsync(int ruleId);
    }
}
