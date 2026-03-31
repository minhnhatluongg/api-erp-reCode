using Dapper;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class SalesHierarchyRepository : ISalesHierarchyRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly HttpClient _hrAccountClient;
        private const string bosHumanRs = "BosHumanResource";
        private const string BosConfigureDb = "BosConfigure";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public SalesHierarchyRepository(IDbConnectionFactory dbConnectionFactory, IHttpClientFactory httpClientFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _hrAccountClient = httpClientFactory.CreateClient("HRAccountClient");
        }

        public async Task<IEnumerable<EmployeeTreeItem>> GetRawSalesTreeAsync(string clnID)
        {
            SqlConnection? sqlConnection = null;
            try
            {
                sqlConnection = _dbConnectionFactory.GetConnection(bosHumanRs);
                return await sqlConnection.QueryAsync<EmployeeTreeItem>(
                    "dbo.frmget_EmplStruct_TeamWorksOfRegion",
                    new { clnID },
                    commandType: CommandType.StoredProcedure
                    );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in {nameof(GetRawSalesTreeAsync)}: {ex.Message}", ex);
            }
            finally
            {
                if (sqlConnection != null && sqlConnection.State == ConnectionState.Open)
                {
                    sqlConnection.Close();
                }
            }
        }

        public async Task<string> RegisterSaleHierarchyAsync(SaleRegistrationModel request, string hardcodedAdminId)
        {
            using var connection = _dbConnectionFactory.GetConnection(bosHumanRs);

            // Sử dụng DynamicParameters để tường minh hóa tham số đầu vào
            var parameters = new DynamicParameters();
            parameters.Add("@FullName", request.FullName);
            parameters.Add("@Email", request.Email);
            parameters.Add("@ManagerEmplID", request.ManagerEmplID);
            parameters.Add("@PsID", request.PsID);
            parameters.Add("@socmnd", request.SoCMND);
            parameters.Add("@CrtUser", hardcodedAdminId);

            return await connection.QueryFirstOrDefaultAsync<string>(
                "wsp_RegisterSaleHierarchy",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<string> CreateERPAccountOnlyAsync(string loginName, string password, string fullName, string email, string emplId)
        {
            using var connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);
            string encryptedPassword = Sha1.Encrypt(password);
            var parameters = new DynamicParameters();

            parameters.Add("@App", "EContract", DbType.String);
            parameters.Add("@AppLoginCode", emplId, DbType.String); 
            parameters.Add("@AppLoginName", loginName, DbType.String);
            parameters.Add("@AppLoginPassword", encryptedPassword, DbType.String);
            parameters.Add("@AppLoginFullName", fullName, DbType.String);
            parameters.Add("@AppLoginEmail", email, DbType.String);

            return await connection.QueryFirstOrDefaultAsync<string>(
                "bosConfigure.dbo.bosInsertUserOnApp",
                param: parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Dictionary<string, string>> GetLoginNameBatchAsync(IEnumerable<string> userCodes)
        {
            var codes = userCodes.Distinct().ToList();
            if (codes.Count == 0) return new Dictionary<string, string>();
            using var con = _dbConnectionFactory.GetConnection(BosConfigureDb);
            var inClause = string.Join(",", codes.Select(c => $"'{c.Replace("'", "''")}'"));
            var sql = $@"
                SELECT UserCode, LoginName
                FROM   bosConfigure.dbo.BosUser WITH (NOLOCK)
                WHERE  UserCode IN ({inClause})";
            var rows = await con.QueryAsync(sql);

            return rows.ToDictionary(
                r => (string)((IDictionary<string, object>)r)["UserCode"],
                r => ((IDictionary<string, object>)r)["LoginName"]?.ToString() ?? "");
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateHRAccountAsync(
            string fullName, string email, string phone, string username, string password, string winId)
        {
            const string endpoint = "/api/v1/hr/account-requests/submit";

            var payload = new
            {
                full_name = fullName,
                email = email,
                phone = phone ?? "",
                username = username,
                win_pass = Convert.ToBase64String(Encoding.UTF8.GetBytes(password)),
                win_id = winId,
                source_ref = $"ERP-{winId}"
            };

            var httpResponse = await _hrAccountClient.PostAsJsonAsync(endpoint, payload, _jsonOptions);
            var rawJson = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                return (false, $"HTTP {(int)httpResponse.StatusCode}: {rawJson}");
            }

            return (true, null);
        }
    }
}
