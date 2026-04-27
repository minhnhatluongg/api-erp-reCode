using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IVirusScanService
    {
        Task<(bool IsClean, string VirusName)> ScanAsync(Stream fileStream, CancellationToken ct = default);
    }
}
