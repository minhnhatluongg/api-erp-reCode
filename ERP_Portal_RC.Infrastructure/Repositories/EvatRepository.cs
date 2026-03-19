using Dapper;
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
    public class EvatRepository : IEvatRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private const string BosEvat = "BosEVAT";
        public EvatRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _connectionFactory = dbConnectionFactory;
        }
        public async Task<EvatAccountInfo?> GetAccountByTaxcodeAsync(string connectionString, string mst, string? cccd)
        {
            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);

            var p = new DynamicParameters();
            p.Add("@Taxcode", mst);

            p.Add("@CCCD", string.IsNullOrWhiteSpace(cccd) ? null : cccd);

            var result = await conn.QueryFirstOrDefaultAsync("[BosEVAT].[dbo].[GetInfoByTaxcode_V25]",
                p, commandType: CommandType.StoredProcedure);

            if (result == null) return null;

            var row = (IDictionary<string, object>)result;
            return new EvatAccountInfo
            {
                HasAccount = true,
                Mst = mst,
                Cccd = cccd,
                CmpnName = row.ContainsKey("CmpnName") ? row["CmpnName"]?.ToString() : null,
                MerchantId = row.ContainsKey("MerchantID") ? row["MerchantID"]?.ToString() : null
            };
        }
    }
}
