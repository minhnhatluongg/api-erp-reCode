using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseTestController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<DatabaseTestController> _logger;

        public DatabaseTestController(
            IDbConnectionFactory dbConnectionFactory,
            ILogger<DatabaseTestController> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Test kết nối đến database chỉ với tên database
        /// </summary>
        /// <param name="databaseName">Tên database (VD: BosOnline, BosEVAT, BosAccount...)</param>
        /// <returns>Thông tin kết nối và trạng thái</returns>
        [HttpGet("test-connection/{databaseName}")]
        public IActionResult TestConnection(string databaseName)
        {
            SqlConnection connection = null;
            try
            {
                // Chỉ cần truyền tên database
                connection = _dbConnectionFactory.OpenConnection(databaseName);

                var result = new
                {
                    Success = true,
                    Message = $"Kết nối thành công đến database: {databaseName}",
                    DatabaseName = connection.Database,
                    ServerVersion = connection.ServerVersion,
                    State = connection.State.ToString()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kết nối đến database: {databaseName}");
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Không thể kết nối đến database: {databaseName}",
                    Error = ex.Message
                });
            }
            finally
            {
                // Đóng kết nối
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Thực hiện query đơn giản
        /// </summary>
        /// <param name="databaseName">Tên database</param>
        /// <param name="query">Câu query (mặc định: SELECT @@VERSION)</param>
        /// <returns>Kết quả query</returns>
        [HttpGet("query/{databaseName}")]
        public IActionResult ExecuteQuery(string databaseName, [FromQuery] string query = "SELECT @@VERSION AS Version")
        {
            SqlConnection connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(databaseName);

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            results.Add(row);
                        }

                        return Ok(new
                        {
                            Success = true,
                            DatabaseName = databaseName,
                            Query = query,
                            RowCount = results.Count,
                            Data = results
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi thực hiện query trên database: {databaseName}");
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Lỗi khi thực hiện query",
                    Error = ex.Message
                });
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả databases đã cấu hình
        /// </summary>
        [HttpGet("available-databases")]
        public IActionResult GetAvailableDatabases()
        {
            var databases = new List<string>
            {
                "BosAccount",
                "BosApproval",
                "BosAsset",
                "BosCataloge",
                "BosConfigure",
                "BosDocument",
                "BosEVAT",
                "BosHumanResource",
                "BosInfo",
                "BosInventory",
                "BosManufacture",
                "BosOnline",
                "BosSales",
                "BosSupply",
                "BosWarehouseData",
                "BosEVATExt",
                "Bos235"
            };

            return Ok(new
            {
                Success = true,
                Count = databases.Count,
                Databases = databases
            });
        }
    }
}
