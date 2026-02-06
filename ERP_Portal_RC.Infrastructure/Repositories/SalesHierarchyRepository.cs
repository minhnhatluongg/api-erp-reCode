using Dapper;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class SalesHierarchyRepository : ISalesHierarchyRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string bosHumanRs = "BosHumanResource";
        private const string BosConfigureDb = "BosConfigure";

        public SalesHierarchyRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
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
    }
}
