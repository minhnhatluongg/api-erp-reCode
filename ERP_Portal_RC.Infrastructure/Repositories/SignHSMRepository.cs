using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class SignHSMRepository : ISignHSMRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<SignHSMRepository> _logger;
        private const string BosEvat = "BosEVAT";
        private const string BosControlEVAT = "BosControlEVAT";
        public SignHSMRepository(IDbConnectionFactory dbConnectionFactory, ILogger<SignHSMRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }
        public async Task<string> GetPayloadJsonAsync(string oid)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosEvat);
            const string sql = @"
                SELECT TOP 1 PayloadDataJson
                FROM   [BosEVAT].dbo.EVAT_AppSign_Process WITH (NOLOCK)
                WHERE  OID = @OID
                ORDER BY CreatedDate DESC";

            return await connection.ExecuteScalarAsync<string>(sql, new { OID = oid }) ?? "";
        }

        public async Task<SignHSMResult> SaveSignedXmlAsync(SignHSMEntity entity)
        {
            using var con = _dbConnectionFactory.GetConnection(BosControlEVAT);

            var param = new DynamicParameters();
            param.Add("@OID", entity.OID);
            param.Add("@ODate", entity.ODate.Date, DbType.Date);
            param.Add("@PartyASoCCCD", entity.PartyASoCCCD);
            param.Add("@PartyATaxcode", entity.PartyATaxcode);
            param.Add("@PartyAName", entity.PartyAName);
            param.Add("@PartyBTaxcode", entity.PartyBTaxcode);
            param.Add("@PartyBName", entity.PartyBName);
            param.Add("@ECtrlContentXML", entity.SignedXmlBase64);
            param.Add("@OK", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("@Message", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);

            await con.ExecuteAsync(
                "[BosControlEVAT].[dbo].[Ins_ContractContent_SignedByOdoo_origin]",
                param,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 3600);

            int ok = param.Get<int>("@OK");
            string msg = param.Get<string>("@Message") ?? "";

            _logger.LogInformation(
                "[SignHSM][Repo] SP result — OID={OID} | OK={OK} | Msg={Msg}",
                entity.OID, ok, msg);

            return new SignHSMResult { IsSuccess = ok == 1, Message = msg };
        }

        public async Task UpdateProcessStatusAsync(string oid, int status, string message)
        {
            using var con = _dbConnectionFactory.GetConnection(BosEvat);

            const string sql = @"
                UPDATE [BosEVAT].dbo.EVAT_AppSign_Process
                SET    Status        = @Status,
                       StatusMessage = @Message,
                       CompletedDate = CASE WHEN @Status IN (2, -1) THEN GETDATE() ELSE CompletedDate END
                WHERE  OID = @OID";

            await con.ExecuteAsync(sql, new { OID = oid, Status = status, Message = message });
        }
    }
}
