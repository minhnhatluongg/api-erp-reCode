using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ITvanRenewalRepository
    {
        Task<PagedResult<TvanRenewalItem>> GetExpiringSoonAsync(
                int daysBeforeExpiry,
                bool includeExpired,
                string? searchMst,
                string? searchSaleCode,
                string? searchKeyword,
                string? rangeKey,
                int page,
                int size,
                CancellationToken ct = default);
    }
}
