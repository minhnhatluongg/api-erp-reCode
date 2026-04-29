using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ITvanRenewalService
    {
        Task<PagedResult<TvanRenewalItemDto>> GetExpiringSoonAsync(
            TvanRenewalQueryDto query, CancellationToken ct = default);
    }
}
