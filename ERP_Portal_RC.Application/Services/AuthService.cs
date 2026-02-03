using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ERP_Portal_RC.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly ICustomStore _customStore;
        private readonly ITokenService _tokenService;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository,
            ICustomStore customStore,
            ITokenService tokenService,
            IMapper mapper,
            IAccountService accountService,
            ILogger<AuthService> logger)
        {
            _accountService = accountService;
            _authRepository = authRepository;
            _customStore = customStore;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent)
        {
            try
            {
                // Lấy thông tin user từ database
                var users = await _customStore.GetUserByLoginNameAsync(request.LoginName, "00");
                var userOnAp = users.FirstOrDefault();

                if (userOnAp == null)
                {
                    _logger.LogWarning("Login failed: User not found - {LoginName}", request.LoginName);
                    return null;
                }

                // Kiểm tra password - sử dụng SHA1 encryption
                var encryptedPassword = Sha1.Encrypt(request.Password);
                
                // So sánh password
                if (userOnAp.Password != encryptedPassword)
                {
                    _logger.LogWarning("Login failed: Invalid password - {LoginName}", request.LoginName);
                    return null;
                }

                var appConfigs = _accountService.ParseApiLoginString(userOnAp.APIlogin ?? string.Empty);

                //define app default (Bos > Econtract > Invoice)
                string defaultAppSite = "Bos";
                if (appConfigs.ContainsKey("WINBOS"))
                    defaultAppSite = "Bos";
                else if (appConfigs.ContainsKey("WINECONTRACT"))
                    defaultAppSite = "EContract";
                else if (appConfigs.ContainsKey("WININVOICE"))
                    defaultAppSite = "Invoice";

                // Map sang ApplicationUser
                var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        LoginName = userOnAp.LoginName ?? string.Empty,
                        UserCode = userOnAp.UserCode,
                        FullName = userOnAp.FullName ?? string.Empty,
                        UserName = userOnAp.LoginName ?? string.Empty,
                        Email = $"{userOnAp.Email}", 
                        Password = userOnAp.Password ?? string.Empty,
                        Grp_List = userOnAp.Grp_List ?? string.Empty,
                        LanguageDefault = userOnAp.LanguageDefault ?? "VN",
                        CmpnID = userOnAp.CmpnID_List ?? string.Empty,
                        DefaultAppSite = defaultAppSite
                };

                // Generate tokens sử dụng TokenService
                var (accessToken, jwtId, expiresAt) = _tokenService.GenerateAccessToken(user);
                var refreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(user.Id, jwtId, ipAddress, userAgent);

                var userDto = _mapper.Map<UserDto>(user);
                userDto.DefaultAppSite = defaultAppSite; 

                _logger.LogInformation("User {LoginName} logged in successfully", request.LoginName);

                return new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {LoginName}", request.LoginName);
                throw;
            }
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request, string? ipAddress, string? userAgent)
        {
            try
            {
                var existingCheck = _customStore.ChkUser(request.LoginName);
                if (existingCheck > 0)
                {
                    _logger.LogWarning("Registration failed: User already exists - {LoginName}", request.LoginName);
                    return null;
                }
                var encryptedPassword = Sha1.Encrypt(request.Password);

                var newUser = new ApplicationUser
                {
                    LoginName = request.LoginName,
                    Password = encryptedPassword,
                    FullName = request.FullName,
                    Email = request.Email
                };
                
                var createResult = _customStore.CreateUser(newUser);
                
                if (createResult <= 0)
                {
                    _logger.LogWarning("Registration failed: Cannot create user - {LoginName}", request.LoginName);
                    return null;
                }
                newUser.UserCode = createResult.ToString();
                // Thêm user vào group mặc định
                try
                {
                    _customStore.AddUserToGroup(newUser);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add user to default group: {LoginName}", request.LoginName);
                    // Continue anyway - user đã được tạo
                }

                // Generate tokens
                var (accessToken, jwtId, expiresAt) = _tokenService.GenerateAccessToken(newUser);
                var refreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(newUser.Id, jwtId, ipAddress, userAgent);

                var userDto = _mapper.Map<UserDto>(newUser);

                _logger.LogInformation("User {LoginName} registered successfully", request.LoginName);

                return new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {LoginName}", request.LoginName);
                throw;
            }
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress, string? userAgent)
        {
            try
            {
                // Validate access token (không cần check expiration)
                var principal = _tokenService.GetPrincipalFromToken(request.AccessToken, validateLifetime: false);
                
                if (principal == null)
                {
                    _logger.LogWarning("Refresh token failed: Invalid access token");
                    return null;
                }

                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var jwtId = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                var loginName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var companyId = principal.FindFirst("CmpnID")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtId) || string.IsNullOrEmpty(loginName))
                {
                    _logger.LogWarning("Refresh token failed: Missing claims in access token");
                    return null;
                }

                // Validate refresh token
                var storedRefreshToken = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

                if (storedRefreshToken == null)
                {
                    _logger.LogWarning("Refresh token failed: Token not found");
                    return null;
                }

                if (storedRefreshToken.IsUsed || storedRefreshToken.IsRevoked)
                {
                    _logger.LogWarning("Refresh token failed: Token already used or revoked");
                    return null;
                }

                if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token failed: Token expired");
                    return null;
                }

                if (storedRefreshToken.JwtId != jwtId)
                {
                    _logger.LogWarning("Refresh token failed: Token mismatch");
                    return null;
                }

                // Mark old refresh token as used
                storedRefreshToken.IsUsed = true;
                await _authRepository.UpdateRefreshTokenAsync(storedRefreshToken);

                // Lấy user info từ database
                var users = await _customStore.GetUserByLoginNameAsync(loginName,companyId);
                var userOnAp = users.FirstOrDefault();

                if (userOnAp == null)
                {
                    _logger.LogWarning("Refresh token failed: User not found");
                    return null;
                }

                var user = new ApplicationUser
                {
                    Id = userId,
                    LoginName = userOnAp.LoginName ?? string.Empty,
                    UserCode = userOnAp.UserCode,
                    FullName = userOnAp.FullName ?? string.Empty,
                    UserName = userOnAp.FullName,
                    Email = $"{userOnAp.Email}"
                };

                // Generate new tokens
                var (newAccessToken, newJwtId, expiresAt) = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(user.Id, newJwtId, ipAddress, userAgent);

                var userDto = _mapper.Map<UserDto>(user);

                _logger.LogInformation("Token refreshed successfully for user: {LoginName}", loginName);

                return new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                throw;
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _authRepository.GetRefreshTokenAsync(refreshToken);
                
                if (storedToken == null)
                {
                    return false;
                }

                storedToken.IsRevoked = true;
                return await _authRepository.UpdateRefreshTokenAsync(storedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                throw;
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(string userId)
        {
            try
            {
                return await _authRepository.DeleteAllUserRefreshTokensAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all user tokens for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            return await Task.FromResult(_tokenService.ValidateToken(accessToken));
        }

        public async Task<ApplicationUser?> GetUserFromTokenAsync(string accessToken)
        {
            try
            {
                var principal = _tokenService.GetPrincipalFromToken(accessToken, validateLifetime: true);
                if (principal == null) return null;
                
                var cmpnId = principal.FindFirst("CmpnID")?.Value ?? "00";

                if (principal == null)
                {
                    return null;
                }

                var loginName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                
                if (string.IsNullOrEmpty(loginName))
                {
                    return null;
                }

                var users = await _customStore.GetUserByLoginNameAsync(loginName, cmpnId);
                var userOnAp = users.FirstOrDefault();

                if (userOnAp == null)
                {
                    return null;
                }

                return new ApplicationUser
                {
                    Id = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                    LoginName = userOnAp.LoginName ?? string.Empty,
                    UserCode = userOnAp.UserCode,
                    FullName = userOnAp.FullName ?? string.Empty,
                    UserName = userOnAp.FullName ?? string.Empty,
                    Email = $"{userOnAp.FullName}",
                    DefaultAppSite = userOnAp.AppvSite ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user from token");
                return null;
            }
        }
    }
}
