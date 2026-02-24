using System.Data.SqlClient;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IDbConnectionFactory
    {
        SqlConnection GetConnection(string databaseName);
        SqlConnection OpenConnection(string databaseName);
        void CloseConnection(SqlConnection connection);
    }
}
