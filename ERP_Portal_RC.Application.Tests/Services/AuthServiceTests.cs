
using AutoMapper;
using ERP_Portal_RC.Application.DTOs.ChangePassword;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ERP_Portal_RC.Application.Tests.Services
{
    /// <summary>
    /// Unit test cho AuthService - minh họa cách test method có nhiều dependency.
    /// Trọng tâm: ChangePasswordAsync (nhiều nhánh, dễ bắt bug logic).
    ///
    /// Kỹ thuật chính được minh họa:
    ///  1. Mock dependency bằng Moq (IAuthRepository).
    ///  2. Dùng NullLogger cho ILogger (vì logging không phải là hành vi cần assert).
    ///  3. Setup giá trị trả về theo kịch bản (Setup + ReturnsAsync).
    ///  4. Verify: kiểm tra method của dependency có/không được gọi.
    ///  5. It.IsAny&lt;T&gt;() và Times.Once/Never để assert lời gọi.
    /// </summary>
    public class AuthServiceTests
    {
        // Dependency - khai báo sẵn làm field để mọi test dùng chung.
        private readonly Mock<IAuthRepository> _authRepoMock = new();
        private readonly Mock<ICustomStore> _customStoreMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IAccountService> _accountServiceMock = new();
        private readonly ILogger<AuthService> _logger = NullLogger<AuthService>.Instance;

        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _sut = new AuthService(
                _authRepoMock.Object,
                _customStoreMock.Object,
                _tokenServiceMock.Object,
                _mapperMock.Object,
                _accountServiceMock.Object,
                _logger);
        }

        // ────────────────────────────────────────────────────────────────────
        // ChangePasswordAsync
        // ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ChangePasswordAsync_WhenNewPasswordEqualsOldPassword_ShouldReturnFail()
        {
            // Arrange
            var request = new ChangePasswordDto
            {
                LoginName = "alice",
                OldPassword = "same123",
                NewPassword = "same123",
                ConfirmPassword = "same123"
            };

            // Act
            var result = await _sut.ChangePasswordAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("không được trùng");

            // Repository không được gọi vì đã fail ngay bước validate
            _authRepoMock.Verify(
                r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenRepositoryReturns1_ShouldReturnSuccess()
        {
            // Arrange
            var request = new ChangePasswordDto
            {
                LoginName = "alice",
                OldPassword = "old123",
                NewPassword = "new456",
                ConfirmPassword = "new456"
            };

            // Stub repository: trả về 1 = thành công
            _authRepoMock
                .Setup(r => r.ChangePasswordAsync(request.LoginName, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1);

            // Stub sync HR thành công
            _authRepoMock
                .Setup(r => r.SyncPasswordToHRAsync(request.LoginName, It.IsAny<string>()))
                .ReturnsAsync((true, (string?)null));

            // Act
            var result = await _sut.ChangePasswordAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Đổi mật khẩu thành công.");
            result.ExternalSyncWarning.Should().BeNull();

            // Verify: ChangePasswordAsync được gọi đúng 1 lần với đúng loginName
            _authRepoMock.Verify(
                r => r.ChangePasswordAsync(request.LoginName, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

            // Verify: có gọi SyncPasswordToHRAsync
            _authRepoMock.Verify(
                r => r.SyncPasswordToHRAsync(request.LoginName, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenSyncHRFails_ShouldSucceedButWithWarning()
        {
            // Arrange
            var request = new ChangePasswordDto
            {
                LoginName = "alice",
                OldPassword = "old123",
                NewPassword = "new456",
                ConfirmPassword = "new456"
            };

            _authRepoMock
                .Setup(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1);

            _authRepoMock
                .Setup(r => r.SyncPasswordToHRAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, "HR API timeout"));

            // Act
            var result = await _sut.ChangePasswordAsync(request);

            // Assert
            result.Success.Should().BeTrue();                         // vẫn thành công
            result.ExternalSyncWarning.Should().NotBeNull();          // nhưng có warning
            result.ExternalSyncWarning.Should().Contain("HR API timeout");
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenSyncHRThrowsException_ShouldSucceedWithWarning()
        {
            // Arrange
            var request = new ChangePasswordDto
            {
                LoginName = "alice",
                OldPassword = "old123",
                NewPassword = "new456",
                ConfirmPassword = "new456"
            };

            _authRepoMock
                .Setup(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1);

            // Mô phỏng exception khi gọi API HR
            _authRepoMock
                .Setup(r => r.SyncPasswordToHRAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Network down"));

            // Act
            var result = await _sut.ChangePasswordAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.ExternalSyncWarning.Should().Contain("Network down");
        }

        // ────────────────────────────────────────────────────────────────────
        // Theory: test các mã lỗi trả về từ Stored Procedure
        // ────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(0,  "Mật khẩu cũ không đúng.")]
        [InlineData(-1, "Tài khoản không tồn tại.")]
        [InlineData(-2, "Tài khoản đã bị vô hiệu hóa.")]
        [InlineData(99, "Đã xảy ra lỗi không xác định.")]
        public async Task ChangePasswordAsync_WhenRepositoryReturnsErrorCode_ShouldMapToExpectedMessage(
            int repoResult, string expectedMessage)
        {
            // Arrange
            var request = new ChangePasswordDto
            {
                LoginName = "alice",
                OldPassword = "old123",
                NewPassword = "new456",
                ConfirmPassword = "new456"
            };

            _authRepoMock
                .Setup(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(repoResult);

            // Act
            var result = await _sut.ChangePasswordAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be(expectedMessage);

            // Verify: Sync HR KHÔNG được gọi vì đổi mật khẩu DB đã fail
            _authRepoMock.Verify(
                r => r.SyncPasswordToHRAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenRepositoryThrows_ShouldReturnServerErrorMessage()
        {
            // Arrange
            var request = new ChangePasswordDto
            {
                LoginName = "alice",
                OldPassword = "old123",
                NewPassword = "new456",
                ConfirmPassword = "new456"
            };

            _authRepoMock
                .Setup(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SQL error"));

            // Act
            var result = await _sut.ChangePasswordAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Lỗi máy chủ");
        }

        // ────────────────────────────────────────────────────────────────────
        // RevokeTokenAsync - minh họa test method đơn giản với mock
        // ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RevokeTokenAsync_WhenTokenNotFound_ShouldReturnFalse()
        {
            // Arrange
            _authRepoMock
                .Setup(r => r.GetRefreshTokenAsync("missing-token"))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            var result = await _sut.RevokeTokenAsync("missing-token");

            // Assert
            result.Should().BeFalse();
            _authRepoMock.Verify(
                r => r.UpdateRefreshTokenAsync(It.IsAny<RefreshToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RevokeTokenAsync_WhenTokenExists_ShouldMarkRevokedAndReturnTrue()
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = "valid-token",
                IsRevoked = false,
                IsUsed = false
            };

            _authRepoMock
                .Setup(r => r.GetRefreshTokenAsync("valid-token"))
                .ReturnsAsync(token);

            _authRepoMock
                .Setup(r => r.UpdateRefreshTokenAsync(It.IsAny<RefreshToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.RevokeTokenAsync("valid-token");

            // Assert
            result.Should().BeTrue();
            token.IsRevoked.Should().BeTrue(); // state bị thay đổi

            // Verify: token bị gán IsRevoked=true được truyền vào UpdateRefreshTokenAsync
            _authRepoMock.Verify(
                r => r.UpdateRefreshTokenAsync(It.Is<RefreshToken>(t => t.IsRevoked == true)),
                Times.Once);
        }
    }
}
