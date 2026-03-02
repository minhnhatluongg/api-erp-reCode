using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
namespace Interface.ReleaseInvoice.Repo
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";

        public TemplateRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<InvoiceTemplate> GetByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = "SELECT * FROM dbo.InvoiceTemplates WHERE TemplateID = @id AND IsActive = 1";

            return await conn.QuerySingleOrDefaultAsync<InvoiceTemplate>(sql, new { id });
        }

        public async Task<IEnumerable<InvoiceTemplate>> GetListAsync(string invoiceType = null)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT TemplateID, TemplateCode, FileName, TemplateName, 
                               InvoiceType, Version, IsActive, CreatedDate
                        FROM dbo.InvoiceTemplates
                        WHERE IsActive = 1
                          AND (@invoiceType IS NULL OR InvoiceType = @invoiceType)
                        ORDER BY TemplateName";

            return await conn.QueryAsync<InvoiceTemplate>(sql, new { invoiceType });
        }

        public async Task<InvoiceTemplate> GetByCodeAsync(string templateCode)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = "SELECT TOP 1 * FROM dbo.InvoiceTemplates WHERE TemplateCode = @templateCode AND IsActive = 1";

            return await conn.QuerySingleOrDefaultAsync<InvoiceTemplate>(sql, new { templateCode });
        }

        public async Task<InvoiceTemplate> GetByFileNameAsync(string fileName)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = "SELECT TOP 1 * FROM dbo.InvoiceTemplates WHERE FileName = @fileName AND IsActive = 1";

            return await conn.QuerySingleOrDefaultAsync<InvoiceTemplate>(sql, new { fileName });
        }

        public async Task<bool> InsertTemplateAsync(InvoiceTemplate model)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"INSERT INTO dbo.InvoiceTemplates
                        (TemplateCode, FileName, TemplateName, InvoiceType,
                         InvoiceContent, Version, IsActive)
                        VALUES
                        (@TemplateCode, @FileName, @TemplateName, @InvoiceType,
                         @InvoiceContent, 1, 1)";

            // model.CreatedBy nếu bạn có dùng thì hãy add thêm vào tham số bên dưới
            int rows = await conn.ExecuteAsync(sql, new
            {
                model.TemplateCode,
                model.FileName,
                model.TemplateName,
                model.InvoiceType,
                model.InvoiceContent
            });

            return rows > 0;
        }

        public async Task<bool> UpdateTemplateContentAsync(int templateId, string zippedBase64)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"UPDATE dbo.InvoiceTemplates
                        SET InvoiceContent = @content,
                            Version = Version + 1
                        WHERE TemplateID = @templateId";

            int rows = await conn.ExecuteAsync(sql, new
            {
                templateId,
                content = zippedBase64
            });

            return rows > 0;
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = "UPDATE dbo.InvoiceTemplates SET IsActive = 0 WHERE TemplateID = @templateId";

            int rows = await conn.ExecuteAsync(sql, new { templateId });
            return rows > 0;
        }
    }
}
