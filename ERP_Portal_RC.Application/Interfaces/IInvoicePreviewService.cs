using ERP_Portal_RC.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IInvoicePreviewService
    {
        Task<string> BuildInvoiceHtmlAsync(InvoiceBuildRequest req);
        Task<InvoiceSampleResult> BuildSampleFilesAsync(InvoiceBuildRequest req, string saleID);
    }
}
