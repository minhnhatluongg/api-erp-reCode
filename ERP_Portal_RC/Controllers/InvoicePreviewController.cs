using Dapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class InvoicePreviewController : ControllerBase
{
    private readonly IInvoicePreviewService _previewService;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IConnectionRepository _connection;
    private readonly IConfiguration _cfg;
    private readonly ILogger<InvoicePreviewController> _log;

    private const string BosOnline = "BosOnline";
    private const string BosConfigure = "BosConfigure";

    public InvoicePreviewController(
        IInvoicePreviewService previewService,
        IDbConnectionFactory dbConnectionFactory,
        IConnectionRepository connection,
        IConfiguration cfg,
        ILogger<InvoicePreviewController> log)
    {
        _previewService = previewService;
        _dbConnectionFactory = dbConnectionFactory;
        _connection = connection;
        _cfg = cfg;
        _log = log;
    }

    /// <summary>
    /// API View hóa đơn - Dành cho nghiệp vụ thiết kế mẫu - Demo : winsale.wininvoice.vn
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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

    /// <summary>
    /// API Phát hành mẫu sau khi chọn mẫu xong - Dành cho nghiệp vụ thiết kế mẫu - Check sau khi hoàn thành ở quanly.wininvoice.vn
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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

    /// <summary>
    /// API Quick Publish – Phát hành nhanh mẫu hóa đơn chính thức (dời từ TVAN_WEB_API /api/odoo/orders/quick-publish).
    /// Tạo Job duyệt + chạy SP bosConvert_TT78 để phát hành mẫu trên server EVAT của khách hàng.
    /// </summary>
    [HttpPost("quick-publish")]
    [Produces("application/json")]
    public async Task<IActionResult> QuickPublish([FromBody] FinalConfirmSampleDto model)
    {
        string traceId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        if (model == null)
            return BadRequest(new { Success = false, Message = "Request body rỗng." });

        if (string.IsNullOrEmpty(model.OID))
            return BadRequest(new { Success = false, Message = "OID không được để trống." });

        if (string.IsNullOrEmpty(model.ConfiguredXsltBase64))
            return BadRequest(new { Success = false, Message = "ConfiguredXslt không được để trống." });

        var result = await HandleQuickPublishAsync(model, traceId);

        if (result.Success)
        {
            return Ok(new { Success = true, Message = result.Message, TraceId = traceId });
        }

        return StatusCode(500, new { Success = false, Message = result.Message, TraceId = traceId });
    }

    #region QuickPublish – Flow Orchestrator

    private async Task<(bool Success, string? Message)> HandleQuickPublishAsync(FinalConfirmSampleDto model, string traceId)
    {
        if (model.Company == null)
            return (false, "Thiếu thông tin công ty (Company).");

        var cnEvat = _connection.GetCnServerByMST(model.Company.MerchantID, "", "EVAT");
        if (string.IsNullOrEmpty(cnEvat))
            return (false, "Không xác định được server EVAT cho MST này.");

        try
        {
            string internalMerchantID = string.Empty;

            using (var dbConfig = _dbConnectionFactory.GetConnection(BosConfigure))
            {
                await dbConfig.OpenAsync();

                var row = await dbConfig.QueryFirstOrDefaultAsync<dynamic>(
                    "bosConfigure.dbo.bos_ChkServerSidesMST_v26",
                    new { MST = model.Company.MerchantID },
                    commandType: CommandType.StoredProcedure);

                if (row != null)
                {
                    internalMerchantID = row.MerchantID?.ToString();
                }
            }

            if (string.IsNullOrEmpty(internalMerchantID))
                return (false, $"MST {model.Company.MerchantID} chưa có MerchantID trên bosConfigure.");

            string originalMST = model.Company.MerchantID;
            model.Company.MerchantID = internalMerchantID;

            using (var dbOnline = _dbConnectionFactory.GetConnection(BosOnline))
            using (var dbEvat = new SqlConnection(cnEvat))
            {
                if (dbEvat.State == ConnectionState.Closed) await dbEvat.OpenAsync();
                if (dbOnline.State == ConnectionState.Closed) await dbOnline.OpenAsync();

                var econPlaceholder = new EContracts
                {
                    CusTax = originalMST,
                    CmpnTax = "0312308303",
                    CusName = model.Company.SName,
                    SampleID = model.InvSample ?? model.SampleData?.Pattern,
                    SerialID = model.InvSign ?? model.SampleData?.Serial
                };

                var jobResult = await InsertJobAndApprove2(dbOnline, model.OID!, "JB:004", "JOB_00002", econPlaceholder, model, traceId);
                if (!jobResult.Success)
                    return (false, $"Lỗi tạo/duyệt Job: {jobResult.OIDJob}");

                // STEP 2: Giải mã XSLT Content
                string xsltRaw = string.Empty;
                if (!string.IsNullOrEmpty(model.ConfiguredXsltBase64))
                {
                    var base64Data = model.ConfiguredXsltBase64.Contains(",")
                        ? model.ConfiguredXsltBase64.Split(',')[1]
                        : model.ConfiguredXsltBase64;
                    xsltRaw = Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                }

                await ExecuteBosConvertTT78(dbEvat, model.OID!, model, xsltRaw, traceId);

                return (true, "Phát hành mẫu thành công.");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[{traceId}] ERROR QuickPublish: {ex.Message}", "publish-error");
            _log.LogError(ex, "[QuickPublish] {TraceId} - {Message}", traceId, ex.Message);
            return (false, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    private async Task ExecuteBosConvertTT78(IDbConnection db, string oid, FinalConfirmSampleDto model, string xsltContent, string traceId)
    {
        var p = new DynamicParameters();

        // Xác định factorID (VCNB hay thường)
        string factorId = model.FactorId ?? "EXPOR_GOODSINVC";

        // Xác định SampleID (NEW hay mã cụ thể)
        string sampleId = model.SampleId ?? "NEW";

        // Parameters giống tool Winform
        p.Add("@SampleID", sampleId);
        p.Add("@InvcFrm", 1); // Luôn bắt đầu từ 1
        p.Add("@InvcEnd", model.InvTo);
        p.Add("@InvcTotal", model.InvTo);
        p.Add("@IsActive", model.Config?.TokhaiApproved ?? false); // cksToKhai.Checked
        p.Add("@IsMultiTax", model.Config?.IsMultiVat ?? false); // ckDTS.Checked
        p.Add("@govSampleSign", model.Company?.SampleID); // txtSample.Text - Mẫu số chính thức
        p.Add("@govInvcSign", model.Company?.SampleSerial); // txtSign.Text - Ký hiệu chính thức
        p.Add("@XsltContent", xsltContent);
        p.Add("@XsltFile", model.XsltFileName ?? "template.xslt"); // _fileName
        p.Add("@LogoBase64", model.LogoBase64 ?? "");
        p.Add("@Filelogo", model.logoFileName ?? "logo.png"); // txtLogoName.Text
        p.Add("@FileBackground", model.backgroundFileName ?? "background.png"); // txtBackground.Text
        p.Add("@BackgroundBase64", model.BackgroundBase64 ?? "");
        p.Add("@MerchantName", model.Company?.SName); // txtCmpnName.Text
        p.Add("@Tel", model.Company?.Tel ?? ""); // txtPhone.Text
        p.Add("@Fax", model.Company?.Fax ?? ""); // txtUyQuyen.Text (lưu ý: trong tool Fax map từ txtUyQuyen)
        p.Add("@FullAddress", model.Company?.Address ?? ""); // txtAddress.Text
        p.Add("@BankNumber", model.Company?.BankNumber ?? ""); // txtBanknumber.Text
        p.Add("@BankAddress", model.Company?.BankInfo ?? ""); // txtBankName.Text
        p.Add("@website", model.Company?.Website ?? ""); // txtWebsite.Text
        p.Add("@Email", model.Company?.Email ?? ""); // txtEmail.Text
        p.Add("@MerchantID", model.Company?.MerchantID); // txtMerchantID.Text (MST)
        p.Add("@Crt_User", model.Company?.SaleID); // Session.UserCode
        p.Add("@Descript", model.Company?.Description ?? ""); // txtDescript.Text
        p.Add("@IsSignServerProcess", model.Config?.cksIsSignServerProcess ?? false); // cksIsSignServerProcess.Checked
        p.Add("@UseFactorID", factorId); // "EXPOR_GOODSINVC" hoặc "EXPOR_INVCVCNB"

        try
        {
            LogToFile($"[{traceId}] 🚀 Executing bosConvert_TT78 with params: SampleID={sampleId}, InvcEnd={model.SampleData?.Invc_End}, FactorID={factorId}", "sample-publish");

            var rs = await db.QueryAsync<dynamic>(
                "BosEVAT..bosConvert_TT78",
                p,
                commandType: CommandType.StoredProcedure);

            if (rs == null || !System.Linq.Enumerable.Any(rs))
            {
                LogToFile($"[{traceId}] ⚠️ bosConvert_TT78 returned no result (might be OK)", "sample-publish");
            }
            else
            {
                LogToFile($"[{traceId}] ✅ bosConvert_TT78 Execution Success - Result count: {System.Linq.Enumerable.Count(rs)}", "sample-publish-success");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[{traceId}] ❌ bosConvert_TT78 FAILED: {ex.Message}\nStackTrace: {ex.StackTrace}", "sample-publish-error");
            throw new Exception($"Lỗi phát hành mẫu (bosConvert_TT78): {ex.Message}");
        }
    }

    private async Task<(bool Success, string? OIDJob)> InsertJobAndApprove2(
        IDbConnection db,
        string referenceOID,
        string entryId,
        string factorId,
        EContracts econtract,
        FinalConfirmSampleDto model,
        string traceId)
    {
        LogToFile(string.Format("[{0}] Starting Job creation (Ref OID: {1})", traceId, referenceOID), "job-init");

        var jobParams = new DynamicParameters();
        jobParams.Add("@ReferenceID", referenceOID);
        jobParams.Add("@FactorID", factorId);
        jobParams.Add("@EntryID", entryId);
        jobParams.Add("@Descrip", model.Company?.Description ?? string.Empty);
        jobParams.Add("@Crt_User", model.Company?.SaleID);
        jobParams.Add("@InvcSign", model.InvSign);
        jobParams.Add("@InvcFrm", SafeInt(model.InvFrom));
        jobParams.Add("@InvcEnd", SafeInt(model.InvTo));
        jobParams.Add("@invcSample", model.InvSample);
        jobParams.Add("@CmpnID", "26");
        jobParams.Add("@MailAcc", "ketoan.hoadonso@gmail.com");
        jobParams.Add("@ReferenceInfo", string.Format("Yêu cầu cấp tài khoản {0}-{1}", model.Company?.MerchantID, econtract.CusName));
        jobParams.Add("@isAuto", true);

        var oidJob = await db.QueryFirstOrDefaultAsync<string>(
            "BosOnline..wspInsert_EContractJobs_IsAuto_v22",
            jobParams,
            commandType: CommandType.StoredProcedure);

        if (string.IsNullOrWhiteSpace(oidJob))
        {
            LogToFile(string.Format("[{0}] ❌ Job creation failed (No OID returned)", traceId), "job-error");
            return (false, null);
        }

        LogToFile(string.Format("[{0}] Job created. OIDJob={1}", traceId, oidJob), "job-success");

        try
        {
            await ApproveJobStep2(db, oidJob, referenceOID, econtract, model, 0, 101, traceId);
            await ApproveJobStep2(db, oidJob, referenceOID, econtract, model, 101, 201, traceId);
        }
        catch (Exception ex)
        {
            LogToFile(string.Format("[{0}] ❌ Job Approve failed for OIDJob={1}: {2}", traceId, oidJob, ex.Message), "job-approve-error");
            return (true, oidJob);
        }

        return (true, oidJob);
    }

    private async Task ApproveJobStep2(
        IDbConnection db,
        string oidJob,
        string referenceOID,
        EContracts econtract,
        FinalConfirmSampleDto model,
        int holdSignNumb,
        int nextSignNumb,
        string traceId)
    {
        LogToFile(string.Format("[{0}] Approving Job {1}: Step {2}->{3}", traceId, oidJob, holdSignNumb, nextSignNumb), "job-approve");

        var appParams = new DynamicParameters();

        appParams.Add("@FactorID", "JOB_00002");
        appParams.Add("@OID", oidJob);
        appParams.Add("@ODate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        appParams.Add("@CmpnID", "26");
        appParams.Add("@Crt_User", model.Company?.SaleID);

        appParams.Add("@DataTbl", "EContractJobs");
        appParams.Add("@SignTble", "zsgn_EContractJobs");
        appParams.Add("@SignChck", 0);
        appParams.Add("@holdSignNumb", holdSignNumb);
        appParams.Add("@nextSignNumb", nextSignNumb);
        appParams.Add("@AppvMess", string.Format("Trình ký Job Cấp TK {0}->{1}", holdSignNumb, nextSignNumb));
        appParams.Add("@EntryID", "JB:004");

        appParams.Add("@Variant26", econtract.CusTax);
        appParams.Add("@Variant27", econtract.CusTax);
        appParams.Add("@Variant28", "0312308303");
        appParams.Add("@Variant29", model.SampleData?.Serial);
        appParams.Add("@Variant30", "1");

        await db.ExecuteAsync(
            "BosApproval.dbo.zsgn_EContractJobs_NOR",
            appParams,
            commandType: CommandType.StoredProcedure);

        LogToFile(string.Format("[{0}] Job {1} moved to state {2}", traceId, oidJob, nextSignNumb), "job-approve-success");
    }

    private static int SafeInt(int value) => value;

    #endregion

    #region Mapping helpers

    private InvoiceBuildRequest MapPreviewRequestToBuildRequest(PreviewRequestDto input)
    {
        if (input == null)
        {
            throw new ArgumentException("Request body rỗng! Vui lòng gửi JSON data với Content-Type: application/json");
        }
        var isVCNB = input.Config!.IsVCNB;
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
                SName = input.Company!.SName,
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

    #endregion

    #region Logging Helper

    /// <summary>
    /// Ghi log đơn giản ra file – chuyển nguyên từ TVAN_WEB_API/OdooOrdersController.LogToFile.
    /// </summary>
    private void LogToFile(string msg, string prefix)
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

            var file = Path.Combine(logDir, string.Format("{0}-{1}.log", prefix, DateTime.Now.ToString("yyyyMMdd")));
            System.IO.File.AppendAllText(file, string.Format("[{0}] {1}{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), msg, Environment.NewLine), Encoding.UTF8);
        }
        catch
        {
            // Nuốt mọi exception ghi log để không ảnh hưởng nghiệp vụ chính
        }
    }

    #endregion
}
