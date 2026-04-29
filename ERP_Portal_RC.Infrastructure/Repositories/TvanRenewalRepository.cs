using Dapper;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class TvanRenewalRepository : ITvanRenewalRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";

        public TvanRenewalRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task<PagedResult<TvanRenewalItem>> GetExpiringSoonAsync(int daysBeforeExpiry, bool includeExpired, string? searchMst, string? searchSaleCode, string? searchKeyword,string? rangeKey, int page, int size, CancellationToken ct = default)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var p = new DynamicParameters();
            p.Add("@DaysBeforeExpiry", daysBeforeExpiry, DbType.Int32);
            p.Add("@IncludeExpired", includeExpired, DbType.Boolean);
            p.Add("@SearchMst", searchMst, DbType.String, size: 50);
            p.Add("@SearchSaleCode", searchSaleCode, DbType.String, size: 50);
            p.Add("@SearchKeyword", searchKeyword, DbType.String, size: 200);
            p.Add("@RangeKey", rangeKey, DbType.AnsiString, size: 20);
            p.Add("@PageNumber", page, DbType.Int32);
            p.Add("@PageSize", size, DbType.Int32);
            p.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var cmd = new CommandDefinition(
                commandText: "dbo.sp_GetTvanContractsExpiringSoon",
                parameters: p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            var items = (await conn.QueryAsync<TvanRenewalItem>(cmd)).AsList();
            var total = p.Get<int>("@TotalRecords");

            return new PagedResult<TvanRenewalItem>(items, total, page, size);
        }
    }
}
