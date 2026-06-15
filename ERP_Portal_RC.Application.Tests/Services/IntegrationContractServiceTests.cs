using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ERP_Portal_RC.Application.Tests.Services
{
    /// <summary>
    /// Unit test cho IntegrationContractService.CreateGhostContractAsync ("Hợp đồng Ma").
    ///
    /// Trọng tâm minh họa:
    ///   1. Mock IEcontractService + IEContractRepository (Moq).
    ///   2. Tạo IConfiguration in-memory để nạp GhostContractOptions (thay cho appsettings).
    ///   3. Kỹ thuật QUAN TRỌNG: dùng Callback "bắt" ContractPreviewRequest mà service build ra,
    ///      rồi assert nội dung (2 dòng gói, giá 0, dòng hóa đơn có ký hiệu, dòng truyền nhận trống).
    ///   4. Verify số lần gọi ProcessSaveContractAsync.
    ///
    /// LƯU Ý (testability smell):
    ///   IntegrationContractFileLogger là CLASS cụ thể (không phải interface) + method không virtual
    ///   → không mock bằng Moq được. Ở đây ta dùng logger THẬT, trỏ log vào thư mục temp (vô hại).
    ///   Nếu muốn mock sạch: tách interface IIntegrationContractFileLogger hoặc cho method virtual.
    ///
    /// Quy ước tên test: [Method]_[Scenario]_[ExpectedResult]
    /// Cấu trúc: AAA (Arrange / Act / Assert)
    /// </summary>
    public class IntegrationContractServiceTests
    {
        private const string InvoiceItemId = "0036473";       // Cấp bù — dòng mang dải số HĐ
        private const string TransmissionItemId = "2100398";  // Tvan truyền nhận — không số

        private readonly Mock<IEcontractService> _econtractServiceMock = new();
        private readonly Mock<IEContractRepository> _repoMock = new();

        // Bắt lại ContractPreviewRequest mà service truyền vào ProcessSaveContractAsync.
        private ContractPreviewRequest? _capturedPreview;

        private readonly IntegrationContractService _sut;

        public IntegrationContractServiceTests()
        {
            // ── Mock mặc định cho happy path ──────────────────────────────────
            _repoMock
                .Setup(r => r.GetOwnerContractAsync(It.IsAny<string>()))
                .ReturnsAsync(BuildOwner());

            _repoMock
                .Setup(r => r.GetProductsCatalogAsync(It.IsAny<ProductCatalogQuery>()))
                .ReturnsAsync(BuildCatalog());

            _econtractServiceMock
                .Setup(s => s.ProcessSaveContractAsync(It.IsAny<ContractPreviewRequest>(), It.IsAny<string>()))
                .Callback<ContractPreviewRequest, string>((preview, _) => _capturedPreview = preview)
                .ReturnsAsync(ApiResponse<string>.SuccessResponse("OID123", "Tạo hợp đồng thành công."));

            _sut = BuildSut(BuildConfig());
        }

        // ════════════════════════════════════════════════════════════════════
        // Validation — case lỗi
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateGhostContractAsync_WhenInvSignMissing_ShouldReturn400()
        {
            // Arrange
            var req = ValidRequest();
            req.InvSign = null;

            // Act
            var result = await _sut.CreateGhostContractAsync(req, "000642");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            // Không được chạm tới luồng lưu hợp đồng
            _econtractServiceMock.Verify(
                s => s.ProcessSaveContractAsync(It.IsAny<ContractPreviewRequest>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateGhostContractAsync_WhenInvFromGreaterThanInvTo_ShouldReturn400()
        {
            // Arrange
            var req = ValidRequest();
            req.InvFrom = 100;
            req.InvTo = 10;

            // Act
            var result = await _sut.CreateGhostContractAsync(req, "000642");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateGhostContractAsync_WhenNoSaleEmIDAnywhere_ShouldReturn400()
        {
            // Arrange — config không có SaleEmID, token rỗng, request cũng không truyền
            var sut = BuildSut(BuildConfig(saleEmId: ""));
            var req = ValidRequest();
            req.SaleEmID = null;

            // Act
            var result = await sut.CreateGhostContractAsync(req, callerUserCode: "");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        // ════════════════════════════════════════════════════════════════════
        // Dependency trả về bất thường
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateGhostContractAsync_WhenOwnerNotFound_ShouldReturn500()
        {
            // Arrange — Bên B không tồn tại
            _repoMock
                .Setup(r => r.GetOwnerContractAsync(It.IsAny<string>()))
                .ReturnsAsync((OwnerContract)null!);

            // Act
            var result = await _sut.CreateGhostContractAsync(ValidRequest(), "000642");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateGhostContractAsync_WhenInvoiceItemNotInCatalog_ShouldReturnError()
        {
            // Arrange — catalog chỉ có gói truyền nhận, thiếu gói hóa đơn
            _repoMock
                .Setup(r => r.GetProductsCatalogAsync(It.IsAny<ProductCatalogQuery>()))
                .ReturnsAsync(new List<ProductCatalogItem> { BuildItem(TransmissionItemId, "Tvan", 1, "Dịch vụ") });

            // Act
            var result = await _sut.CreateGhostContractAsync(ValidRequest(), "000642");

            // Assert
            result.Success.Should().BeFalse();
            _econtractServiceMock.Verify(
                s => s.ProcessSaveContractAsync(It.IsAny<ContractPreviewRequest>(), It.IsAny<string>()),
                Times.Never);
        }

        // ════════════════════════════════════════════════════════════════════
        // Happy path — trọng tâm: build đúng 2 dòng gói, giá 0
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateGhostContractAsync_HappyPath_ShouldReturnOidAndCallSaveOnce()
        {
            // Act
            var result = await _sut.CreateGhostContractAsync(ValidRequest(), "000642");

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.OID.Should().Be("OID123");
            _econtractServiceMock.Verify(
                s => s.ProcessSaveContractAsync(It.IsAny<ContractPreviewRequest>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateGhostContractAsync_HappyPath_ShouldBuildTwoZeroPriceDetails()
        {
            // Act
            await _sut.CreateGhostContractAsync(ValidRequest(), "000642");

            // Assert — soi cái ContractPreviewRequest đã bắt được
            _capturedPreview.Should().NotBeNull();
            _capturedPreview!.Details.Should().HaveCount(2);
            _capturedPreview.Details.Should().OnlyContain(d => d.Price == 0m);

            // Dòng hóa đơn (Cấp bù): mang dải số HĐ
            var invoice = _capturedPreview.Details.Single(d => d.ItemID == InvoiceItemId);
            invoice.InvcSign.Should().Be("C26LMN");
            invoice.InvcFrm.Should().Be(1);
            invoice.InvcEnd.Should().Be(150);

            // Dòng truyền nhận: KHÔNG có dải số
            var transmission = _capturedPreview.Details.Single(d => d.ItemID == TransmissionItemId);
            transmission.InvcSign.Should().BeNullOrEmpty();
            transmission.InvcFrm.Should().Be(0);
            transmission.InvcEnd.Should().Be(0);
        }

        [Fact]
        public async Task CreateGhostContractAsync_WhenCustomerInRequest_ShouldOverrideConfigDefault()
        {
            // Arrange — request truyền Bên A riêng, phải ghi đè DefaultCustomer trong config
            var req = ValidRequest();
            req.Customer = new GhostContractCustomer
            {
                CusTax = "0999999999",
                CusName = "KH Override"
            };

            // Act
            await _sut.CreateGhostContractAsync(req, "000642");

            // Assert
            _capturedPreview!.PartnerVat.Should().Be("0999999999");
            _capturedPreview.PartnerName.Should().Be("KH Override");
        }

        [Fact]
        public async Task CreateGhostContractAsync_HappyPath_ShouldFillPartyBFromOwner()
        {
            // Act
            await _sut.CreateGhostContractAsync(ValidRequest(), "000642");

            // Assert — Bên B (người bán) server tự fill từ OwnerContract
            _capturedPreview!.CmpnTax.Should().Be("0312303803");
            _capturedPreview.CmpnName.Should().Be("WIN TECH");
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        private IntegrationContractService BuildSut(IConfiguration config)
        {
            var fileLogger = new IntegrationContractFileLogger(config);
            return new IntegrationContractService(
                _econtractServiceMock.Object,
                _repoMock.Object,
                config,
                fileLogger,
                NullLogger<IntegrationContractService>.Instance);
        }

        private static IConfiguration BuildConfig(string? saleEmId = "000642")
        {
            // Log file viết vào thư mục temp để test không làm bẩn repo.
            var tempLogPath = Path.Combine(Path.GetTempPath(), "ghost-contract-tests");

            var dict = new Dictionary<string, string?>
            {
                ["GhostContract:SaleEmID"]                       = saleEmId,
                ["GhostContract:CmpnID"]                         = "26",
                ["GhostContract:InvoiceItemID"]                  = InvoiceItemId,
                ["GhostContract:TransmissionItemID"]             = TransmissionItemId,
                ["GhostContract:DefaultCustomer:CusTax"]         = "0312345678",
                ["GhostContract:DefaultCustomer:CusName"]        = "Cong ty Test",
                ["GhostContract:DefaultCustomer:CusPosition_BySign"] = "Giám Đốc",

                ["ExternalApiLogConfig:LogPath"]                 = tempLogPath,
                ["ExternalApiLogConfig:RetentionDays"]           = "30",
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }

        private static GhostContractRequest ValidRequest() => new()
        {
            InvSample = "1",
            InvSign = "C26LMN",
            InvFrom = 1,
            InvTo = 150,
            Source = "UNIT_TEST"
        };

        private static OwnerContract BuildOwner() => new()
        {
            CmpnID = "26",
            CmpnName = "WIN TECH",
            CmpnAddress = "123 ABC",
            CmpnContactAddress = "123 ABC",
            CmpnTax = "0312303803",
            CmpnTel = "02838222333",
            CmpnMail = "info@win-tech.vn",
            CmpnPeople_Sign = "Nguyen Van A",
            CmpnPosition_BySign = "Giám Đốc",
            CmpnBankNumber = "123456789",
            CmpnBankAddress = "Vietcombank"
        };

        private static List<ProductCatalogItem> BuildCatalog() => new()
        {
            BuildItem(InvoiceItemId, "Cấp bù", 10000, "Gói"),
            BuildItem(TransmissionItemId, "Phí duy trì hệ thống và Tvan truyền nhận HĐĐT", 1, "Dịch vụ"),
            BuildItem("9999999", "Gói không liên quan", 1, "Gói"),
        };

        private static ProductCatalogItem BuildItem(string id, string name, int perBox, string unitName) => new()
        {
            ItemID = id,
            ItemName = name,
            ItemUnit = "Un:0044",
            ItemUnitName = unitName,
            ItemPerBox = perBox,
            ItemPrice = 0m,
            VAT_Rate = "8",
            VAT_Name = "VAT 8%",
            ItemType = "Gói",
            IsRepaire = 0
        };
    }
}
