using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IXmlDataRepository
    {
        Task<InvoiceXMLData> GetByIdAsync(int id);
        Task<InvoiceXMLData> GetByCodeAsync(string code);
        Task<List<InvoiceXMLData>> GetAllAsync();

        // Các hàm thêm/sửa/xóa nếu cần cho chức năng quản trị (CRUD)
        Task<bool> InsertAsync(InvoiceXMLData model);
        Task<bool> UpdateAsync(InvoiceXMLData model);
        Task<bool> DeleteAsync(int id);
    }
}
