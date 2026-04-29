using Dapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class EContractV26Repository : IEContractV26Repository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";

        public EContractV26Repository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<(IEnumerable<EContract_Monitor> Data, IEnumerable<SubEmpl> SubEmpl, PageMeta Meta)> GetAllPagedAsync(
            string crtUser,
            string frmDate,
            string endDate,
            string? search,
            int? statusFilter,
            string? filterSaleEmID,
            int page,
            int pageSize)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var param = new DynamicParameters();
            param.Add("@CrtUser",        crtUser,       DbType.String);
            param.Add("@Frm_date",       frmDate,       DbType.String);
            param.Add("@End_date",       endDate,       DbType.String);
            param.Add("@strSearch",      search ?? "",  DbType.String);
            param.Add("@StatusFilter",   statusFilter,  DbType.Int32);
            param.Add("@FilterSaleEmID", filterSaleEmID,DbType.String);
            param.Add("@Page",           page,          DbType.Int32);
            param.Add("@PageSize",       pageSize,      DbType.Int32);

            using var multi = await conn.QueryMultipleAsync(
                "wspList_EContracts_PagedV27",
                param,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            // Resultset 1: data trang hiện tại
            var data = await multi.ReadAsync<EContract_Monitor>();

            // Resultset 2: danh sách nhân viên team
            var subEmpl = await multi.ReadAsync<SubEmpl>();

            // Resultset 3: pagination metadata (Page, PageSize, TotalCount, TotalPages)
            var meta = await multi.ReadSingleOrDefaultAsync<PageMeta>()
                       ?? new PageMeta { Page = page, PageSize = pageSize };

            return (data, subEmpl, meta);
        }
    }

}
