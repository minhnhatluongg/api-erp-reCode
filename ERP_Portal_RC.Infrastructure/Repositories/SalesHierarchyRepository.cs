using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class SalesHierarchyRepository : ISalesHierarchyRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string bosHumanRs = "BosHumanResource";

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
    }
}
