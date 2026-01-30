using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IAuthRepository
    {
        /// Thêm refresh token mới vào database
        Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken);

        /// Lấy refresh token theo token string
        Task<RefreshToken?> GetRefreshTokenAsync(string token);

        /// Cập nhật refresh token (đánh dấu used/revoked)
        Task<bool> UpdateRefreshTokenAsync(RefreshToken refreshToken);

        /// Xóa refresh token (logout)
        Task<bool> DeleteRefreshTokenAsync(string token);

        /// Xóa tất cả refresh tokens của một user (logout all devices)
        Task<bool> DeleteAllUserRefreshTokensAsync(string userId);

        /// Lấy danh sách refresh tokens của user
        Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(string userId);

        /// Xóa các token đã hết hạn
        Task<int> CleanupExpiredTokensAsync();
    }
}
