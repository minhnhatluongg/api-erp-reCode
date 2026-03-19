using Dapper;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Infrastructure.Repositories
{
    public class ConnectionRepository : IConnectionRepository
    {
        private readonly string _bosConfigureConn;

        public ConnectionRepository(IConfiguration configuration)
        {
            // Lấy trực tiếp từ node ConnectionStrings:BosConfigure
            _bosConfigureConn = configuration.GetConnectionString("BosConfigure") ?? "";
        }

        public string GetCnServerByMST(string mst, string? cccd, string system)
        {
            if (string.IsNullOrWhiteSpace(mst)) return string.Empty;

            var row = GetServerInfoRow(mst, cccd);
            if (row == null) return string.Empty;

            // Ép kiểu an toàn sang Dictionary để check Key
            var rowDict = row as IDictionary<string, object>;
            if (rowDict == null) return string.Empty;

            string serverIp = ResolveServer(row, system);

            // Lấy keyWork và Decrypt
            string encryptedPwd = rowDict.ContainsKey("keyWork") ? rowDict["keyWork"]?.ToString() ?? "" : "";
            string password = Sha1.Decrypt(encryptedPwd);

            // Xác định Catalog
            string catalog = ResolveCatalog(system);

            if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(password))
                return string.Empty;

            // Build Connection String
            return $"Server={serverIp};" +
                   $"Initial Catalog={catalog};" +
                   $"Persist Security Info=False;" +
                   $"User ID=bosR;" +
                   $"Password={password};" +
                   $"MultipleActiveResultSets=False;" +
                   $"Encrypt=True;" +
                   $"TrustServerCertificate=True;" +
                   $"Connection Timeout=30;";
        }

        public string GetIPServerByMST(string mst, string? cccd, string system)
        {
            if (string.IsNullOrWhiteSpace(mst)) return string.Empty;

            var row = GetServerInfoRow(mst, cccd);
            return row == null ? string.Empty : ResolveServer(row, system);
        }

        #region Private Helpers

        private dynamic? GetServerInfoRow(string mst, string? cccd)
        {
            using (var con = new SqlConnection(_bosConfigureConn))
            {
                var p = new DynamicParameters();
                p.Add("@MST", mst);

                if (!string.IsNullOrWhiteSpace(cccd))
                {
                    p.Add("@CCCD", cccd);
                }
                else if (IsValidCCCD(mst))
                {
                    p.Add("@CCCD", mst);
                }

                return con.QueryFirstOrDefault("bosConfigure..bos_ChkServerSidesMST", p,
                    commandType: CommandType.StoredProcedure);
            }
        }

        private string ResolveServer(dynamic row, string system)
        {
            var rowDict = row as IDictionary<string, object>;
            if (rowDict == null) return string.Empty;

            string invNew = rowDict.ContainsKey("INVnew") ? rowDict["INVnew"]?.ToString()?.Trim() ?? "" : "";
            string sideServer = rowDict.ContainsKey("SideServer") ? rowDict["SideServer"]?.ToString()?.Trim() ?? "" : "";
            string tvan = rowDict.ContainsKey("TVAN") ? rowDict["TVAN"]?.ToString()?.Trim() ?? "" : "";
            string erp = rowDict.ContainsKey("ERP") ? rowDict["ERP"]?.ToString()?.Trim() ?? "" : "";

            return (system?.ToUpperInvariant()) switch
            {
                "EVAT" => !string.IsNullOrEmpty(sideServer) ? sideServer : invNew,
                "EVATNEW" => invNew,
                "TVAN" => tvan,
                "ERP" => erp,
                _ => string.Empty
            };
        }

        private string ResolveCatalog(string system)
        {
            string sys = system?.ToUpperInvariant() ?? "";
            return (sys == "TVAN" || sys == "ERP") ? "BosTVAN" : "BosEVAT";
        }

        private bool IsValidCCCD(string input) =>
            !string.IsNullOrWhiteSpace(input) && input.All(char.IsDigit) && (input.Length == 9 || input.Length == 12);

        #endregion
    }
}