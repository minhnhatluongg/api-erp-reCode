using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _connectionStrings;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            InitializeConnectionStrings();
        }

        private void InitializeConnectionStrings()
        {
            // Đọc connection strings từ appsettings.json
            var connectionStringsSection = _configuration.GetSection("ConnectionStrings");
            
            foreach (var item in connectionStringsSection.GetChildren())
            {
                _connectionStrings[item.Key] = item.Value;
            }
        }

        public SqlConnection GetConnection(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Database name không được để trống", nameof(databaseName));
            }

            if (!_connectionStrings.TryGetValue(databaseName, out var connectionString))
            {
                throw new InvalidOperationException($"Không tìm thấy connection string cho database: {databaseName}");
            }

            return new SqlConnection(connectionString);
        }

        public SqlConnection OpenConnection(string databaseName)
        {
            var connection = GetConnection(databaseName);
            
            try
            {
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                connection?.Dispose();
                throw new InvalidOperationException($"Không thể kết nối đến database: {databaseName}", ex);
            }
        }

        public void CloseConnection(SqlConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            try
            {
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
            catch (Exception ex)
            {
                // Log exception nếu cần
                throw new InvalidOperationException("Lỗi khi đóng kết nối database", ex);
            }
        }
    }
}
