using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Interfaces;

namespace ERP_Portal_RC.Application.Tests.Services
{
    /// <summary>
    /// Unit test cho AccountService.
    /// Mục đích: minh họa cách test một method thuần (không đụng DB/API).
    /// Method được test: ParseApiLoginString(string) - parse chuỗi cấu hình app login.
    ///
    /// Quy ước đặt tên test: [MethodUnderTest]_[Scenario]_[ExpectedResult]
    /// Cấu trúc 1 test: AAA pattern - Arrange / Act / Assert
    ///
    /// LƯU Ý QUAN TRỌNG:
    ///   Method ParseApiLoginString có gọi Sha1.Decrypt(value) cho key "AppLoginPassword".
    ///   Sha1.Decrypt sẽ ném exception nếu value không phải Base64 hợp lệ.
    ///   Exception bị catch swallow ở ngoài → nếu block đang xử lý dở sẽ KHÔNG được add vào dict.
    ///   Do đó các test dưới đây KHÔNG truyền AppLoginPassword trừ khi dùng giá trị đã mã hoá hợp lệ.
    ///   Giá trị hợp lệ được sinh sẵn trong constructor bằng Sha1.Encrypt.
    /// </summary>
    public class AccountServiceTests
    {
        // SUT = System Under Test (thuật ngữ chuẩn trong unit testing)
        private readonly AccountService _sut;

        // Mock các dependency - dù test này không dùng tới nhưng constructor yêu cầu.
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<ICustomStore> _customStoreMock = new();
        private readonly Mock<IConnectionRepository> _connectionRepoMock = new();
        private readonly Mock<IEvatRepository> _evatRepoMock = new();

        // Password đã được mã hoá đúng chuẩn Base64 để Sha1.Decrypt không throw
        private readonly string _encryptedPassword;

        public AccountServiceTests()
        {
            _sut = new AccountService(
                _accountRepoMock.Object,
                _customStoreMock.Object,
                _connectionRepoMock.Object,
                _evatRepoMock.Object);

            // Tạo 1 password đã được encrypt hợp lệ để dùng chung cho các test cần
            _encryptedPassword = Sha1.Encrypt("secret123");
        }

        // ────────────────────────────────────────────────────────────────────
        // Fact = 1 test case cố định, không có tham số
        // ────────────────────────────────────────────────────────────────────

        [Fact]
        public void ParseApiLoginString_WhenInputIsNull_ShouldReturnEmptyDictionary()
        {
            // Arrange
            string? input = null;

            // Act
            var result = _sut.ParseApiLoginString(input!);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseApiLoginString_WhenInputIsEmpty_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseApiLoginString_WhenInputHasOneApp_ShouldParseCorrectly()
        {
            // Arrange - KHÔNG truyền AppLoginPassword để tránh Sha1.Decrypt throw
            var input = "App=WINBOS;AppUrl=http://bos.local;AppLoginName=admin";

            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert
            result.Should().ContainKey("WINBOS");
            result["WINBOS"].AppName.Should().Be("WINBOS");
            result["WINBOS"].AppUrl.Should().Be("http://bos.local");
            result["WINBOS"].LoginName.Should().Be("admin");
            result["WINBOS"].IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void ParseApiLoginString_WhenPasswordIsValidBase64_ShouldStoreRawValue()
        {
            // Arrange - dùng password đã mã hoá Base64 hợp lệ
            var input = $"App=WINBOS;AppLoginName=admin;AppLoginPassword={_encryptedPassword}";

            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert
            result.Should().ContainKey("WINBOS");
            // Lưu ý: code gán Password = Sha1.Decrypt(value) rồi OVERRIDE = value,
            // nên giá trị cuối vẫn là chuỗi Base64 nguyên bản.
            result["WINBOS"].Password.Should().Be(_encryptedPassword);
        }

        [Fact]
        public void ParseApiLoginString_WhenInputHasMultipleApps_ShouldParseAll()
        {
            // Arrange - 3 app cách nhau bởi dấu "|", KHÔNG có AppLoginPassword
            var input =
                "App=WINBOS;AppUrl=http://bos.local;AppLoginName=u1|" +
                "App=WINECONTRACT;AppUrl=http://ec.local;AppLoginName=u2|" +
                "App=WININVOICE;AppUrl=http://inv.local;AppLoginName=u3";

            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainKeys("WINBOS", "WINECONTRACT", "WININVOICE");
            result["WINECONTRACT"].LoginName.Should().Be("u2");
            result["WININVOICE"].AppUrl.Should().Be("http://inv.local");
        }

        [Fact]
        public void ParseApiLoginString_WhenAppNameMissing_ShouldNotAddEntry()
        {
            // Arrange - block đầu tiên không có "App=..." → bị bỏ qua
            var input = "AppUrl=http://x;AppLoginName=admin|App=WINBOS;AppLoginName=root";

            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey("WINBOS");
            result["WINBOS"].LoginName.Should().Be("root");
        }

        [Fact]
        public void ParseApiLoginString_WhenKeyIsMixedCase_ShouldBeCaseInsensitiveOnAppName()
        {
            // Arrange - Dictionary dùng OrdinalIgnoreCase cho key AppName
            var input = "App=WinBos;AppLoginName=admin";

            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert - có thể truy cập bằng mọi cách viết hoa/thường
            result.Should().ContainKey("WINBOS");
            result.Should().ContainKey("winbos");
            result.Should().ContainKey("WinBos");
        }

        // ────────────────────────────────────────────────────────────────────
        // Theory = 1 test chạy với nhiều bộ tham số (data-driven test)
        // ────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ParseApiLoginString_WhenInputIsNullOrWhitespace_ShouldReturnEmpty(string? input)
        {
            // Act
            var result = _sut.ParseApiLoginString(input!);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("App=WINBOS", "WINBOS")]
        [InlineData("App=WINECONTRACT", "WINECONTRACT")]
        [InlineData("App=WININVOICE", "WININVOICE")]
        public void ParseApiLoginString_WhenOnlyAppKeyPresent_ShouldReturnEntryWithEmptyFields(
            string input, string expectedKey)
        {
            // Act
            var result = _sut.ParseApiLoginString(input);

            // Assert
            result.Should().ContainKey(expectedKey);
            result[expectedKey].AppUrl.Should().BeEmpty();
            result[expectedKey].LoginName.Should().BeEmpty();
        }
    }
}
