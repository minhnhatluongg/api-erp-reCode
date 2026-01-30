namespace ERP_Portal_RC.Application.DTOs
{
    public class LoginRequestDto
    {
        public string LoginName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class RegisterRequestDto
    {
        public string LoginName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? UserCode { get; set; }
        public string? Grp_List { get; set; }
    }

    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }

    public class RefreshTokenRequestDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO cho revoke token
    /// </summary>
    public class RevokeTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// JWT Settings từ appsettings.json
    /// </summary>
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
