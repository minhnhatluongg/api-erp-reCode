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

        /// Đổi mật khẩu thông qua Stored Procedure
        /// <returns>
        ///  1  = thành công
        ///  0  = mật khẩu cũ không đúng
        /// -1  = user không tồn tại
        /// -2  = tài khoản bị vô hiệu hóa
        /// </returns>
        Task<int> ChangePasswordAsync(string loginName, string hashedOldPassword, string hashedNewPassword);
        /// Lấy thông tin user theo LoginName
        Task<BosUser?> GetByLoginNameAsync(string loginName);

        /// <summary>
        /// Đồng bộ mật khẩu mới sang hệ thống HR bên ngoài.
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> SyncPasswordToHRAsync(string loginName, string encryptedNewPassword);
    }
}
