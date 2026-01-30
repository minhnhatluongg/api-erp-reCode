using System.Data.SqlClient;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Lấy kết nối SQL theo tên database
        /// </summary>
        /// <param name="databaseName">Tên database (BosAccount, BosApproval, BosOnline, etc.)</param>
        /// <returns>SqlConnection đã sẵn sàng sử dụng</returns>
        SqlConnection GetConnection(string databaseName);

        /// <summary>
        /// Mở kết nối SQL
        /// </summary>
        /// <param name="databaseName">Tên database</param>
        /// <returns>SqlConnection đã được mở</returns>
        SqlConnection OpenConnection(string databaseName);

        /// <summary>
        /// Đóng kết nối SQL
        /// </summary>
        /// <param name="connection">SqlConnection cần đóng</param>
        void CloseConnection(SqlConnection connection);
    }
}
