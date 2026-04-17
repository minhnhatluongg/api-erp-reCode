using Dapper;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using ERP_Portal_RC.Domain.Interfaces;
using ERP_Portal_RC.Domain.Interfaces.Accounts_payable;
using Microsoft.AspNetCore.Connections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories.Accounts_payable
{
    public class ServiceTypeRepository : IServiceTypeRepository
    {
        private const string BosOnline = "BosOnline";
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public ServiceTypeRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> CreateAsync(ServiceType entity)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var newId = await connection.ExecuteScalarAsync<int>(
                sql: "sp_ServiceType_Create",
                param: new
                {
                    entity.Code,
                    entity.Name,
                    entity.Description,
                    IsActive = 1,
                    Crt_User = entity.Crt_User,
                    Crt_Date = DateTime.Now
                },
                commandType: System.Data.CommandType.StoredProcedure);
            return newId;
        }

        public async Task<bool> DeactivateAsync(int serviceTypeId)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            // Soft-delete: chỉ set IsActive = 0, ẩn state
            var rowsAffected = await conn.ExecuteAsync(
                sql: @"UPDATE dbo.ServiceType
                       SET    IsActive = 0,
                              ChgeDate = GETDATE()
                       WHERE  ServiceTypeID = @ServiceTypeID",
                param: new { ServiceTypeID = serviceTypeId }
            );

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<ServiceType>> GetAllActiveAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            return await conn.QueryAsync<ServiceType>(
                sql: "sp_ServiceType_GetAll",
                param: new { IsActiveOnly = 1 },     //  1 = chỉ lấy active 
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<ServiceType>> GetAllAsync()
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            return await conn.QueryAsync<ServiceType>(
                sql: "sp_ServiceType_GetAll",
                param: new { IsActiveOnly = 0 },       // 0 = lấy tất cả
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<ServiceType?> GetByIdAsync(int serviceTypeId)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            return await conn.QuerySingleOrDefaultAsync<ServiceType>(
                sql: "sp_ServiceType_GetById",
                param: new { ServiceTypeID = serviceTypeId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> IsCodeExistsAsync(string serviceTypeCode, int? excludeId = null)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            // SP trả về 1 nếu code đã tồn tại, 0 nếu chưa
            var result = await conn.ExecuteScalarAsync<int>(
                sql: @"SELECT COUNT(1)
                       FROM   dbo.ServiceType
                       WHERE  Code     = @Code
                         AND  IsActive = 1
                         AND  (@ExcludeID IS NULL OR ServiceTypeID <> @ExcludeID)",
                param: new { Code = serviceTypeCode, ExcludeID = excludeId }
            );

            return result > 0;
        }

        public async Task<bool> UpdateAsync(ServiceType entity)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var rowsAffected = await conn.ExecuteAsync(
                sql: "sp_ServiceType_Update",
                param: new
                {
                    entity.ServiceTypeID,
                    entity.Code,
                    entity.Name,
                    entity.Description,
                    entity.IsActive,
                    ChgeUser = entity.ChgeUser,
                    ChgeDate = DateTime.Now
                },
                commandType: CommandType.StoredProcedure
            );
            return rowsAffected > 0;
        }
    }
}
