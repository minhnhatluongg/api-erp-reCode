using Dapper;
using ERP_Portal_RC.Domain.Entities.Tax;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class TaxRepository : ITaxRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<TaxRepository> _log;
        private const string BosOnline = "BosOnline";

        public TaxRepository(IDbConnectionFactory dbConnectionFactory, ILogger<TaxRepository> log)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _log = log;
        }

        // ── BosOnline ────────────────────────────────────────────────────────

        public async Task<EContractTaxInfo?> GetEContractInfoByMstAsync(string mst, int loaiCap)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var p = new DynamicParameters();
            p.Add("@custax", mst);
            p.Add("@IsLoaiCap", loaiCap);

            return await conn.QueryFirstOrDefaultAsync<EContractTaxInfo>(
                "BosOnline..Get_Info_byMST_V25",
                p,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<EContractTaxInfo?> GetEContractInfoByOidAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            return await conn.QueryFirstOrDefaultAsync<EContractTaxInfo>(
                "BosOnline..Get_Econtract_ByOID_V25",
                new { OID = oid },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<ContractSummaryRow>> GetOidListByMstAsync(string mst)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var rs = await conn.QueryAsync<ContractSummaryRow>(
                "BosOnline..sp_GetListContractByTaxCode",
                new { TaxCode = mst },
                commandType: CommandType.StoredProcedure);

            return rs;
        }

        public async Task<TaxContractRange?> GetContractRangeAsync(string cusTax, string invSign, string invSample)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            return await conn.QueryFirstOrDefaultAsync<TaxContractRange>(
                "BosOnline..Check_Econtract",
                new
                {
                    CusTax = cusTax,
                    invSign = invSign,
                    invSample = invSample
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<TaxProductRow>> GetEContractDetailByOidAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var rs = await conn.QueryAsync<TaxProductRow>(
                "BosOnline..GetEcontractDetailByOID",
                new { OID = oid },
                commandType: CommandType.StoredProcedure);

            return rs;
        }

        // ── BosEVAT (server theo MST) ────────────────────────────────────────

        public async Task<TaxCmpnInfo?> GetEvatCmpnInfoAsync(string evatConnStr, string taxcode, string cccd)
        {
            if (string.IsNullOrWhiteSpace(evatConnStr)) return null;

            using var conn = new SqlConnection(evatConnStr);

            var p = new DynamicParameters();
            p.Add("@Taxcode", taxcode);
            p.Add("@CCCD", cccd);

            return await conn.QueryFirstOrDefaultAsync<TaxCmpnInfo>(
                "BosEVAT..GetInfoByTaxcode_v25",
                p,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<SampleTT78>> GetSampleTT78Async(string evatConnStr, string taxcode)
        {
            if (string.IsNullOrWhiteSpace(evatConnStr)) return new List<SampleTT78>();

            using var conn = new SqlConnection(evatConnStr);

            var p = new DynamicParameters();
            p.Add("@Taxcode", taxcode);
            p.Add("@TTu", "TT32");
            p.Add("@GetRemnQtty", 0);

            var rs = await conn.QueryAsync<SampleTT78>(
                "BosEVAT..GetSampleIDByTaxCode_TT78_v25",
                p,
                commandType: CommandType.StoredProcedure);

            return rs;
        }

        // ── BosTVAN (server theo MST) ────────────────────────────────────────

        public async Task<bool> CheckConfirmTokhaiAsync(string tvanConnStr, string mst, string cccd)
        {
            if (string.IsNullOrWhiteSpace(tvanConnStr)) return false;

            try
            {
                using var conn = new SqlConnection(tvanConnStr);
                var rs = await conn.QueryAsync<dynamic>(
                    "BosTVAN..CheckConfirmTK_v25",
                    new { MST = mst, CCCD = cccd },
                    commandType: CommandType.StoredProcedure);

                return rs.Any();
            }
            catch (System.Exception ex)
            {
                _log.LogError(ex, "[Tax] CheckConfirmTokhai TVAN lỗi MST={MST}", mst);
                return false;
            }
        }
    }
}
