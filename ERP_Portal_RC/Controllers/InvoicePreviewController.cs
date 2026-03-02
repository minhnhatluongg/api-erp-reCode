using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using Interface.ReleaseInvoice.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class InvoicePreviewController : ControllerBase
{
    private readonly IInvoicePreviewService _previewService;

    public InvoicePreviewController(IInvoicePreviewService previewService)
    {
        _previewService = previewService;
    }

    [HttpPost("view")]
    public async Task<IActionResult> ViewInvoicePreview([FromBody] PreviewRequestDto input)
    {
        try
        {
            // Gọi hàm hiện tại để lấy HTML
            string htmlContent = await BuildHtmlFromRequest(input);

            return Content(htmlContent, "text/html", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // Trả về HTML lỗi thân thiện
            string errorHtml = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Lỗi</title>
                <style>
                    body {{ font-family: Arial; padding: 40px; background: #f5f5f5; }}
                    .error {{ background: white; padding: 30px; border-left: 4px solid #e74c3c; border-radius: 4px; }}
                    h2 {{ color: #e74c3c; margin-top: 0; }}
                    pre {{ background: #f8f8f8; padding: 15px; border-radius: 4px; overflow-x: auto; }}
                </style>
            </head>
            <body>
                <div class='error'>
                    <h2>⚠️ Lỗi xử lý xem trước</h2>
                    <pre>{WebUtility.HtmlEncode(ex.Message)}</pre>
                </div>
            </body>
            </html>";

            // Trả về lỗi 500
            return StatusCode(500, Content(errorHtml, "text/html", Encoding.UTF8));
        }
    }
    
    [HttpPost("confirm-sample")]
    [Produces("application/json")]
    public async Task<IActionResult> ConfirmSampleAndGetFiles([FromBody] PreviewRequestDto input)
    {
        try
        {
            var buildRequest = MapPreviewRequestToBuildRequest(input);
            var userCode = input.Company?.SaleID ?? string.Empty;
            if (string.IsNullOrEmpty(userCode))
            {
                return BadRequest(new { error = "Mã Sale (userCode) không được để trống trong dữ liệu công ty." });
            }
            var result = await _previewService.BuildSampleFilesAsync(buildRequest, userCode);

            return Ok(result);
        }
        catch (ArgumentException argEx)
        {
            return BadRequest(new { error = argEx.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi xử lý xác nhận mẫu: " + ex.Message });
        }
    }
    
    private InvoiceBuildRequest MapPreviewRequestToBuildRequest(PreviewRequestDto input)
    {
        if (input == null)
        {
            throw new ArgumentException("Request body rỗng! Vui lòng gửi JSON data với Content-Type: application/json");
        }
        var isVCNB = input.Config.IsVCNB;
        var isTaxDocument = input.Config.IsTaxDocument;
        var isHangGuiDaiLy = input.Config.isHangGuiDaiLy;
        var isSpecialTemplate = isVCNB || isTaxDocument || isHangGuiDaiLy;

        if (!isSpecialTemplate && input.TemplateId == 0)
        {
            throw new ArgumentException("Vui lòng chọn loại hóa đơn hoặc mẫu hóa đơn!");
        }

        var adjustConfig = input.Config.AdjustConfig ?? new AdjustConfigDto();
        var logoPos = adjustConfig.LogoPos ?? new PosConfig();
        var backgroundPos = adjustConfig.BackgroundPos ?? new PosConfig();
        var vienConfig = adjustConfig.VienConfig ?? new VienConfig();

        // 2. Mapping DTO
        var buildRequest = new InvoiceBuildRequest
        {
            TemplateId = input.TemplateId,
            XmlDataId = input.XmlDataId,
            Company = new CmpnInfo2
            {
                SName = input.Company.SName,
                MerchantID = input.Company.MerchantID,
                Address = input.Company.Address,
                Email = input.Company.Email,
                Tel = input.Company.Tel,
                Fax = input.Company.Fax,
                Website = input.Company.Website,
                BankNumber = input.Company.BankNumber,
                BankInfo = input.Company.BankInfo,
                SampleID = input.SampleData?.Pattern ?? "1",
                SampleSerial = input.SampleData?.Serial ?? "",
                LogoBase64 = input.Company.LogoBase64 ?? "",
                BackgroundBase64 = input.Company.BackgroundBase64 ?? "",
                PersonOfMerchant = input.Company.PersonOfMerchant ?? "",
                SaleID = input.Company.SaleID ?? ""
            },
            Options = new InvoiceConfigDto
            {
                // Flags
                TokhaiApproved = input.Config.TokhaiApproved,
                IsVCNB = input.Config.IsVCNB,
                SignAtClient = input.Config.SignAtClient,
                IsMultiVat = input.Config.IsMultiVat,
                GenerateNumberOnSign = input.Config.GenerateNumberOnSign,
                SendMailAtServer = input.Config.SendMailAtServer,
                PriceBeforeVat = input.Config.PriceBeforeVat,
                HasFee = input.Config.HasFee,
                IsTaxDocument = input.Config.IsTaxDocument,
                isHangGuiDaiLy = input.Config.isHangGuiDaiLy,
                UseSampleData = input.Config.UseSampleData,

                // Base64 & Custom Content
                LogoBase64 = input.Images?.LogoBase64 ?? input.Company?.LogoBase64 ?? "",
                BackgroundBase64 = input.Images?.BackgroundBase64 ?? input.Company?.BackgroundBase64 ?? "",
                CustomXsltContent = input.Config.CustomXsltContent,
                CustomCss = input.Config.CustomCss,

                // Adjust Config
                AdjustConfig = new AdjustConfigDto
                {
                    IsEmail = adjustConfig.IsEmail,
                    IsFax = adjustConfig.IsFax,
                    IsSoDT = adjustConfig.IsSoDT,
                    IsWebsite = adjustConfig.IsWebsite,
                    IsSongNgu = adjustConfig.IsSongNgu,
                    IsTaiKhoanNganHang = adjustConfig.IsTaiKhoanNganHang,

                    LogoPos = new PosConfig
                    {
                        Width = logoPos.Width,
                        Height = logoPos.Height,
                        Top = logoPos.Top,
                        Left = logoPos.Left
                    },
                    BackgroundPos = new PosConfig
                    {
                        Width = backgroundPos.Width,
                        Height = backgroundPos.Height,
                        Top = backgroundPos.Top,
                        Left = backgroundPos.Left
                    },
                    IsThayDoiVien = adjustConfig.IsThayDoiVien,
                    VienConfig = new VienConfig
                    {
                        SelectedVien = vienConfig.SelectedVien,
                        DoManh = vienConfig.DoManh
                    },
                },
            }
        };
        return buildRequest;
    }
    private async Task<string> BuildHtmlFromRequest(PreviewRequestDto input)
    {
        var buildRequest = MapPreviewRequestToBuildRequest(input);
        return await _previewService.BuildInvoiceHtmlAsync(buildRequest);
    }
}