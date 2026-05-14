using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class PartnerRepository : IPartnerRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private const string BosOnline = "BosOnline";

        public PartnerRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<EContract_Monitor_Refactor>> GetContractsByDateAsync(
            string managerCode,
            string fromDate,
            string toDate,
            int    page,
            int    pageSize)
        {
            using var conn = _dbFactory.GetConnection(BosOnline);

            var param = new DynamicParameters();
            param.Add("@CrtUser",      managerCode, DbType.String);
            param.Add("@Frm_date",     fromDate,    DbType.String);
            param.Add("@End_date",     toDate,      DbType.String);
            param.Add("@strSearch",    "",           DbType.String);
            param.Add("@StatusFilter", (int?)null,   DbType.Int32);
            param.Add("@SaleFilter",   "",           DbType.String); // '' = cả team
            param.Add("@Page",         page,         DbType.Int32);
            param.Add("@PageSize",     pageSize,     DbType.Int32);

            // SP trả 2 resultsets: RS1=data, RS2=SubEmpl_Root (bỏ qua)
            using var multi = await conn.QueryMultipleAsync(
                "dbo.wspList_EContracts_PagedV26ASM_FIX_1",
                param,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            var data = (await multi.ReadAsync<EContract_Monitor_Refactor>()).ToList();
            if (!multi.IsConsumed)
                await multi.ReadAsync<dynamic>(); 

            return data;
        }

        public async Task<PagedEContractByDateResult> GetContractsByDateOnlyAsync(string fromDate, string toDate, string? strSearch = null, int? statusFilter = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            await using var conn = _dbFactory.GetConnection(BosOnline);

            var parameters = new DynamicParameters();
            parameters.Add("@Frm_date", fromDate, DbType.String);
            parameters.Add("@End_date", toDate, DbType.String);
            parameters.Add("@strSearch", strSearch ?? string.Empty, DbType.String);
            parameters.Add("@StatusFilter", statusFilter, DbType.Int32);
            parameters.Add("@Page", page, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            var rows = (await conn.QueryAsync<EContract_Monitor_Refactor>(
                "dbo.wspList_EContracts_PagedByDate",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120)).AsList();

            return new PagedEContractByDateResult
            {
                TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
                Page = page,
                PageSize = pageSize,
                Data = rows
            };
        }
    }
}
