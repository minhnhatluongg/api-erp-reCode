using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IAuthService
    {
        /// Đăng nhập và tạo JWT tokens
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent);
        /// Refresh access token sử dụng refresh token
        Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, string? userAgent);
        /// Revoke refresh token (logout một device)
        Task<bool> RevokeTokenAsync(string refreshToken);
        /// Revoke tất cả refresh tokens của user (logout all devices)
        Task<bool> RevokeAllUserTokensAsync(string userId);
        /// Validate JWT access token
        Task<bool> ValidateAccessTokenAsync(string accessToken);
        /// Get user từ JWT token
        Task<ApplicationUser?> GetUserFromTokenAsync(string accessToken);
    }
}
