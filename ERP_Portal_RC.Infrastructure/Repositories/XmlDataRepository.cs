using Dapper;
using ERP_Portal_RC.Domain.Entities; 
using ERP_Portal_RC.Domain.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class XmlDataRepository : IXmlDataRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";

        public XmlDataRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<InvoiceXMLData> GetByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"
                SELECT TOP 1 * FROM dbo.InvoiceXMLData WITH(NOLOCK) 
                WHERE DataID = @id";

            return await conn.QuerySingleOrDefaultAsync<InvoiceXMLData>(sql, new { id });
        }

        public async Task<InvoiceXMLData> GetByCodeAsync(string code)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"
                SELECT TOP 1 * FROM dbo.InvoiceXMLData WITH(NOLOCK) 
                WHERE DataCode = @code AND IsActive = 1
                ORDER BY CreatedDate DESC";

            return await conn.QuerySingleOrDefaultAsync<InvoiceXMLData>(sql, new { code });
        }

        public async Task<List<InvoiceXMLData>> GetAllAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"
                SELECT * FROM dbo.InvoiceXMLData WITH(NOLOCK) 
                WHERE IsActive = 1
                ORDER BY DataCode";

            var result = await conn.QueryAsync<InvoiceXMLData>(sql);
            return result.ToList();
        }

        public async Task<bool> InsertAsync(InvoiceXMLData model)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"
                INSERT INTO dbo.InvoiceXMLData 
                (DataCode, Description, XmlContent, IsActive, CreatedDate, CreatedBy)
                VALUES 
                (@DataCode, @Description, @XmlContent, @IsActive, GETDATE(), @CreatedBy)";

            var rows = await conn.ExecuteAsync(sql, model);
            return rows > 0;
        }

        public async Task<bool> UpdateAsync(InvoiceXMLData model)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"
                UPDATE dbo.InvoiceXMLData 
                SET Description = @Description,
                    XmlContent = @XmlContent,
                    IsActive = @IsActive
                WHERE DataID = @DataID";

            var rows = await conn.ExecuteAsync(sql, model);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"UPDATE dbo.InvoiceXMLData SET IsActive = 0 WHERE DataID = @id";

            var rows = await conn.ExecuteAsync(sql, new { id });
            return rows > 0;
        }
    }
}