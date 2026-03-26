using ERP_Portal_RC.Domain.EntitiesInvoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        /// <summary>
        /// Gửi request tạo hóa đơn điện tử sang WinInvoice API
        /// </summary>
        /// <param name="payload">Payload đã mapping sẵn theo chuẩn WinInvoice</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Response từ WinInvoice</returns>
        Task<WinInvoiceCreateResponse> CreateInvoiceAsync(
            WinInvoiceCreateRequest payload,
            CancellationToken cancellationToken = default);
    }
}
