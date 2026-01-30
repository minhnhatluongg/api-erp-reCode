using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnlineDb = "BosOnline";

        public AuthRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = @"
                    INSERT INTO RefreshTokens 
                    (UserId, Token, JwtId, IsUsed, IsRevoked, CreatedAt, ExpiresAt, IpAddress, UserAgent)
                    VALUES 
                    (@UserId, @Token, @JwtId, @IsUsed, @IsRevoked, @CreatedAt, @ExpiresAt, @IpAddress, @UserAgent);
                    
                    SELECT CAST(SCOPE_IDENTITY() as int);
                ";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", refreshToken.UserId);
                command.Parameters.AddWithValue("@Token", refreshToken.Token);
                command.Parameters.AddWithValue("@JwtId", refreshToken.JwtId);
                command.Parameters.AddWithValue("@IsUsed", refreshToken.IsUsed);
                command.Parameters.AddWithValue("@IsRevoked", refreshToken.IsRevoked);
                command.Parameters.AddWithValue("@CreatedAt", refreshToken.CreatedAt);
                command.Parameters.AddWithValue("@ExpiresAt", refreshToken.ExpiresAt);
                command.Parameters.AddWithValue("@IpAddress", (object?)refreshToken.IpAddress ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserAgent", (object?)refreshToken.UserAgent ?? DBNull.Value);

                var id = await command.ExecuteScalarAsync();
                refreshToken.Id = Convert.ToInt32(id);

                return refreshToken;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi khi lưu refresh token", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = @"
                    SELECT Id, UserId, Token, JwtId, IsUsed, IsRevoked, CreatedAt, ExpiresAt, IpAddress, UserAgent
                    FROM RefreshTokens
                    WHERE Token = @Token
                ";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Token", token);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new RefreshToken
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        Token = reader.GetString(reader.GetOrdinal("Token")),
                        JwtId = reader.GetString(reader.GetOrdinal("JwtId")),
                        IsUsed = reader.GetBoolean(reader.GetOrdinal("IsUsed")),
                        IsRevoked = reader.GetBoolean(reader.GetOrdinal("IsRevoked")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                        IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                        UserAgent = reader.IsDBNull(reader.GetOrdinal("UserAgent")) ? null : reader.GetString(reader.GetOrdinal("UserAgent"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi lấy refresh token: {token}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<bool> UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = @"
                    UPDATE RefreshTokens
                    SET IsUsed = @IsUsed, IsRevoked = @IsRevoked
                    WHERE Id = @Id
                ";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IsUsed", refreshToken.IsUsed);
                command.Parameters.AddWithValue("@IsRevoked", refreshToken.IsRevoked);
                command.Parameters.AddWithValue("@Id", refreshToken.Id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi cập nhật refresh token: {refreshToken.Id}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<bool> DeleteRefreshTokenAsync(string token)
        {
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = "DELETE FROM RefreshTokens WHERE Token = @Token";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Token", token);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi xóa refresh token: {token}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<bool> DeleteAllUserRefreshTokensAsync(string userId)
        {
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = "DELETE FROM RefreshTokens WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi xóa tất cả refresh tokens của user: {userId}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(string userId)
        {
            SqlConnection? connection = null;
            var tokens = new List<RefreshToken>();

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = @"
                    SELECT Id, UserId, Token, JwtId, IsUsed, IsRevoked, CreatedAt, ExpiresAt, IpAddress, UserAgent
                    FROM RefreshTokens
                    WHERE UserId = @UserId
                    ORDER BY CreatedAt DESC
                ";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tokens.Add(new RefreshToken
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        Token = reader.GetString(reader.GetOrdinal("Token")),
                        JwtId = reader.GetString(reader.GetOrdinal("JwtId")),
                        IsUsed = reader.GetBoolean(reader.GetOrdinal("IsUsed")),
                        IsRevoked = reader.GetBoolean(reader.GetOrdinal("IsRevoked")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                        IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                        UserAgent = reader.IsDBNull(reader.GetOrdinal("UserAgent")) ? null : reader.GetString(reader.GetOrdinal("UserAgent"))
                    });
                }

                return tokens;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi lấy danh sách refresh tokens của user: {userId}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                var query = "DELETE FROM RefreshTokens WHERE ExpiresAt < @Now";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Now", DateTime.UtcNow);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi khi cleanup expired tokens", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }
    }
}
