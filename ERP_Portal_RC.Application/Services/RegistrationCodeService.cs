using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class RegistrationCodeService : IRegistrationCodeService
    {
        private readonly ITechnicalUserRepository _techRepo;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        public RegistrationCodeService(ITechnicalUserRepository technicalUser, ITokenService tokenService, IMapper mapper)
        {
            _techRepo = technicalUser;
            _tokenService = tokenService;
            _mapper = mapper;
        }
        public async Task<string> GenerateAndSaveCodeAsync(int techUserId)
        {
            string newCode = "WIN" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var codeEntity = new RegistertrationCodes
            {
                Code = newCode,
                CreatedByUserId = techUserId,
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddHours(1),
                IsUsed = false
            };

            await _techRepo.AddRegistrationCodeAsync(codeEntity);
            return newCode;
        }

        public async Task<LoginResponseDto?> LoginAsync(TechnicalLoginRequest request)
        {
            var user = await _techRepo.GetByUserNameAsync(request.Username);

            if (user == null || !user.IsActive || user.PasswordHash != Sha1.Encrypt(request.Password))
            {
                return null;
            }

            var (accessToken, jwtId, expiresAt) = _tokenService.GenerateAccessToken(user);

            var refreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(
                user.Id.ToString(),
                jwtId,
                null,
                null);

            var response = _mapper.Map<LoginResponseDto>(user);
            response.AccessToken = accessToken;
            response.RefreshToken = refreshToken;
            response.ExpiresAt = expiresAt;

            return response;
        }

        public async Task<bool> RegisterAsync(TechnicalRegistrationRequest request)
        {
            var existingUser = await _techRepo.GetByUserNameAsync(request.Username);
            if (existingUser != null) return false;

            var newUser = _mapper.Map<TechnicalUser>(request);

            newUser.PasswordHash = Sha1.Encrypt(request.Password);

            await _techRepo.AddTechnicalUserAsync(newUser);
            return true;
        }

        public async Task<bool> ValidateAndUseCodeAsync(string code, string email)
        {
            var validCode = await _techRepo.GetValidCodeAsync(code);
            if (validCode == null)
                return false;

            validCode.IsUsed = true;
            validCode.UsedAt = DateTime.Now;
            validCode.UsedByEmail = email;

            await _techRepo.UpdateRegistrationCodeAsync(validCode);
            return true;

        }

        public async Task<bool> ValidateCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            var codeEntity = _techRepo.GetValidCodeAsync(code);
            // 1. Phải tồn tại trong DB
            // 2. Chưa bị sử dụng (IsUsed == false)
            // 3. Thời gian hiện tại phải nhỏ hơn thời gian hết hạn (ExpiredAt)
            if (codeEntity == null || codeEntity.Result.IsUsed || codeEntity.Result.ExpiredAt < DateTime.Now)
                return false;
            return true;
        }
    }
}
