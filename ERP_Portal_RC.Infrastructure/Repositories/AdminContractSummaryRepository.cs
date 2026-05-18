using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class AdminContractSummaryRepository : IAdminContractSummaryRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private const string BosOnline = "BosOnline";

        public AdminContractSummaryRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<ContractSummaryResponse?> GetSummaryAsync(string oid)
        {
            using var conn = _dbFactory.GetConnection(BosOnline);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.sp_GetEContract_Summary",
                new { OID = oid },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            // RS1: Thông tin hợp đồng
            var contract = (await multi.ReadAsync<ContractInfoEntity>()).FirstOrDefault();
            if (contract == null) return null;

            var signHistory = (await multi.ReadAsync<SignHistoryEntity>()).ToList();
            var jobs        = (await multi.ReadAsync<JobStatusSummaryEntity>()).ToList();
            var tracking    = (await multi.ReadAsync<TrackingEntity>()).ToList();
            var publicInfo  = (await multi.ReadAsync<PublicInfoEntity>()).FirstOrDefault();

            return new ContractSummaryResponse
            {
                Contract    = contract,
                SignHistory = signHistory,
                Jobs        = jobs,
                Tracking    = tracking,
                PublicInfo  = publicInfo
            };
        }
    }
}
