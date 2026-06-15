using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Application.Tests.Services
{
    /// <summary>
    /// Unit test cho IntegrationService.ProcessEContractIntegrationAsync
    /// (luồng API POST /api/integration/incom/econtract).
    ///
    /// Bao phủ các nhánh nghiệp vụ:
    ///   - Validate input (null / thiếu CusTax / CusName / OrderOID / OidContract khi cấp bù-gia hạn).
    ///   - B1 Check account: null → 500; chưa có TK → NEW; đã có TK + Sale khác → 409; cùng Sale → EXISTING.
    ///   - B2 Duplicate OID → 409.
    ///   - B3 Insert fail → 500; thành công → 200.
    ///   - Side-effect: set SaleEmID = crtUser, fill Bên B từ Owner, tự tính tổng từ Details.
    ///   - Exception trong dependency → 500 (catch tổng).
    ///
    /// LƯU Ý: EContractFileLogger là class cụ thể (không mock được) → dùng thật, log vào temp.
    /// </summary>
    public class IntegrationServiceTests
    {
        private readonly Mock<IAccountService> _accountServiceMock = new();
        private readonly Mock<IEcontractService> _econtractServiceMock = new();
        private readonly Mock<IConnectionRepository> _connectionRepoMock = new();  // ctor cần, method không dùng
        private readonly Mock<ICompanyService> _companyServiceMock = new();        // ctor cần, method không dùng

        // Bắt lại model truyền vào CreateOrderAsync để soi side-effect.
        private EContractIntegrationRequestDto? _capturedModel;

        private readonly IntegrationService _sut;

        public IntegrationServiceTests()
        {
            // ── Happy path mặc định = khách NEW (chưa có tài khoản) ──
            _accountServiceMock
                .Setup(a => a.CheckAccountAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(new EvatAccountInfo { HasAccount = false });

            _econtractServiceMock
                .Setup(s => s.GetOwnerContractAsync(It.IsAny<string>()))
                .ReturnsAsync(BuildOwner());

            _econtractServiceMock
                .Setup(s => s.OrderExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _econtractServiceMock
                .Setup(s => s.CreateOrderAsync(
                    It.IsAny<EContractIntegrationRequestDto>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<EContractIntegrationRequestDto, string, string, string>(
                    (m, _, _, _) => _capturedModel = m)
                .ReturnsAsync(true);

            _sut = new IntegrationService(
                _accountServiceMock.Object,
                _connectionRepoMock.Object,
                _econtractServiceMock.Object,
                BuildLogger(),
                _companyServiceMock.Object);
        }

        // ════════════════════════════════════════════════════════════════════
        // Validate input
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Process_WhenModelNull_ShouldReturn400()
        {
            var result = await _sut.ProcessEContractIntegrationAsync(null!, "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Theory]
        [InlineData(null, "KH A", "OID1")]   // thiếu CusTax
        [InlineData("0312", null, "OID1")]   // thiếu CusName
        [InlineData("0312", "KH A", null)]   // thiếu OrderOID
        public async Task Process_WhenRequiredFieldMissing_ShouldReturn400(
            string? cusTax, string? cusName, string? orderOid)
        {
            var model = ValidRequest();
            model.CusTax = cusTax!;
            model.CusName = cusName!;
            model.OrderOID = orderOid!;

            var result = await _sut.ProcessEContractIntegrationAsync(model, "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Process_WhenIsCapBuButNoOidContract_ShouldReturn400()
        {
            var model = ValidRequest();
            model.IsCapBu = true;
            model.OidContract = null;

            var result = await _sut.ProcessEContractIntegrationAsync(model, "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Process_WhenIsGiaHanButNoOidContract_ShouldReturn400()
        {
            var model = ValidRequest();
            model.IsGiaHan = true;
            model.OidContract = "";

            var result = await _sut.ProcessEContractIntegrationAsync(model, "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        // ════════════════════════════════════════════════════════════════════
        // B1 — Check account
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Process_WhenCheckAccountReturnsNull_ShouldReturn500()
        {
            _accountServiceMock
                .Setup(a => a.CheckAccountAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync((EvatAccountInfo)null!);

            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Process_WhenNewCustomer_ShouldReturn200_StatusNew_EmptyMerchant()
        {
            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.CustomerStatus.Should().Be("NEW");
            result.Data.MerchantId.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_WhenExistingCustomerOwnedByAnotherSale_ShouldReturn409()
        {
            // Arrange — đã có TK, nhưng Sale hiện tại KHÔNG sở hữu HĐ
            _accountServiceMock
                .Setup(a => a.CheckAccountAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(new EvatAccountInfo { HasAccount = true, MerchantId = "M001" });
            _econtractServiceMock
                .Setup(s => s.CheckOrderBySaleAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            // Không được lưu đơn
            _econtractServiceMock.Verify(s => s.CreateOrderAsync(
                It.IsAny<EContractIntegrationRequestDto>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Process_WhenExistingCustomerSameSale_ShouldReturn200_StatusExisting_WithMerchant()
        {
            _accountServiceMock
                .Setup(a => a.CheckAccountAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(new EvatAccountInfo { HasAccount = true, MerchantId = "M001" });
            _econtractServiceMock
                .Setup(s => s.CheckOrderBySaleAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeTrue();
            result.Data!.CustomerStatus.Should().Be("EXISTING");
            result.Data.MerchantId.Should().Be("M001");
        }

        // ════════════════════════════════════════════════════════════════════
        // B2 / B3 — Duplicate & Insert
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Process_WhenDuplicateOrderOID_ShouldReturn409()
        {
            _econtractServiceMock
                .Setup(s => s.OrderExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Process_WhenCreateOrderFails_ShouldReturn500()
        {
            _econtractServiceMock
                .Setup(s => s.CreateOrderAsync(
                    It.IsAny<EContractIntegrationRequestDto>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }

        // ════════════════════════════════════════════════════════════════════
        // Side-effects (capture model)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Process_HappyPath_ShouldSetSaleEmIDAndFillOwner()
        {
            await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "EMP777");

            _capturedModel.Should().NotBeNull();
            _capturedModel!.SaleEmID.Should().Be("EMP777");        // gán = crtUser
            _capturedModel.MyCmpnName.Should().Be("WIN TECH");     // fill từ Owner (Bên B)
            _capturedModel.MyCmpnTax.Should().Be("0312303803");
        }

        [Fact]
        public async Task Process_HappyPath_ShouldComputeTotalsFromDetails()
        {
            var model = ValidRequest();
            model.Details = new List<EContractDetailDTO>
            {
                new() { ItemAmnt = 100m, VAT_Amnt = 8m, Sum_Amnt = 108m, VAT_Rate = 8m },
                new() { ItemAmnt = 200m, VAT_Amnt = 16m, Sum_Amnt = 216m, VAT_Rate = 8m },
            };

            await _sut.ProcessEContractIntegrationAsync(model, "000642");

            _capturedModel!.PrdcAmnt.Should().Be(300m);
            _capturedModel.VAT_Amnt.Should().Be(24m);
            _capturedModel.Sum_Amnt.Should().Be(324m);
            _capturedModel.VAT_Rate.Should().Be(8m);  // lấy VAT_Rate của detail đầu
        }

        // ════════════════════════════════════════════════════════════════════
        // Exception
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Process_WhenDependencyThrows_ShouldReturn500()
        {
            _econtractServiceMock
                .Setup(s => s.GetOwnerContractAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB down"));

            var result = await _sut.ProcessEContractIntegrationAsync(ValidRequest(), "000642");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        private static EContractFileLogger BuildLogger()
        {
            var tempLogPath = Path.Combine(Path.GetTempPath(), "incom-integration-tests");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EContractLogConfig:LogPath"] = tempLogPath,
                    ["EContractLogConfig:RetentionDays"] = "14",
                })
                .Build();
            return new EContractFileLogger(config);
        }

        private static EContractIntegrationRequestDto ValidRequest() => new()
        {
            CusTax = "0312345678",
            CusName = "Công ty TNHH ABC",
            OrderOID = "000642/260611:0001",
            CusAddress = "123 Lê Lợi",
            Details = new List<EContractDetailDTO>
            {
                new() { ItemAmnt = 100m, VAT_Amnt = 8m, Sum_Amnt = 108m, VAT_Rate = 8m }
            }
        };

        private static OwnerContract BuildOwner() => new()
        {
            CmpnID = "26",
            CmpnName = "WIN TECH",
            CmpnTax = "0312303803",
            CmpnAddress = "123 ABC",
            CmpnContactAddress = "123 ABC",
            CmpnTel = "02838222333",
            CmpnMail = "info@win-tech.vn",
            CmpnPeople_Sign = "Nguyen Van A",
            CmpnPosition_BySign = "Giám Đốc",
            CmpnBankNumber = "123456789",
            CmpnBankAddress = "Vietcombank"
        };
    }
}
