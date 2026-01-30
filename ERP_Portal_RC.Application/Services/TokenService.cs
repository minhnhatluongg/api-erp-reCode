using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ERP_Portal_RC.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<TokenService> _logger;
        private readonly JwtSettings _jwtSettings;

        public TokenService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _authRepository = authRepository;
            _logger = logger;

            // Load JWT settings
            _jwtSettings = new JwtSettings
            {
                SecretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey không được cấu hình"),
                Issuer = configuration["Jwt:Issuer"] ?? "ERPPortalAPI",
                Audience = configuration["Jwt:Audience"] ?? "ERPPortalClient",
                AccessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60"),
                RefreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")
            };
        }

        public (string accessToken, string jwtId, DateTime expiresAt) GenerateAccessToken(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            var jwtId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.LoginName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("UserCode", user.UserCode),
                new Claim("FullName", user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId),
                new Claim(JwtRegisteredClaimNames.Sub, user.LoginName),
                new Claim("Grp_List", user.Grp_List), 
                new Claim("CmpnID", user.CmpnID),
                new Claim("DefaultAppSite", user.DefaultAppSite ?? "Bos"),

            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            return (accessToken, jwtId, expiresAt);
        }

        public async Task<string> GenerateAndSaveRefreshTokenAsync(string userId, string jwtId, string? ipAddress, string? userAgent)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = GenerateRandomToken(),
                JwtId = jwtId,
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _authRepository.AddRefreshTokenAsync(refreshToken);
            return refreshToken.Token;
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = validateLifetime,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get principal from token");
                return null;
            }
        }

        public string GenerateRandomToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
