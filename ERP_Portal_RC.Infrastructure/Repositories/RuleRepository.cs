using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using ERP_Portal_RC.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Interface.ReleaseInvoice.Repo
{
    public class RuleRepository : IRuleRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";
        public RuleRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task<IEnumerable<InvoiceTemplateRule>> GetAllActiveAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT RuleID, RuleCode, RuleName, RuleContent, 
                           Version, IsActive, CreatedDate
                    FROM dbo.InvoiceTemplateRules
                    WHERE IsActive = 1";

            return await conn.QueryAsync<InvoiceTemplateRule>(sql);
        }

        public async Task<InvoiceTemplateRule> GetByCodeAsync(string code)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT TOP 1 * FROM BosOnline.dbo.InvoiceTemplateRules WHERE RuleCode = @code";

            return await conn.QuerySingleOrDefaultAsync<InvoiceTemplateRule>(sql, new { code });
        }

        public async Task<IEnumerable<InvoiceTemplateRule>> GetAllAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT * FROM dbo.InvoiceTemplateRules 
                    WHERE IsActive = 1 
                    ORDER BY RuleCode";

            return await conn.QueryAsync<InvoiceTemplateRule>(sql);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = "DELETE FROM dbo.InvoiceTemplateRules WHERE RuleID = @id";

            int rows = await conn.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        public async Task<IEnumerable<InvoiceTemplateRule>> GetListAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT * FROM dbo.InvoiceTemplateRules ORDER BY RuleID DESC";

            return await conn.QueryAsync<InvoiceTemplateRule>(sql);
        }

        public async Task<InvoiceTemplateRule> GetByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT TOP 1 * FROM dbo.InvoiceTemplateRules WHERE RuleID = @id";

            return await conn.QuerySingleOrDefaultAsync<InvoiceTemplateRule>(sql, new { id });
        }

        public async Task<bool> InsertAsync(string ruleCode, string ruleName, string contentBase64Gzip)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"INSERT INTO dbo.InvoiceTemplateRules (RuleCode, RuleName, RuleContent, Version, IsActive)
                    VALUES (@RuleCode, @RuleName, @Content, 1, 1)";

            int rows = await conn.ExecuteAsync(sql, new
            {
                RuleCode = ruleCode,
                RuleName = ruleName,
                Content = contentBase64Gzip
            });

            return rows > 0;
        }

        public async Task<bool> UpdateAsync(string ruleCode, string contentBase64Gzip)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"UPDATE dbo.InvoiceTemplateRules
                    SET RuleContent = @Content,
                        Version = Version + 1
                    WHERE RuleCode = @RuleCode";

            int rows = await conn.ExecuteAsync(sql, new
            {
                RuleCode = ruleCode,
                Content = contentBase64Gzip
            });

            return rows > 0;
        }

        public async Task<Dictionary<string, string>> GetAllActiveRulesAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT RuleCode, RuleContent 
                    FROM dbo.InvoiceTemplateRules 
                    WHERE IsActive = 1";

            var list = await conn.QueryAsync<InvoiceTemplateRule>(sql);

            var result = new Dictionary<string, string>();
            foreach (var item in list)
            {
                if (!result.ContainsKey(item.RuleCode))
                {
                    string decodedContent = Decode(item.RuleContent);
                    result.Add(item.RuleCode, decodedContent);
                }
            }
            return result;
        }
        private string Decode(string encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded)) return "";

            try
            {
                var gzipBytes = Convert.FromBase64String(encoded);
                using (var ms = new MemoryStream(gzipBytes))
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                using (var outMs = new MemoryStream())
                {
                    gzip.CopyTo(outMs);
                    var base64 = Encoding.UTF8.GetString(outMs.ToArray());

                    try
                    {
                        var data = Convert.FromBase64String(base64);
                        return Encoding.UTF8.GetString(data);
                    }
                    catch
                    {
                        return base64;
                    }
                }
            }
            catch
            {
                return encoded; 
            }
        }
    }
}
