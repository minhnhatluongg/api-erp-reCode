using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    /// <summary>
    /// Triển khai IContractSignRepository.
    /// Chịu trách nhiệm duy nhất: truy cập dữ liệu cho nghiệp vụ ký số hợp đồng.
    ///
    /// Clean Architecture: Infrastructure phụ thuộc Domain, KHÔNG phụ thuộc Application.
    /// Tất cả type dùng ở đây đều là Domain entities / primitives.
    /// </summary>
    public class ContractSignRepository : IContractSignRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IEContractRepository _eContractRepository;

        // Tên key trong appsettings.json ConnectionStrings
        private const string BosEVAT        = "BosEVAT";
        private const string BosControlEVAT = "BosControlEVAT";

        public ContractSignRepository(
            IDbConnectionFactory dbConnectionFactory,
            IEContractRepository eContractRepository)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _eContractRepository = eContractRepository;
        }

        // ─── IsSigned ───────────────────────────────────────────────────────────

        public async Task<(bool IsSigned, string Message)> IsSignedAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosControlEVAT);
            await ((System.Data.Common.DbConnection)conn).OpenAsync();

            const string sql = "SELECT COUNT(1) FROM [dbo].ECtr_PublicInfo WHERE InvcCode = @OID";
            int count = await conn.ExecuteScalarAsync<int>(sql, new { OID = oid });

            return count > 0
                ? (true, "Hợp đồng này đã được ký số trước đó.")
                : (false, "Chưa ký");
        }

        // ─── AppSign Process ────────────────────────────────────────────────────

        public async Task<bool> UpdateSignStatusAsync(string processName, int status, string message)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosEVAT);
            await ((System.Data.Common.DbConnection)conn).OpenAsync();

            const string sql = @"
                UPDATE [dbo].EVAT_AppSign_Process
                SET Status = @Status,
                    StatusMessage = @Message,
                    CompletedDate = CASE WHEN @Status IN (2, -1) THEN GETDATE() ELSE CompletedDate END
                WHERE KeyUID = @ProcessName";

            int rows = await conn.ExecuteAsync(sql, new
            {
                ProcessName = processName,
                Status      = status,
                Message     = message
            });
            return rows > 0;
        }

        public async Task<List<PendingOidItem>> GetPendingOidsByKeyAsync(string keyId)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosEVAT);
            await ((System.Data.Common.DbConnection)conn).OpenAsync();

            // Normalize KeyID: bỏ dấu gạch dưới + gạch ngang, uppercase để match
            const string sql = @"
                SELECT OID
                FROM [dbo].EVAT_AppSign_Process
                WHERE REPLACE(REPLACE(UPPER(KeyUID), '_', ''), '-', '') = @KeyID
                  AND Status = 0";

            var result = await conn.QueryAsync<string>(sql, new { KeyID = keyId });

            return result
                .Select(oid => new PendingOidItem { InvOID = oid })
                .ToList();
        }

        public async Task<string?> GetPayloadByOidAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosEVAT);
            await ((System.Data.Common.DbConnection)conn).OpenAsync();

            const string sql = @"
                SELECT TOP 1 PayloadDataJson
                FROM [dbo].EVAT_AppSign_Process
                WHERE OID = @OID
                ORDER BY CreatedDate DESC";

            return await conn.ExecuteScalarAsync<string?>(sql, new { OID = oid });
        }

        public async Task<bool> SaveSignedXmlAsync(
            string oid,
            string signedXmlBase64,
            DateTime orderDate,
            string partnerVat,
            string partnerName,
            string companyTax,
            string companyName)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosControlEVAT);
            await ((System.Data.Common.DbConnection)conn).OpenAsync();

            var p = new DynamicParameters();
            p.Add("@OID",             oid);
            p.Add("@ODate",           orderDate.Date,   dbType: DbType.Date);
            p.Add("@PartyASoCCCD",    partnerVat,       size: 20);
            p.Add("@PartyATaxcode",   partnerVat,       size: 20);
            p.Add("@PartyAName",      partnerName,      size: 512);
            p.Add("@PartyBTaxcode",   companyTax,       size: 20);
            p.Add("@PartyBName",      companyName,      size: 512);
            p.Add("@ECtrlContentXML", signedXmlBase64,  size: -1);
            p.Add("@OK",      dbType: DbType.Int32,   direction: ParameterDirection.Output);
            p.Add("@Message", dbType: DbType.String,  size: 4000, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "[dbo].[Ins_ContractContent_SignedByOdoo_origin]",
                p,
                commandType: CommandType.StoredProcedure);

            return p.Get<int>("@OK") == 1;
        }

        public async Task UpdateAppSignStatusByOidAsync(string oid, int status, string message)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosEVAT);
            await ((System.Data.Common.DbConnection)conn).OpenAsync();

            const string sql = @"
                UPDATE [dbo].EVAT_AppSign_Process
                SET Status = @Status,
                    StatusMessage = @Message,
                    CompletedDate = GETDATE()
                WHERE OID = @OID";

            await conn.ExecuteAsync(sql, new { OID = oid, Status = status, Message = message });
        }

        // ─── Server Sign ─────────────────────────────────────────────────────────

        public async Task<SignContractResult> SignContractServerAsync(SignContractDomainRequest request)
        {
            // Kiểm tra template có tồn tại không
            var template = await _eContractRepository.GetTemplateByCodeAsync("TT78_EContract");
            if (template == null || string.IsNullOrEmpty(template.XmlContent))
                return new SignContractResult
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy mẫu hợp đồng trong hệ thống."
                };

            // Business rule: không ký lại nếu đã ký rồi
            var (isSigned, signedMsg) = await IsSignedAsync(request.OID);
            if (isSigned)
                return new SignContractResult
                {
                    IsSuccess = false,
                    Message = $"Hợp đồng đã được ký số trước đó. ({signedMsg})"
                };

            return new SignContractResult
            {
                IsSuccess = true,
                Message = "Yêu cầu ký Server đã được tiếp nhận. Vui lòng kiểm tra lại trạng thái."
            };
        }

        public async Task<CheckSignStatusResult> CheckSignStatusServerAsync(string oid)
        {
            var (isSigned, message) = await IsSignedAsync(oid);
            return new CheckSignStatusResult
            {
                Status  = isSigned ? 2 : 0,
                Message = message
            };
        }
    }
}
