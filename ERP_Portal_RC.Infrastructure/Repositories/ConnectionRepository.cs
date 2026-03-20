using Dapper;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Infrastructure.Repositories
{
    public class ConnectionRepository : IConnectionRepository
    {
        private readonly string _bosConfigureConn;
        private readonly string _server234Conn;

        public ConnectionRepository(IConfiguration configuration)
        {
            _bosConfigureConn = configuration.GetConnectionString("BosConfigure")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:BosConfigure.");

            _server234Conn = configuration.GetConnectionString("Server234")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:Server234.");
        }

        public string GetCnServerByMST(string mst, string? cccd, string system)
        {
            if (string.IsNullOrWhiteSpace(mst)) return string.Empty;

            var row = QueryServerInfoRow(mst, cccd);
            if (row == null) return string.Empty;

            string password = Sha1.Decrypt(row.KeyWork);
            if (string.IsNullOrEmpty(password)) return string.Empty;

            string server = ResolveServerRemote(row, system);
            string catalog = ResolveCatalog(system);

            if (string.IsNullOrEmpty(server)) return string.Empty;

            return BuildConnectionString(server, catalog, password);
        }

        public string GetIPServerByMST(string mst, string? cccd, string system)
        {
            if (string.IsNullOrWhiteSpace(mst)) return string.Empty;

            var row = QueryServerInfoRow(mst, cccd);
            if (row == null) return string.Empty;

            return ResolveServerRemote(row, system);
        }

        public ServerInfoRow? GetServerInfo(string mst, string? cccd)
        {
            if (string.IsNullOrWhiteSpace(mst)) return null;

            var row = QueryServerInfoRow(mst, cccd);
            if (row == null) return null;

            return new ServerInfoRow
            {
                SideServer = row.SideServer,
                SideServerLocal = row.SideServerLocal,
                TVAN = row.TVAN,
                TVANLocal = row.TVANLocal,
                KeyWork = row.KeyWork,
                INVnew = row.INVnew,
                INVnewLocal = row.INVnewLocal,
                ERP = row.ERP, 
                ERPLocal = row.ERPLocal
            };
        }
       
        public string GetConnectionStringServer234() => _server234Conn;
       
        private ServerInfoRow? QueryServerInfoRow(string mst, string? cccd)
        {
            try
            {
                using var con = new SqlConnection(_bosConfigureConn);
                var p = new DynamicParameters();
                p.Add("@MST", mst);

                string cccdValue = !string.IsNullOrWhiteSpace(cccd) ? cccd
                                 : IsValidCCCD(mst) ? mst
                                 : "";

                if (!string.IsNullOrEmpty(cccdValue))
                    p.Add("@CCCD", cccdValue);

                var raw = con.QueryFirstOrDefault(
                    "bosConfigure..bos_ChkServerSidesMST", p,
                    commandType: CommandType.StoredProcedure);

                if (raw == null) return null;

                var d = (IDictionary<string, object>)raw;

                string Get(string key) =>
                    d.ContainsKey(key) ? d[key]?.ToString()?.Trim() ?? "" : "";

                return new ServerInfoRow
                {
                    SideServer = Get("SideServer"),
                    SideServerLocal = Get("SideServerLocal"),
                    KeyWork = Get("keyWork"),
                    INVnew = Get("INVnew"),
                    INVnewLocal = Get("INVnewLocal"),
                    TVAN = Get("TVAN"),
                    TVANLocal = Get("TVANLocal"),
                    ERP = Get("ERP"),
                    ERPLocal = Get("ERPLocal")
                };
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveServerRemote(ServerInfoRow row, string system) =>
            system?.ToUpperInvariant() switch
            {
                "EVAT" => !string.IsNullOrEmpty(row.SideServer) ? row.SideServer : row.INVnew,
                "EVATNEW" => row.INVnew,
                "TVAN" => row.TVAN,
                "ERP" => row.ERP,
                _ => string.Empty
            };

        private static string ResolveCatalog(string system) =>
            system?.ToUpperInvariant() is "TVAN" or "ERP" ? "BosTVAN" : "BosEVAT";

        private static string BuildConnectionString(string server, string catalog, string password) =>
            $"Server={server};" +
            $"Initial Catalog={catalog};" +
            $"Persist Security Info=False;" +
            $"User ID=bosR;" +
            $"Password={password};" +
            $"MultipleActiveResultSets=False;" +
            $"Encrypt=True;" +
            $"TrustServerCertificate=True;" +
            $"Connection Timeout=3600;";

        private static bool IsValidCCCD(string input) =>
            !string.IsNullOrWhiteSpace(input)
            && input.All(char.IsDigit)
            && (input.Length == 9 || input.Length == 12);
    }
}
