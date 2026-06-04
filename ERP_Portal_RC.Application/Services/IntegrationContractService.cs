using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP_Portal_RC.Application.Services
{
    /// <summary>
    /// Implementation cho IIntegrationContractService.
    /// Quy trình tổng quát:
    ///   1) Validate input (MST, Product.ItemID, ký hiệu/số hóa đơn, sale).
    ///   2) Load thông tin Bên B từ Check_OwnerContract (CmpnID=26).
    ///   3) Build EContractDetailItem từ Product (FE đã gửi đủ ItemName/Price/Unit theo schema
    ///      của get-products).
    ///   4) Build ContractPreviewRequest đầy đủ (mapping tương đương payload của save-and-approve).
    ///   5) Ủy quyền cho IEcontractService.ProcessSaveContractAsync — giữ NGUYÊN luồng cũ.
    ///   6) Trả thêm Meta gồm Source / ResolvedProduct để n8n và Google Sheet theo dõi.
    /// </summary>
    public class IntegrationContractService : IIntegrationContractService
    {
        private const string DefaultCmpnID = "26";
        private const string DefaultInvSample = "1";   // theo yêu cầu nghiệp vụ: '1' + InvSign (vd "C26LMN") → "1C26LMN"

        private readonly IEcontractService _econtractService;
        private readonly IEContractRepository _econtractRepository;
        private readonly IConfiguration _configuration;
        private readonly IntegrationContractFileLogger _fileLogger;
        private readonly ILogger<IntegrationContractService> _logger;

        public IntegrationContractService(
            IEcontractService econtractService,
            IEContractRepository econtractRepository,
            IConfiguration configuration,
            IntegrationContractFileLogger fileLogger,
            ILogger<IntegrationContractService> logger)
        {
            _econtractService = econtractService;
            _econtractRepository = econtractRepository;
            _configuration = configuration;
            _fileLogger = fileLogger;
            _logger = logger;
        }

        public async Task<ApiResponse<QuickCreateContractResponse>> QuickCreateAsync(
            QuickCreateContractRequest request,
            string callerUserCode)
        {
            // ── 1. Validate input ─────────────────────────────────────────
            var validationError = ValidateRequest(request);
            if (validationError != null)
                return ApiResponse<QuickCreateContractResponse>.ErrorResponse(validationError, 400);

            var invSample = string.IsNullOrWhiteSpace(request.InvSample) ? DefaultInvSample : request.InvSample!.Trim();
            var invSign = request.InvSign!.Trim();
            var invFrom = request.InvFrom!.Value;
            var invTo = request.InvTo!.Value;

            // ── 2. Bên B — server tự fill ─────────────────────────────────
            OwnerContract owner;
            try
            {
                owner = await _econtractRepository.GetOwnerContractAsync(DefaultCmpnID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QuickCreate: GetOwnerContractAsync FAIL");
                return ApiResponse<QuickCreateContractResponse>.ErrorResponse(
                    "Không lấy được thông tin công ty chủ quản (Bên B).", 500);
            }
            if (owner == null)
            {
                return ApiResponse<QuickCreateContractResponse>.ErrorResponse(
                    $"Không tìm thấy thông tin công ty chủ quản CmpnID={DefaultCmpnID}.", 500);
            }

            // ── 3. Build Detail từ Product ────────────────────────────────
            var product = request.Product!;
            var qtty = (product.Qtty.HasValue && product.Qtty.Value > 0)
                ? product.Qtty.Value
                : (product.ItemPerBox > 0 ? product.ItemPerBox : 1m);

            var unit = !string.IsNullOrWhiteSpace(product.ItemUnitName)
                ? product.ItemUnitName!
                : (!string.IsNullOrWhiteSpace(product.ItemUnit) ? product.ItemUnit! : "Gói");

            var vatRate = string.IsNullOrWhiteSpace(product.VAT_Rate) ? "8" : product.VAT_Rate!.Trim();

            var detail = new EContractDetailItem
            {
                ItemID = product.ItemID.Trim(),
                ItemName = product.ItemName ?? string.Empty,
                Unit = unit,
                Qtty = qtty,
                Price = product.ItemPrice,
                VAT_Rate = vatRate,
                InvcSample = invSample,
                InvcSign = invSign,
                InvcFrm = invFrom,
                InvcEnd = invTo
            };

            // ── 4. Build ContractPreviewRequest ──────────────────────────
            var saleEmId = (request.SaleEmID ?? callerUserCode ?? string.Empty).Trim();
            var orderCode = BuildOrderCode(saleEmId);

            var preview = new ContractPreviewRequest
            {
                OrderCode = orderCode,
                FactorID = "EContract",
                SampleID = invSample,   // theo yêu cầu: SampleID đi cùng InvSample
                EntryID = "EC:001",
                ODate = DateTime.Now,
                SignDate = DateTime.Now,
                SaleEmID = saleEmId,

                // Bên B — server fill từ owner (ghi đè giá trị FE truyền nếu có)
                CmpnID = string.IsNullOrWhiteSpace(owner.CmpnID) ? DefaultCmpnID : owner.CmpnID,
                CmpnName = owner.CmpnName,
                CmpnAddress = owner.CmpnAddress,
                CmpnContactAddress = owner.CmpnContactAddress,
                CmpnTax = owner.CmpnTax,
                CmpnTel = owner.CmpnTel,
                CmpnMail = owner.CmpnMail,
                CmpnPeople_Sign = owner.CmpnPeople_Sign,
                CmpnPosition_Sign = owner.CmpnPosition_BySign,
                CmpnBankAddress = owner.CmpnBankAddress,
                CmpnBankNumber = owner.CmpnBankNumber,

                // Bên A — FE truyền
                PartnerName = request.CusName,
                PartnerVat = (request.CusTax ?? string.Empty).Replace(" ", string.Empty),
                PartnerAddress = request.CusAddress ?? string.Empty,
                PartnerPhone = request.CusTel ?? string.Empty,
                PartnerEmail = request.CusEmail ?? string.Empty,
                PartnerBankNo = request.CusBankNumber ?? string.Empty,
                PartnerBankAddress = request.CusBankAddress ?? string.Empty,
                PartnerWebsite = request.CusWebsite ?? string.Empty,
                PartnerContactName = request.CusPeople_Sign,
                PartnerContactJob = request.CusPosition_BySign ?? "Giám Đốc",

                // Loại đơn
                IsGiaHan = request.IsGiaHan,
                IsCapBu = false,
                IsTT78 = true,
                IsOnline = true,
                OIDContract = request.IsGiaHan ? request.OIDContract : null,
                RefeContractDate = request.IsGiaHan ? (request.RefeContractDate ?? DateTime.Now) : null,

                Descrip = BuildDescription(request),
                HTMLContent = "UE9TVCBPSw==",

                Details = new List<EContractDetailItem> { detail }
            };

            // ── 5. Ủy quyền cho ProcessSaveContractAsync ─────────────────
            ApiResponse<string> saveResult;
            try
            {
                saveResult = await _econtractService.ProcessSaveContractAsync(preview, saleEmId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QuickCreate: ProcessSaveContractAsync EXCEPTION OrderCode={OID}", orderCode);
                return ApiResponse<QuickCreateContractResponse>.ErrorResponse(
                    $"Lỗi khi lưu hợp đồng: {ex.Message}", 500);
            }

            if (!saveResult.Success)
            {
                return ApiResponse<QuickCreateContractResponse>.ErrorResponse(
                    saveResult.Message, saveResult.StatusCode == 0 ? 500 : saveResult.StatusCode);
            }

            // ── 6. Build response ────────────────────────────────────────
            var response = new QuickCreateContractResponse
            {
                OID = saveResult.Data ?? orderCode,
                IsGiaHan = request.IsGiaHan,
                OIDContract = request.IsGiaHan ? request.OIDContract : null,
                Source = request.Source,
                ResolvedProduct = new ResolvedItem
                {
                    ItemID = detail.ItemID,
                    ItemName = detail.ItemName,
                    Unit = detail.Unit,
                    Qtty = detail.Qtty,
                    Price = detail.Price,
                    VAT_Rate = detail.VAT_Rate,
                    InvSample = invSample,
                    InvSign = invSign,
                    InvFrom = invFrom,
                    InvTo = invTo
                }
            };
            if (saveResult.Meta != null)
            {
                if (saveResult.Meta.TryGetValue("accountCreated", out var ac) && ac is bool acb) response.AccountCreated = acb;
                if (saveResult.Meta.TryGetValue("alreadyHadAccount", out var ah) && ah is bool ahb) response.AlreadyHadAccount = ahb;
                if (saveResult.Meta.TryGetValue("jobOid", out var jo)) response.JobOid = jo?.ToString();
            }

            var meta = new Dictionary<string, object>
            {
                ["source"] = request.Source ?? string.Empty,
                ["campaign"] = request.Campaign ?? string.Empty,
                ["customerExternalId"] = request.CustomerExternalID ?? string.Empty,
                ["isGiaHan"] = request.IsGiaHan,
                ["invSampleSign"] = $"{invSample}{invSign}",
                ["accountCreated"] = response.AccountCreated,
                ["alreadyHadAccount"] = response.AlreadyHadAccount,
                ["jobOid"] = response.JobOid ?? string.Empty
            };

            return ApiResponse<QuickCreateContractResponse>.SuccessResponseWithMeta(
                response, meta, saveResult.Message ?? "Tạo hợp đồng thành công.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HỢP ĐỒNG MA (Ghost Contract)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<GhostContractResponse>> CreateGhostContractAsync(
            GhostContractRequest request,
            string callerUserCode)
        {
            const string endpoint = "POST /api/integration/ghost-contract";
            var correlationId = Guid.NewGuid().ToString("N");

            if (request == null)
            {
                await _fileLogger.LogErrorAsync(correlationId, endpoint, "Body request rỗng.");
                return ApiResponse<GhostContractResponse>.ErrorResponse("Body request không được rỗng.", 400);
            }

            await _fileLogger.LogInboundAsync(correlationId, endpoint, "RECEIVED", payload: request,
                message: $"Ghost-contract — Sale={request.SaleEmID}, Source={request.Source}");

            // ── 1. Validate thông tin hóa đơn (cái caller bắt buộc gửi) ──
            string? validationError = null;
            if (string.IsNullOrWhiteSpace(request.InvSign)) validationError = "Thiếu ký hiệu hóa đơn (InvSign).";
            else if (!request.InvFrom.HasValue) validationError = "Thiếu số bắt đầu hóa đơn (InvFrom).";
            else if (!request.InvTo.HasValue) validationError = "Thiếu số kết thúc hóa đơn (InvTo).";
            else if (request.InvFrom.Value > request.InvTo.Value) validationError = "InvFrom phải nhỏ hơn hoặc bằng InvTo.";
            if (validationError != null)
            {
                await _fileLogger.LogErrorAsync(correlationId, endpoint, validationError, request);
                return ApiResponse<GhostContractResponse>.ErrorResponse(validationError, 400);
            }

            // ── 2. Đọc config + resolve giá trị (request > config > token) ──
            var opt = _configuration.GetSection(GhostContractOptions.SectionName).Get<GhostContractOptions>()
                      ?? new GhostContractOptions();

            var invSample = string.IsNullOrWhiteSpace(request.InvSample) ? DefaultInvSample : request.InvSample!.Trim();
            var invSign = request.InvSign!.Trim();
            var invFrom = request.InvFrom.Value;
            var invTo = request.InvTo.Value;

            var saleEmId = FirstNonEmpty(request.SaleEmID, opt.SaleEmID, callerUserCode);
            if (string.IsNullOrWhiteSpace(saleEmId))
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    "Không xác định được SaleEmID (request, GhostContract:SaleEmID và token đều trống).", 400);

            var invoiceItemId = FirstNonEmpty(request.InvoiceItemID, opt.InvoiceItemID);
            var transmissionItemId = FirstNonEmpty(request.TransmissionItemID, opt.TransmissionItemID);
            if (string.IsNullOrWhiteSpace(invoiceItemId) || string.IsNullOrWhiteSpace(transmissionItemId))
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    "Thiếu cấu hình ItemID gói (GhostContract:InvoiceItemID / TransmissionItemID).", 500);

            var customer = request.Customer ?? opt.DefaultCustomer;
            if (customer == null
                || string.IsNullOrWhiteSpace(customer.CusTax)
                || string.IsNullOrWhiteSpace(customer.CusName))
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    "Thiếu thông tin Bên A (Customer.CusTax/CusName) — truyền trong request hoặc cấu hình GhostContract:DefaultCustomer.", 400);

            var cmpnId = string.IsNullOrWhiteSpace(opt.CmpnID) ? DefaultCmpnID : opt.CmpnID.Trim();

            // ── 3. Bên B — server tự fill ──
            OwnerContract owner;
            try
            {
                owner = await _econtractRepository.GetOwnerContractAsync(cmpnId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GhostContract: GetOwnerContractAsync FAIL");
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    "Không lấy được thông tin công ty chủ quản (Bên B).", 500);
            }
            if (owner == null)
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    $"Không tìm thấy thông tin công ty chủ quản CmpnID={cmpnId}.", 500);

            // ── 4. Resolve 2 gói free từ catalog ──
            List<ProductCatalogItem> catalog;
            try
            {
                catalog = (await _econtractRepository.GetProductsCatalogAsync(new ProductCatalogQuery())).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GhostContract: GetProductsCatalogAsync FAIL");
                return ApiResponse<GhostContractResponse>.ErrorResponse("Không lấy được danh mục gói cước.", 500);
            }

            var invoiceItem = catalog.FirstOrDefault(x => string.Equals(x.ItemID?.Trim(), invoiceItemId, StringComparison.OrdinalIgnoreCase));
            var transmissionItem = catalog.FirstOrDefault(x => string.Equals(x.ItemID?.Trim(), transmissionItemId, StringComparison.OrdinalIgnoreCase));
            if (invoiceItem == null)
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    $"Không tìm thấy gói hóa đơn ItemID={invoiceItemId} trong danh mục.", 400);
            if (transmissionItem == null)
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    $"Không tìm thấy gói truyền nhận ItemID={transmissionItemId} trong danh mục.", 400);

            // ── 5. Build 2 Details (đều Price = 0) ──
            // Dòng hóa đơn: mang dải số hóa đơn.
            var invoiceDetail = new EContractDetailItem
            {
                ItemID = invoiceItem.ItemID.Trim(),
                ItemName = invoiceItem.ItemName ?? string.Empty,
                Unit = ResolveUnit(invoiceItem),
                Qtty = invoiceItem.ItemPerBox > 0 ? invoiceItem.ItemPerBox : 1m,
                Price = 0m,
                VAT_Rate = ResolveVat(invoiceItem),
                InvcSample = invSample,
                InvcSign = invSign,
                InvcFrm = invFrom,
                InvcEnd = invTo
            };
            // Dòng truyền nhận: KHÔNG có dải số.
            var transmissionDetail = new EContractDetailItem
            {
                ItemID = transmissionItem.ItemID.Trim(),
                ItemName = transmissionItem.ItemName ?? string.Empty,
                Unit = ResolveUnit(transmissionItem),
                Qtty = transmissionItem.ItemPerBox > 0 ? transmissionItem.ItemPerBox : 1m,
                Price = 0m,
                VAT_Rate = ResolveVat(transmissionItem),
                InvcSample = string.Empty,
                InvcSign = string.Empty,
                InvcFrm = 0,
                InvcEnd = 0
            };

            // ── 6. Build ContractPreviewRequest ──
            var orderCode = BuildOrderCode(saleEmId);
            var preview = new ContractPreviewRequest
            {
                OrderCode = orderCode,
                FactorID = "EContract",
                SampleID = invSample,
                EntryID = "EC:001",
                ODate = DateTime.Now,
                SignDate = DateTime.Now,
                SaleEmID = saleEmId,

                // Bên B — server fill từ owner
                CmpnID = string.IsNullOrWhiteSpace(owner.CmpnID) ? cmpnId : owner.CmpnID,
                CmpnName = owner.CmpnName,
                CmpnAddress = owner.CmpnAddress,
                CmpnContactAddress = owner.CmpnContactAddress,
                CmpnTax = owner.CmpnTax,
                CmpnTel = owner.CmpnTel,
                CmpnMail = owner.CmpnMail,
                CmpnPeople_Sign = owner.CmpnPeople_Sign,
                CmpnPosition_Sign = owner.CmpnPosition_BySign,
                CmpnBankAddress = owner.CmpnBankAddress,
                CmpnBankNumber = owner.CmpnBankNumber,

                // Bên A — từ customer (request hoặc config)
                PartnerName = customer.CusName,
                PartnerVat = (customer.CusTax ?? string.Empty).Replace(" ", string.Empty),
                PartnerAddress = customer.CusAddress ?? string.Empty,
                PartnerPhone = customer.CusTel ?? string.Empty,
                PartnerEmail = customer.CusEmail ?? string.Empty,
                PartnerBankNo = customer.CusBankNumber ?? string.Empty,
                PartnerBankAddress = customer.CusBankAddress ?? string.Empty,
                PartnerWebsite = customer.CusWebsite ?? string.Empty,
                PartnerContactName = customer.CusPeople_Sign ?? string.Empty,
                PartnerContactJob = string.IsNullOrWhiteSpace(customer.CusPosition_BySign) ? "Giám Đốc" : customer.CusPosition_BySign!,

                IsGiaHan = false,
                IsCapBu = false,
                IsTT78 = true,
                IsOnline = true,

                Descrip = BuildGhostDescription(request),
                HTMLContent = "UE9TVCBPSw==",

                Details = new List<EContractDetailItem> { invoiceDetail, transmissionDetail }
            };

            // ── 7. Ủy quyền cho ProcessSaveContractAsync (giữ NGUYÊN luồng cũ) ──
            ApiResponse<string> saveResult;
            try
            {
                saveResult = await _econtractService.ProcessSaveContractAsync(preview, saleEmId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GhostContract: ProcessSaveContractAsync EXCEPTION OrderCode={OID}", orderCode);
                await _fileLogger.LogErrorAsync(correlationId, endpoint,
                    $"ProcessSaveContractAsync EXCEPTION: {ex.Message}", preview);
                return ApiResponse<GhostContractResponse>.ErrorResponse($"Lỗi khi lưu hợp đồng: {ex.Message}", 500);
            }

            if (!saveResult.Success)
            {
                await _fileLogger.LogErrorAsync(correlationId, endpoint,
                    $"ProcessSaveContractAsync FAIL: {saveResult.Message}", preview);
                return ApiResponse<GhostContractResponse>.ErrorResponse(
                    saveResult.Message, saveResult.StatusCode == 0 ? 500 : saveResult.StatusCode);
            }

            // ── 8. Build response ──
            var response = new GhostContractResponse
            {
                OID = saveResult.Data ?? orderCode,
                SaleEmID = saleEmId,
                Items = new List<ResolvedItem>
                {
                    new ResolvedItem
                    {
                        ItemID = invoiceDetail.ItemID,
                        ItemName = invoiceDetail.ItemName,
                        Unit = invoiceDetail.Unit,
                        Qtty = invoiceDetail.Qtty,
                        Price = invoiceDetail.Price,
                        VAT_Rate = invoiceDetail.VAT_Rate,
                        InvSample = invSample,
                        InvSign = invSign,
                        InvFrom = invFrom,
                        InvTo = invTo
                    },
                    new ResolvedItem
                    {
                        ItemID = transmissionDetail.ItemID,
                        ItemName = transmissionDetail.ItemName,
                        Unit = transmissionDetail.Unit,
                        Qtty = transmissionDetail.Qtty,
                        Price = transmissionDetail.Price,
                        VAT_Rate = transmissionDetail.VAT_Rate
                    }
                }
            };
            if (saveResult.Meta != null)
            {
                if (saveResult.Meta.TryGetValue("accountCreated", out var ac) && ac is bool acb) response.AccountCreated = acb;
                if (saveResult.Meta.TryGetValue("alreadyHadAccount", out var ah) && ah is bool ahb) response.AlreadyHadAccount = ahb;
                if (saveResult.Meta.TryGetValue("jobOid", out var jo)) response.JobOid = jo?.ToString();
            }

            var meta = new Dictionary<string, object>
            {
                ["source"] = request.Source ?? string.Empty,
                ["campaign"] = request.Campaign ?? string.Empty,
                ["invSampleSign"] = $"{invSample}{invSign}",
                ["saleEmID"] = saleEmId,
                ["accountCreated"] = response.AccountCreated,
                ["alreadyHadAccount"] = response.AlreadyHadAccount,
                ["jobOid"] = response.JobOid ?? string.Empty
            };

            await _fileLogger.LogInfoAsync(correlationId,
                $"Ghost-contract OK — OID={response.OID}, Sale={saleEmId}, InvSign={invSign}, Range={invFrom}-{invTo}",
                response);

            return ApiResponse<GhostContractResponse>.SuccessResponseWithMeta(
                response, meta, saveResult.Message ?? "Tạo hợp đồng ma thành công.");
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrWhiteSpace(v)) return v!.Trim();
            return string.Empty;
        }

        private static string ResolveUnit(ProductCatalogItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.ItemUnitName)) return item.ItemUnitName!.Trim();
            if (!string.IsNullOrWhiteSpace(item.ItemUnit)) return item.ItemUnit!.Trim();
            return "Gói";
        }

        private static string ResolveVat(ProductCatalogItem item)
            => string.IsNullOrWhiteSpace(item.VAT_Rate) ? "8" : item.VAT_Rate!.Trim();

        private static string BuildGhostDescription(GhostContractRequest r)
        {
            if (!string.IsNullOrWhiteSpace(r.Descrip)) return r.Descrip!;
            var src = string.IsNullOrWhiteSpace(r.Source) ? "GHOST" : r.Source!.Trim();
            return $"Hợp đồng ma từ {src}"
                   + (string.IsNullOrWhiteSpace(r.Campaign) ? "" : $" — Campaign: {r.Campaign}");
        }

        private static string? ValidateRequest(QuickCreateContractRequest r)
        {
            if (r == null) return "Body request không được rỗng.";

            if (string.IsNullOrWhiteSpace(r.CusTax)) return "Thiếu MST khách hàng (CusTax).";
            if (string.IsNullOrWhiteSpace(r.CusName)) return "Thiếu tên khách hàng (CusName).";

            if (r.Product == null || string.IsNullOrWhiteSpace(r.Product.ItemID))
                return "Thiếu thông tin gói cước (Product.ItemID).";
            if (r.Product.ItemPrice < 0)
                return "Đơn giá gói cước (Product.ItemPrice) không hợp lệ.";

            if (string.IsNullOrWhiteSpace(r.InvSign)) return "Thiếu ký hiệu hóa đơn (InvSign).";
            if (!r.InvFrom.HasValue) return "Thiếu số bắt đầu hóa đơn (InvFrom).";
            if (!r.InvTo.HasValue) return "Thiếu số kết thúc hóa đơn (InvTo).";
            if (r.InvFrom.Value > r.InvTo.Value)
                return "InvFrom phải nhỏ hơn hoặc bằng InvTo.";

            if (!r.IsGiaHan && string.IsNullOrWhiteSpace(r.SaleEmID))
                return "Đơn mới yêu cầu SaleEmID.";

            if (r.IsGiaHan && string.IsNullOrWhiteSpace(r.OIDContract))
                return "Đơn gia hạn yêu cầu OIDContract (OID hợp đồng cũ).";

            return null;
        }

        private static string BuildOrderCode(string saleEmId)
        {
            var sale = string.IsNullOrWhiteSpace(saleEmId) ? "AUTO" : saleEmId.Trim();
            return $"{sale}/{DateTime.Now:yyMMdd:HHmmssfff}";
        }

        private static string BuildDescription(QuickCreateContractRequest r)
        {
            if (!string.IsNullOrWhiteSpace(r.Descrip)) return r.Descrip!;
            var src = string.IsNullOrWhiteSpace(r.Source) ? "INTEGRATION" : r.Source!.Trim();
            var prefix = r.IsGiaHan ? "Gia hạn" : "Tạo mới";
            return $"{prefix} từ {src}" + (string.IsNullOrWhiteSpace(r.Campaign) ? "" : $" — Campaign: {r.Campaign}");
        }
    }
}
