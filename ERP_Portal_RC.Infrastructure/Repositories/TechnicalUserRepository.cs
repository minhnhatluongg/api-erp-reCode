using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class TechnicalUserRepository : ITechnicalUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnlinedb = "BosOnline";
        public TechnicalUserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task AddRegistrationCodeAsync(RegistertrationCodes code)
        {
            using var connection = _dbConnectionFactory.OpenConnection(BosOnlinedb);
            const string insertQuery = @"
                INSERT INTO RegistrationCodes (Code, CreatedByUserId, CreatedAt, ExpiredAt, IsUsed)
                VALUES (@Code, @CreatedByUserId, @CreatedAt, @ExpiredAt, @IsUsed);";
            await connection.ExecuteAsync(insertQuery, code);
        }

        public async Task<TechnicalUser?> GetByUserNameAsync(string username)
        {
            using var connection = _dbConnectionFactory.OpenConnection(BosOnlinedb);
            const string selectQuery = "SELECT * FROM TechnicalUsers WHERE Username = @Username AND IsActive = 1";
            return await connection.QueryFirstOrDefaultAsync<TechnicalUser>(selectQuery, new { Username = username });
        }

        public async Task<RegistertrationCodes> GetValidCodeAsync(string code)
        {
            using var connection = _dbConnectionFactory.OpenConnection(BosOnlinedb);
            string selectQuery = @"
                SELECT * FROM RegistrationCodes 
                WHERE Code = @Code AND IsUsed = 0 AND ExpiredAt > GETDATE()";
            return await connection.QueryFirstOrDefaultAsync<RegistertrationCodes>(selectQuery, new { Code = code });
        }

        public async Task SaveChangesAsync()
        {
            await Task.CompletedTask;
        }

        public async Task UpdateRegistrationCodeAsync(RegistertrationCodes code)
        {
            using var connection = _dbConnectionFactory.OpenConnection(BosOnlinedb);
            const string sql = @"UPDATE RegistrationCodes 
                             SET IsUsed = @IsUsed, 
                                 UsedByEmail = @UsedByEmail, 
                                 UsedAt = @UsedAt 
                             WHERE Id = @Id";

            await connection.ExecuteAsync(sql, code);
        }

        public async Task AddTechnicalUserAsync(TechnicalUser user)
        {
            using var connection = _dbConnectionFactory.OpenConnection(BosOnlinedb);
            const string insertQuery = @"
                INSERT INTO TechnicalUsers (Username, PasswordHash, FullName, IsActive, CreatedAt)
                VALUES (@Username, @PasswordHash, @FullName, @IsActive, @CreatedAt);";
            await connection.ExecuteAsync(insertQuery, user);
        }
    }
}
