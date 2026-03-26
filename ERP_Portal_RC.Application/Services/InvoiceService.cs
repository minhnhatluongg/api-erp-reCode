using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.EntitiesInvoice;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ERP_Portal_RC.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly EContractFileLogger _fileLogger;
    private readonly IEcontractService _econtractService;

    // ── Config mẫu hóa đơn ───────────────────────────────────────────
    private const string InvNameValue = "1";
    private const string SerialMulti = "C26TAT"; // Đa thuế suất
    private const string SerialSingle = "C26TAA"; // Đơn thuế suất

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        [FromKeyedServices("InvoiceLogger")] EContractFileLogger fileLogger,
        IEcontractService econtractService)
    {
        _invoiceRepository = invoiceRepository;
        _fileLogger = fileLogger;
        _econtractService = econtractService;
    }

    public async Task<CreateInvoiceResultDto> CreateDraftInvoiceAsync(
        CreateInvoiceFromContractDto request,
        CancellationToken cancellationToken = default)
    {
        var contractOid = request.ContractOid;

        // ── Bước 1: Fetch dữ liệu hợp đồng ───────────────────────────
        await _fileLogger.LogInfoAsync(contractOid,
            "BẮT ĐẦU tạo hóa đơn nháp.",
            new { contractOid, invoiceType = request.InvoiceType.ToString() });

        EContractData contractData;
        try
        {
            contractData = await FetchContractAsync(contractOid, cancellationToken);
        }
        catch (Exception ex)
        {
            await _fileLogger.LogErrorAsync(contractOid,
                $"Lỗi khi fetch hợp đồng: {ex.Message}");
            return FailInternal("Không lấy được dữ liệu hợp đồng từ hệ thống.");
        }

        await _fileLogger.LogInfoAsync(contractOid,
            "Fetch hợp đồng thành công.",
            new
            {
                cusName = contractData.EContracts?.CusName,
                cusTax = contractData.EContracts?.CusTax,
                itemCount = contractData.EContractDetails?.Count
            });

        // ── Bước 2: Validate ──────────────────────────────────────────
        var error = Validate(contractData);
        if (error is not null)
        {
            await _fileLogger.LogErrorAsync(contractOid, $"Validate thất bại: {error}");
            return FailInternal(error);
        }

        // ── Bước 3: Mapping payload ───────────────────────────────────
        var payload = BuildPayload(contractData, request.InvoiceType);

        await _fileLogger.LogInfoAsync(contractOid,
            "Mapping payload hoàn tất.",
            new
            {
                invRef = payload.InvRef,
                invName = payload.InvName,
                invSerial = payload.InvSerial,
                invDate = payload.InvDate,
                invVatRate = payload.InvVatRate,
                invSubTotal = payload.InvSubTotal,
                invVatAmount = payload.InvVatAmount,
                invTotalAmount = payload.InvTotalAmount,
                itemCount = payload.Items.Count
            });

        await _fileLogger.LogInfoAsync(contractOid,
            "[REQUEST PAYLOAD] Full payload gửi WinInvoice.", payload);

        // ── Bước 4: Gọi WinInvoice ────────────────────────────────────
        WinInvoiceCreateResponse response;
        try
        {
            response = await _invoiceRepository.CreateInvoiceAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            await _fileLogger.LogErrorAsync(contractOid,
                $"Exception khi gọi WinInvoice: {ex.Message}");
            return FailInternal($"Lỗi kết nối WinInvoice: {ex.Message}");
        }

        // ── Bước 5: Xử lý kết quả ─────────────────────────────────────
        if (!response.IsSuccess)
        {
            await _fileLogger.LogErrorAsync(contractOid,
                $"WinInvoice trả về lỗi. ErrorCode={response.ErrorCode ?? "null"}",
                new { response.ErrorCode, response.ErrorMessage });

            return FailWinInvoice(response.ErrorCode,
                response.ErrorMessage ?? "WinInvoice xử lý thất bại.");
        }

        await _fileLogger.LogInfoAsync(contractOid,
            "Tạo hóa đơn nháp THÀNH CÔNG.",
            new
            {
                winOid = response.Data?.Oid,
                invCode = response.Data?.InvCode,
                invSign = response.Data?.InvSign,
                govCode = response.Data?.GovCode
            });

        return new CreateInvoiceResultDto
        {
            IsSuccess = true,
            Data = MapToDto(response.Data, request.InvoiceType)
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────

    private async Task<EContractData> FetchContractAsync(
        string contractOid,
        CancellationToken cancellationToken)
    {
        var result = await _econtractService.GetContractDetailForDisplayAsync(
            oid: contractOid,
            userCode: string.Empty,
            grpList: string.Empty,
            firstClaimValue: string.Empty);

        if (result is null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng với OID: {contractOid}");

        return new EContractData
        {
            EContracts = result.EContracts is null ? null : new EContracts
            {
                Oid = result.EContracts.OID,
                CusName = result.EContracts.CusName,
                CusPeople_Sign = result.EContracts.CusPeople_Sign,
                CusPosition_BySign = result.EContracts.CusPosition_BySign,
                CusTax = result.EContracts.CusTax,
                CusAddress = result.EContracts.CusAddress,
                CusEmail = result.EContracts.CusEmail,
                CusTel = result.EContracts.CusTel,
                CusBankNumber = result.EContracts.CusBankNumber,
                CusBankAddress = result.EContracts.CusBankAddress,
                ODate = result.EContracts.ODate
            },
            EContractDetails = result.EContractDetails?
                .Select(d => new EContractDetail
                {
                    Oid = d.OID,
                    ItemID = d.ItemID,
                    ItemName = d.ItemName,
                    ItemUnit = d.ItemUnit,
                    ItemUnitName = d.itemUnitName,
                    ItemPrice = d.ItemPrice,
                    ItemQtty = d.ItemQtty,
                    ItemAmnt = d.ItemAmnt,
                    VaT_Rate = d.VAT_Rate,
                    VaT_Amnt = d.VAT_Amnt,
                    Sum_Amnt = d.Sum_Amnt,
                    InvcSign = d.InvcSign,
                    InvcFrm = d.InvcFrm,
                    ItemNo = d.ItemNo,
                    Descrip = d.Descrip,
                    InvcSample = d.InvcSample
                }).ToList()
        };
    }

    private static string? Validate(EContractData data)
    {
        if (data.EContracts is null)
            return "Không có thông tin hợp đồng (eContracts null).";

        if (string.IsNullOrWhiteSpace(data.EContracts.Oid))
            return "OID hợp đồng bị trống.";

        if (data.EContractDetails is null || data.EContractDetails.Count == 0)
            return "Hợp đồng không có dòng sản phẩm/dịch vụ.";

        return null;
    }

    /// <summary>
    /// Mapping EContractData → WinInvoiceCreateRequest.
    ///
    /// InvoiceType.Multi  → InvSerial = C26TAT, invVatRate = -1 (đa thuế suất)
    /// InvoiceType.Single → InvSerial = C26TAA, invVatRate = thuế suất thực của SP
    ///
    /// itemPrice từ ERP đã bao gồm VAT
    ///   → priceNoVat = itemPrice / (1 + vatRate/100)
    /// </summary>
    private static WinInvoiceCreateRequest BuildPayload(
        EContractData data,
        InvoiceType invoiceType)
    {
        var contract = data.EContracts!;
        var details = data.EContractDetails!;

        // Tính toán items (decimal) trước, convert string sau cùng
        var itemMappings = details
            .Select((d, index) => new
            {
                Detail = d,
                FallbackNo = index + 1,
                VatRate = d.VaT_Rate,
                PriceNoVat = d.VaT_Rate > 0
                                ? Math.Round(d.ItemPrice / (1 + d.VaT_Rate / 100m), 0)
                                : d.ItemPrice,
                AmountNoVat = d.ItemPrice > 0 && d.ItemQtty > 0
                                ? (d.VaT_Rate > 0
                                    ? Math.Round(
                                        Math.Round(d.ItemPrice / (1 + d.VaT_Rate / 100m), 0) * d.ItemQtty, 0)
                                    : Math.Round(d.ItemPrice * d.ItemQtty, 0))
                                : d.Sum_Amnt
            })
            .Select(x => new
            {
                x.Detail,
                x.FallbackNo,
                x.VatRate,
                x.PriceNoVat,
                x.AmountNoVat,
                VatAmount = Math.Round(x.AmountNoVat * (x.VatRate / 100m), 0)
            })
            .ToList();

        var invSubTotal = itemMappings.Sum(x => x.AmountNoVat);
        var invVatAmount = itemMappings.Sum(x => x.VatAmount);
        var invTotalAmount = invSubTotal + invVatAmount;

        // invVatRate:
        //   Đa thuế suất (Multi)  → -1
        //   Đơn thuế suất (Single) → lấy thuế suất duy nhất, nếu không có thì 0
        var distinctRates = itemMappings.Select(x => x.VatRate).Distinct().ToList();
        var invVatRate = invoiceType == InvoiceType.Multi
            ? "-1"
            : (distinctRates.Count == 1 ? distinctRates[0].ToString() : "0");

        var invSerial = invoiceType == InvoiceType.Multi ? SerialMulti : SerialSingle;
        var invDate = (contract.ODate ?? DateTime.Now).ToString("yyyy/MM/dd");

        var items = itemMappings.Select(x => new WinInvoiceItem
        {
            ItemNo = (x.Detail.ItemNo > 0 ? x.Detail.ItemNo : x.FallbackNo).ToString(),
            ItemCode = x.Detail.ItemID,
            ItemName = x.Detail.ItemName,
            ItemUnit = !string.IsNullOrWhiteSpace(x.Detail.ItemUnitName)
                                ? x.Detail.ItemUnitName
                                : x.Detail.ItemUnit,
            ItemQuantity = x.Detail.ItemQtty.ToString(),
            ItemPrice = x.PriceNoVat.ToString(),
            ItemVatRate = x.VatRate.ToString(),
            ItemVatAmnt = x.VatAmount.ToString(),
            ItemAmountNoVat = x.AmountNoVat.ToString(),
            ItemNote = x.Detail.Descrip
        }).ToList();

        return new WinInvoiceCreateRequest
        {
            InvAutoSign = "0",
            InvName = InvNameValue,
            InvSerial = invSerial,
            InvNumber = "",
            InvDate = invDate,
            InvRefDate = invDate,
            InvRef = contract.Oid!,
            InvCustomer = "0",
            BuyerName = contract.CusPeople_Sign,
            BuyerCompany = contract.CusName,
            BuyerTax = contract.CusTax,
            BuyerAddress = contract.CusAddress,
            BuyerEmail = contract.CusEmail,
            BuyerPhone = contract.CusTel,
            BuyerAcc = contract.CusBankNumber,
            BuyerBank = contract.CusBankAddress,
            InvSubTotal = invSubTotal.ToString(),
            InvVatRate = invVatRate,
            InvVatAmount = invVatAmount.ToString(),
            InvTotalAmount = invTotalAmount.ToString(),
            InvPayment = "Chuyển khoản",
            InvCurrency = "VND",
            Items = items
        };
    }

    private static InvoiceDraftDataDto? MapToDto(
        WinInvoiceCreateResponseData? src,
        InvoiceType invoiceType)
    {
        if (src is null) return null;

        return new InvoiceDraftDataDto
        {
            Oid = src.Oid,
            InvCode = src.InvCode,
            InvRef = src.InvRef,
            InvSerial = src.InvSign,
            InvName = src.InvName,
            InvDate = src.InvDate,
            GovCode = src.GovCode,
            GovTransfer = src.GovTranfer,
            InvoiceType = invoiceType == InvoiceType.Multi ? "Đa thuế suất" : "Đơn thuế suất"
        };
    }

    private static CreateInvoiceResultDto FailInternal(string message) => new()
    {
        IsSuccess = false,
        ErrorSource = "Internal",
        ErrorMessage = message
    };

    private static CreateInvoiceResultDto FailWinInvoice(string? errorCode, string message) => new()
    {
        IsSuccess = false,
        ErrorSource = "WinInvoice",
        ErrorCode = errorCode,
        ErrorMessage = message
    };
}