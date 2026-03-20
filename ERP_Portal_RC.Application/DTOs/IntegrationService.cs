using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.EntitiesIntergration;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class IntegrationService : IIntegrationService
    {
        private readonly IAccountService _accountService;
        private readonly IConnectionRepository _connectionRepo;
        private readonly ICompanyService _companyService;
        private readonly IEcontractService _contractService;
        private readonly EContractFileLogger _logger;

        public IntegrationService(
            IAccountService accountService,
            IConnectionRepository connectionRepository,
            IEcontractService econtractService,
            EContractFileLogger logger,
            ICompanyService companyService)
        {
            _accountService = accountService;
            _connectionRepo = connectionRepository;
            _companyService = companyService;
            _logger = logger;
            _contractService = econtractService;
        }
        public async Task<ApiResponse<IntegrationResult>> ProcessEContractIntegrationAsync(
    Integration_Incom.EContractIntegrationRequestDto model, string crtUser)
        {
            var oid = model?.OrderOID ?? "Unknown OID";
            await _logger.LogInfoAsync(oid, "Bắt đầu xử lý EContract", new { model?.CusTax, model?.CusName, crtUser });

            try
            {
                // Validate
                if (model == null)
                    return ApiResponse<IntegrationResult>.ErrorResponse("Request không hợp lệ.", 400);
                if (string.IsNullOrWhiteSpace(model.CusTax))
                    return ApiResponse<IntegrationResult>.ErrorResponse("Mã số thuế khách hàng không được để trống.", 400);
                if (string.IsNullOrWhiteSpace(model.CusName))
                    return ApiResponse<IntegrationResult>.ErrorResponse("Tên khách hàng không được để trống.", 400);
                if (string.IsNullOrWhiteSpace(model.OrderOID))
                    return ApiResponse<IntegrationResult>.ErrorResponse("OrderOID không được để trống.", 400);
                if ((model.IsCapBu || model.IsGiaHan) && string.IsNullOrWhiteSpace(model.OidContract))
                    return ApiResponse<IntegrationResult>.ErrorResponse("OidContract không được để trống khi IsCapBu hoặc IsGiaHan.", 400);

                // B0: Fill thông tin công ty chủ quản
                await _logger.LogInfoAsync(oid, "B0: Lấy thông tin công ty chủ quản");
                await FillDefaultCompanyInfoAsync(model);
                model.SaleEmID = crtUser;

                // Tự tính tổng từ details
                model.PrdcAmnt = model.Details?.Sum(d => d.ItemAmnt) ?? 0;
                model.VAT_Amnt = model.Details?.Sum(d => d.VAT_Amnt) ?? 0;
                model.Sum_Amnt = model.Details?.Sum(d => d.Sum_Amnt) ?? 0;
                model.VAT_Rate = model.Details?.FirstOrDefault()?.VAT_Rate ?? 0;

                // B1: Check tài khoản
                await _logger.LogInfoAsync(oid, "B1: Kiểm tra tài khoản", new { model.CusTax });
                var checkAccount = await _accountService.CheckAccountAsync(model.CusTax, null);

                if (checkAccount == null)
                {
                    await _logger.LogErrorAsync(oid, "B1: Kiểm tra tài khoản thất bại");
                    return ApiResponse<IntegrationResult>.ErrorResponse("Không thể kiểm tra tài khoản.", 500);
                }

                string merchantId;
                string customerStatus;

                if (!checkAccount.HasAccount)
                {
                    // ✅ Chưa có account → tiếp tục insert
                    merchantId = string.Empty;
                    customerStatus = "NEW";
                    await _logger.LogInfoAsync(oid, "B1: Công ty chưa có tài khoản, tiếp tục xử lý", new { model.CusTax });
                }
                else
                {
                    // Đã có account → check Sale có HĐ với công ty này không
                    merchantId = checkAccount.MerchantId;
                    await _logger.LogInfoAsync(oid, "B1: Công ty đã có tài khoản, kiểm tra Sale", new { merchantId, crtUser });

                    bool isSaleOwner = await _contractService.CheckOrderBySaleAsync(model.CusTax, crtUser);

                    if (!isSaleOwner)
                    {
                        // ❌ Sale này không có HĐ → thuộc Sale khác
                        await _logger.LogErrorAsync(oid, "B1: Công ty thuộc sở hữu Sale khác", new { model.CusTax, crtUser });
                        return ApiResponse<IntegrationResult>.ErrorResponse(
                            $"Công ty MST '{model.CusTax}' đã thuộc sở hữu của một Sale khác.", 409);
                    }

                    // ✅ Sale này có HĐ → cho tiếp tục
                    customerStatus = "EXISTING";
                    await _logger.LogInfoAsync(oid, "B1: Sale hợp lệ, tiếp tục xử lý", new { merchantId, crtUser });
                }

                // B2: Check duplicate OID
                await _logger.LogInfoAsync(oid, "B2: Kiểm tra duplicate OID", new { model.OrderOID });
                bool isDuplicate = await _contractService.OrderExistsAsync(model.OrderOID);
                if (isDuplicate)
                {
                    await _logger.LogErrorAsync(oid, "B2: OID đã tồn tại", new { model.OrderOID });
                    return ApiResponse<IntegrationResult>.ErrorResponse($"OrderOID '{model.OrderOID}' đã tồn tại.", 409);
                }

                // B3: Insert EContracts + EContractDetails
                await _logger.LogInfoAsync(oid, "B3: Lưu đơn hàng", new { merchantId, model.OrderOID });
                var isSaved = await _contractService.CreateOrderAsync(model, merchantId, model.OrderOID, crtUser);
                if (!isSaved)
                {
                    await _logger.LogErrorAsync(oid, "B3: Lưu đơn hàng thất bại");
                    return ApiResponse<IntegrationResult>.ErrorResponse("Lưu đơn hàng thất bại.", 500);
                }

                var data = new IntegrationResult
                {
                    MerchantId = merchantId,
                    OrderOID = model.OrderOID,
                    TaxCode = model.CusTax,
                    CustomerStatus = customerStatus
                };

                await _logger.LogInfoAsync(oid, "Hoàn thành xử lý EContract", new { merchantId, customerStatus });
                return ApiResponse<IntegrationResult>.SuccessResponse(data, "Tích hợp đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(oid, $"Exception: {ex.Message}", new { ex.StackTrace });
                return ApiResponse<IntegrationResult>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }

        #region Helpers
        private async Task FillDefaultCompanyInfoAsync(EContractIntegrationRequestDto model)
        {
            var companyID = "26";
            var owner = await _contractService.GetOwnerContractAsync(companyID);
            if (!string.IsNullOrWhiteSpace(model.MyCmpnName)) return;
            if (owner == null)
            {
                await _logger.LogErrorAsync(model.OrderOID ?? "N/A", "Không lấy được thông tin công ty chủ quản");
                return;
            }
            model.MyCmpnID = owner.CmpnID;
            model.MyCmpnName = owner.CmpnName;
            model.MyCmpnTax = owner.CmpnTax;
            model.MyCmpnAddress = owner.CmpnAddress;
            model.MyCmpnContactAddress = owner.CmpnContactAddress;
            model.MyCmpnTel = owner.CmpnTel;
            model.MyCmpnMail = owner.CmpnMail;
            model.MyCmpnPeople_Sign = owner.CmpnPeople_Sign;
            model.MyCmpnPosition_Sign = owner.CmpnPosition_BySign;
            model.MyCmpnBankNumber = owner.CmpnBankNumber;     // ← Thêm
            model.MyCmpnBankAddress = owner.CmpnBankAddress;    // ← Thêm
        }
        #endregion
    }
}
