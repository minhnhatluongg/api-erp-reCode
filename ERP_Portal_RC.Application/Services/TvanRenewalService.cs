using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class TvanRenewalService : ITvanRenewalService
    {
        private readonly ITvanRenewalRepository _repo;
        private readonly IMapper _mapper;
        public TvanRenewalService(ITvanRenewalRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }
        public async Task<PagedResult<TvanRenewalItemDto>> GetExpiringSoonAsync(TvanRenewalQueryDto query, CancellationToken ct = default)
        {
            var result = await _repo.GetExpiringSoonAsync(
                query.DaysBeforeExpiry,
                query.IncludeExpired,
                NullIfEmpty(query.Mst),
                NullIfEmpty(query.SaleCode),
                NullIfEmpty(query.Keyword),
                NullIfEmpty(query.RangeKey),
                query.Page,
                query.Size,
                ct);

            var items = _mapper.Map<List<TvanRenewalItemDto>>(result.Items);

            return new PagedResult<TvanRenewalItemDto>(items, result.Total, result.Page, result.Size);
        }

        private static string? NullIfEmpty(string? s) 
            => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
