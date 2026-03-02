using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Interface.ReleaseInvoice.Repo
{
    public class ContractCheckRepository : IContractCheckRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";
        public ContractCheckRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        
        public async Task<List<CheckContract>> CheckContractAsync(string cusTax, string invSign, string invSample)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = "BosOnline..Check_Econtract";
            var parameters = new
            {
                CusTax = cusTax,
                invSign = invSign,
                invSample = invSample
            };

            var result = await conn.QueryAsync<dynamic>(
                sql,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // Mapping thủ công để khớp với logic cũ của bạn (OID -> ContractOID)
            return result.Select(reader => new CheckContract
            {
                ContractOID = reader.OID?.ToString(),
                InvcSample = reader.invcSample?.ToString(),
                InvcSign = reader.InvcSign?.ToString(),
                InvcFrom = reader.InvcFrm?.ToString(),
                InvcEnd = reader.InvcEnd?.ToString(),
                Crt_User = reader.Crt_User?.ToString(),
            }).ToList();
        }
    }
}
