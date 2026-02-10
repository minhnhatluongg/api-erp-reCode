using ERP_Portal_RC.Domain.Entities;
using System.Security.Claims;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ITokenService
    {
        (string accessToken, string jwtId, DateTime expiresAt) GenerateAccessToken(ApplicationUser user);
        Task<string> GenerateAndSaveRefreshTokenAsync(string userId, string jwtId, string? ipAddress, string? userAgent);
        bool ValidateToken(string token);
        ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true);
        string GenerateRandomToken();

        //Dành cho Generate Code Login  /get-registration-code
        (string accessToken, string jwtId, DateTime expiresAt) GenerateAccessToken(TechnicalUser user);
    }
}
