using ERP_Portal_RC.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IInvoiceService
    {
        /// <summary>
        /// Tạo hóa đơn nháp từ OID hợp đồng.
        /// Service tự fetch dữ liệu hợp đồng → mapping → gửi WinInvoice.
        /// </summary>
        Task<CreateInvoiceResultDto> CreateDraftInvoiceAsync(
            CreateInvoiceFromContractDto request,
        CancellationToken cancellationToken = default);
    }
}
