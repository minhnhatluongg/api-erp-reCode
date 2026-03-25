using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.Count_Invoice;
using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.EntitiesIntergration;
using ERP_Portal_RC.Domain.Enum;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;
using System.Xml.Xsl;
using static ERP_Portal_RC.Domain.Enum.PublicEnum;

namespace ERP_Portal_RC.Application.Services
{
    public class EcontractService : IEcontractService
    {
        private readonly IEContractRepository _eContractRepository;
        private readonly IMailService _mailService;
        private readonly IFileStorageService _fileStorageService;
        private readonly FileConfig _fileConfig;
        private readonly IConnectionRepository _connectionRepo;
        private readonly IConfiguration _configuration;

        public EcontractService(IEContractRepository econtractRepo, 
            IMailService mailService, 
            IFileStorageService fileStorageService,
            IConnectionRepository connectionRepository,
            IConfiguration configuration,
            IOptions<FileConfig> fileConfigOptions)
        {
            _eContractRepository = econtractRepo;
            _mailService = mailService;
            _fileStorageService = fileStorageService;
            _configuration = configuration;
            _connectionRepo = connectionRepository;
            _fileConfig = fileConfigOptions.Value;
        }
        public async Task<EContractServiceResult> GetAllEContractsAsync(string userName, EContractFilterRequest request, string groupList, string userCode)
        {

            var menuTask = _eContractRepository.GetDSMenuByID(userName, groupList);
            var moneyTask = _eContractRepository.CheckBCTT("26", userCode, "0000003");

            await Task.WhenAll(menuTask, moneyTask);

            var CN = await moneyTask;
            var resultMenu = await menuTask;

            var dateFrom = !string.IsNullOrEmpty(request.FrmDate) ? request.FrmDate : "2010-01-01";
            var dateTo = !string.IsNullOrEmpty(request.ToDate) ? request.ToDate : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            string crtUser = (resultMenu?.mode == 1 || request.IsUser == "1") ? userCode : "%";

            ListEcontractViewModel result;
            if (!string.IsNullOrEmpty(request.EmplChild) && request.EmplChild != "null")
            {
                string strEmplChild = (string.IsNullOrEmpty(request.StrEmplChild) || request.StrEmplChild == "null")
                    ? userCode : request.StrEmplChild;

                result = await _eContractRepository.GetEContractsByHierarchyAsync(request.EmplChild, strEmplChild, dateFrom, dateTo, userCode);
            }
            else if (!string.IsNullOrEmpty(request.CusTName))
            {
                result = await _eContractRepository.Search(request.CusTName, crtUser, dateFrom, dateTo);
            }
            else
            {
                result = await _eContractRepository.GetAllList(crtUser, dateFrom, dateTo);
            }

            if (result?.lstMonitor == null) return new EContractServiceResult { Total = 0, Data = new List<EContract_Monitor>() };

            var filteredQuery = result.lstMonitor.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Status) && int.TryParse(request.Status, out int statusInt))
            {
                filteredQuery = filteredQuery.Where(x => x.CurrSignNumb == statusInt);
            }

            // 5. Phân trang (Pagination)
            int totalRecords = filteredQuery.Count();
            int pageSize = request.PageSize;
            int page = request.Page;
            int offset = (page - 1) * pageSize;

            var pagedList = (pageSize == -1)
                ? filteredQuery.ToList()
                : filteredQuery.Skip(offset).Take(pageSize).ToList();

            if (pagedList.Any())
            {
                var currentPageOids = pagedList.Select(x => x.OID).ToList();
                var oidsWithDetails = await _eContractRepository.GetListOIDHasDetails(currentPageOids);
                var oidsWithDetailsSet = new HashSet<string>(oidsWithDetails ?? new List<string>());

                foreach (var item in pagedList)
                {
                    // Đảm bảo ngày tháng hợp lệ
                    if (item.ODATE == DateTime.MinValue) item.ODATE = DateTime.Now;

                    // Quyền Disable dựa trên người tạo
                    item.IsDisiable = (item.Crt_User != userCode);

                    // Mapping trạng thái TT2 từ kế toán (TT6) nếu có
                    if (item.currSignNumbJobKT != 0) item.TT2 = item.TT6;

                    // Kiểm tra xem có chi tiết đính kèm không
                    item.isCheckedShow = oidsWithDetailsSet.Contains(item.OID);
                }
            }

            return new EContractServiceResult
            {
                MoneyToBePaid = (CN != null && CN.PTHU != 0.000m) ? FormatCurrency(CN.PTHU) : "0",
                MoneyPaid = (CN != null && CN.DABTT != 0.000m) ? FormatCurrency(CN.DABTT) : "0",
                Data = pagedList,
                Total = totalRecords,
                Disable = result.IsDisiable
            };
        }
        public async Task<DashboardStatsDto> GetContractDashboardAsync(string userCode, string userName, string grpList, bool isManager, ContractSearchRequest request)
        {
            var menuInfo = await _eContractRepository.GetDSMenuByID(userName, grpList);
            string crtUserQuery = (menuInfo.mode == 1) ? userCode : UserMaster.UserCode;

            // Lấy dữ liệu (Mặc định lấy từ đầu năm để có dữ liệu Year/Month)
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1).ToString("yyyy-MM-dd");
            var endOfToday = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var result = await _eContractRepository.CountList(crtUserQuery, startOfYear, endOfToday);
            var list = result.lstMonitor ?? new List<EContract_Monitor>();
            var now = DateTime.Now;

            var ds = new DashboardStatsDto
            {
                // 1. Thống kê tổng quát (Toàn bộ hệ thống hoặc theo phân quyền Master)
                countAll = list.Count,
                count0 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.TRINH_KY),
                count101 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.CHO_KIEM_TRA),
                count201 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.CHO_GD_DUYEN),
                count301 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.HD_DA_DUYET),
                countKHSign = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.KH_DA_KY),
                countClose = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.HD_DONG),
                countPH = list.Count(s => s.TT4 == TTStatus.TT4_DACAP), // Phát hành hóa đơn

                // 2. Thống kê theo thời gian (Dòng dữ liệu bị thiếu của bạn)
                countAllDay = list.Count(s => s.Crt_Date.Date == now.Date),
                countAllMonth = list.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year),
                countAllYear = list.Count(s => s.Crt_Date.Year == now.Year),

                // 3. Thống kê theo User cụ thể (Sale xem dashboard cá nhân)
                countAlluser = list.Count(s => s.Crt_User == userCode),
                count0user = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.TRINH_KY),
                count101user = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.CHO_KIEM_TRA),
                count301user = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.HD_DA_DUYET),
                countKHSignuser = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.KH_DA_KY),
                countPHuser = list.Count(s => s.Crt_User == userCode && s.TT4 == TTStatus.TT4_DACAP),
            };

            // Đừng quên gán các trường Sum nếu UI cần hiển thị
            ds.countAllSum = ds.countAll;
            ds.sumDT = list.Where(s => s.CurrSignNumb == (int)CurrSignNum.HD_DA_DUYET).Sum(s => s.ItemPrice);

            return ds;
        }

        public async Task<ListEcontractViewModel> GetContractListAsync(string userCode, string userName, string grpList, bool isManager, ContractSearchRequest request)
        {
            var menuInfo = await _eContractRepository.GetDSMenuByID(userName, grpList);
            string crtUserQuery = (menuInfo.mode == 1) ? userCode : UserMaster.UserCode;

            ListEcontractViewModel result;
            string dateFrom = request.FrmDate ?? "2019-01-01";
            string dateTo = request.ToDate ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            string searchKeyword = request.CusTName ?? request.CusTTax ?? request.NCC ?? request.Kinhdoanh;

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                result = await _eContractRepository.Search(searchKeyword, crtUserQuery, dateFrom, dateTo);
            }
            else
            {
                result = await _eContractRepository.GetAllList(crtUserQuery, dateFrom, dateTo);
            }

            if (!string.IsNullOrEmpty(request.Status) && result.lstMonitor != null)
            {
                int statusInt = int.Parse(request.Status);
                result.lstMonitor = result.lstMonitor.Where(x => x.CurrSignNumb == statusInt).ToList();
            }

            return result;
        }

        // 1. Trình ký hợp đồng: 0 -> 101 (Sử dụng store webContracts)
        public async Task<(bool success, bool emailSent)> ProposeSignContractAsync(ApprovalWorkflowRequest model, string userId, string saleFullName)
        {
            var config = ("EContract", "EC:001", 101, "zsgn_webContracts_NOR");
            var result = await _eContractRepository.ExecuteApprovalWorkflow(model, config, userId);
            var success = result == 1;

            if (!success) return (false, false);

            bool emailSent = false;
            try
            {
                var (cusTax, cusName) = await _eContractRepository.GetContractInfoForEmailAsync(model.OID);
                await _mailService.SendProposeSignNotificationAsync(
                    oid: model.OID,
                    cusTax: cusTax,
                    cusName: cusName,
                    saleFullName: saleFullName, 
                    ktName: string.Empty
                );
                emailSent = true;
            }
            catch
            {
                emailSent = false;
            }

            return (success, emailSent);
        }

        // 2. Đề xuất tạo mẫu (JB:005/JOB_00001 hoặc JB:004/JOB_00002):
        //    Tạo Job record + sign 0 -> 101 qua Ins_EContractJobs_RequestByOdoo
        public async Task<(bool success, string message)> ProposeTemplateAsync(
            ERP_Portal_RC.Domain.Entities.EContractJobRequest request, string userId)
        {
            return await _eContractRepository.CreateEContractJobAsync(request, userId);
        }


        // 3. Phát hành mẫu (JB:004/JOB_00002): Tìm Job OID -> nâng 101 -> 201
        public async Task<(bool success, string message)> IssueInvoiceAsync(ApprovalWorkflowRequest model, string userId)
        {
            return await _eContractRepository.AdvanceEContractJobSigningAsync(
                contractOid:  model.OID,
                factorId:     "JOB_00002",
                entryId:      "JB:004",
                userId:       userId,
                fromSignNumb: 101,
                toSignNumb:   201,
                appvMess:     model.AppvMess
            );
        }
        public async Task<Template?> GetTemplateByCodeAsync(string factorId)
        {
            return await _eContractRepository.GetTemplateByCodeAsync(factorId);
        }

        public async Task<Template?> GetOriginalContractAsync()
        {
            return await _eContractRepository.GetTemplateByCodeAsync("TT78_EContract");
        }

        public async Task<Template?> GetCompensationContractAsync()
        {
            return await _eContractRepository.GetTemplateByCodeAsync("TT78_EContractExt");
        }

        public async Task<Template?> GetExtensionContractAsync()
        {
            return await _eContractRepository.GetTemplateByCodeAsync("TT78_EContractExt1");
        }
        public async Task<ApiResponse<string>> GenerateContractPreviewAsync(ContractPreviewRequest request)
        {
            var template = await _eContractRepository.GetTemplateByCodeAsync(request.FactorID);
            if (template == null)
                return ApiResponse<string>.ErrorResponse("Không tìm thấy mẫu hợp đồng!", 404);

            try
            {
                string htmlResult = ProcessContractMapping(template, request);
                return ApiResponse<string>.SuccessResponse(htmlResult, "Tạo bản xem trước thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse($"Lỗi xử lý template: {ex.Message}", 500);
            }
        }
        #region Helper
        public string FormatCurrency(decimal? number)
        {
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("vi-VN");
            string a = string.Format(culture, "{0:N0}", number);
            return a;
        }
        private string ProcessContractMapping(Template template, ContractPreviewRequest request)
        {
            // --- BƯỚC 1: BUILD DANH SÁCH SẢN PHẨM {ProductLines} ---
            var sbProducts = new StringBuilder();
            decimal sumTotal = 0;

            if (request.Details != null && request.Details.Any())
            {
                int idx = 1;
                foreach (var detail in request.Details)
                {
                    decimal totalItem = detail.Qtty * detail.Price;
                    sumTotal += totalItem;

                    string escapedName = SecurityElement.Escape(detail.ItemName ?? "");
                    string unit = (detail.Unit ?? "").Trim();

                    sbProducts.Append("<HHDVu>");
                    sbProducts.Append($"<STT>{idx++}</STT>");

                    // Bao phủ đầy đủ các thẻ mà XSLT có thể truy vấn
                    sbProducts.Append($"<THHDVu>{escapedName}</THHDVu><ItemName>{escapedName}</ItemName><itemName>{escapedName}</itemName>");
                    sbProducts.Append($"<DVTinh>{unit}</DVTinh><itemUnitName>{unit}</itemUnitName><ItemUnit>{unit}</ItemUnit>");
                    sbProducts.Append($"<SLuong>{detail.Qtty}</SLuong><ItemQtty>{detail.Qtty}</ItemQtty><itemQtty>{detail.Qtty}</itemQtty>");
                    sbProducts.Append($"<DGia>{detail.Price}</DGia><ItemPrice>{detail.Price}</ItemPrice><itemPrice>{detail.Price}</itemPrice>");
                    sbProducts.Append($"<ThTien>{totalItem}</ThTien><Sum_Amnt>{totalItem}</Sum_Amnt><sum_Amnt>{totalItem}</sum_Amnt>");

                    // Thông tin bổ sung cho bảng kê
                    sbProducts.Append($"<MSo>{detail.InvcSample}</MSo><invcSample>{detail.InvcSample}</invcSample>");
                    sbProducts.Append($"<KHieu>{detail.InvcSign}</KHieu><InvcSign>{detail.InvcSign}</InvcSign>");
                    sbProducts.Append($"<TSo>{detail.InvcFrm}</TSo><invcFrm>{detail.InvcFrm}</invcFrm>");
                    sbProducts.Append($"<DSo>{detail.InvcEnd}</DSo><invcEnd>{detail.InvcEnd}</invcEnd>");

                    sbProducts.Append("</HHDVu>");
                }
            }

            // --- BƯỚC 2: REPLACE PLACEHOLDER VÀO XML TEMPLATE ---
            DateTime now = DateTime.Now;
            string finalXml = template.XmlContent ?? "";

            finalXml = finalXml
                .Replace("{order_code}", request.OrderCode ?? "ĐANG TẠO")
                .Replace("{order_date_day}", now.Day.ToString("00"))
                .Replace("{order_date_month}", now.Month.ToString("00"))
                .Replace("{order_date_year}", now.Year.ToString())
                .Replace("{partner_name}", SecurityElement.Escape(request.PartnerName ?? ""))
                .Replace("{partner_vat}", request.PartnerVat ?? "")
                .Replace("{partner_address}", SecurityElement.Escape(request.PartnerAddress ?? ""))
                .Replace("{partner_phone}", request.PartnerPhone ?? "")
                .Replace("{partner_email}", request.PartnerEmail ?? "")
                .Replace("{partner_bank_no}", request.PartnerBankNo ?? "")
                .Replace("{partner_bank_title}", request.PartnerBankAddress ?? "")
                .Replace("{partner_contact_name}", SecurityElement.Escape(request.PartnerContactName ?? ""))
                .Replace("{partner_contact_job}", request.PartnerContactJob ?? "")
                .Replace("{partner_legal_value}", request.PartnerContactJob ?? "")
                .Replace("{ProductLines}", sbProducts.ToString())
                .Replace("{TgTTTBSo}", sumTotal.ToString("0"))
                .Replace("{TgTTTBChu}", NumberToText(sumTotal)); 

            // --- BƯỚC 3: LÀM SẠCH XSLT (FIX LỖI "Stylesheet must start...") ---
            string xsltContent = (template.XsltContent ?? "").Trim();
            int firstTagIndex = xsltContent.IndexOf("<");
            if (firstTagIndex > 0)
            {
                xsltContent = xsltContent.Substring(firstTagIndex);
            }

            // --- BƯỚC 4: TRANSFORM XML -> HTML ---
            var transformer = new XslCompiledTransform();
            using (var srXslt = new StringReader(xsltContent))
            using (var xrXslt = XmlReader.Create(srXslt))
            {
                transformer.Load(xrXslt, new XsltSettings(true, true), new XmlUrlResolver());
            }

            var sbResult = new StringBuilder();
            using (var srXml = new StringReader(finalXml))
            using (var xrXml = XmlReader.Create(srXml))
            using (var sw = new StringWriter(sbResult))
            {
                transformer.Transform(xrXml, null, sw);
            }

            return sbResult.ToString();
        }

        public string NumberToText(decimal total)
        {
            try
            {
                if (total == 0) return "Không đồng chẵn./.";

                string[] unit = { "", " nghìn", " triệu", " tỷ", " nghìn tỷ" };
                string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
                string result = "";
                long number = (long)Math.Abs(total);
                int unitIndex = 0;

                while (number > 0)
                {
                    int group = (int)(number % 1000);
                    if (group > 0)
                    {
                        string groupText = "";
                        int h = group / 100;
                        int t = (group % 100) / 10;
                        int u = group % 10;

                        // Đọc hàng trăm
                        if (h > 0 || number > 1000) groupText += digits[h] + " trăm ";

                        // Đọc hàng chục (mươi/mười/lẻ)
                        if (t > 1) groupText += digits[t] + " mươi ";
                        else if (t == 1) groupText += "mười ";
                        else if (h > 0 && u > 0) groupText += "lẻ ";

                        // Đọc hàng đơn vị (mốt/lăm)
                        if (t > 1 && u == 1) groupText += "mốt ";
                        else if (t > 0 && u == 5) groupText += "lăm ";
                        else if (u > 0) groupText += digits[u] + " ";

                        result = groupText + unit[unitIndex] + " " + result;
                    }
                    number /= 1000;
                    unitIndex++;
                }

                result = result.Trim().Replace("  ", " ");
                return char.ToUpper(result[0]) + result.Substring(1);
            }
            catch { return "Lỗi đọc số"; }
        }

        private EContractMaster MapToMaster(ContractPreviewRequest req, string user)
        {
            return new EContractMaster
            {
                OID = req.OrderCode,
                CmpnID = "26",
                CmpnName = req.CmpnName,
                CmpnAddress = req.CmpnAddress,
                CmpnContactAddress = req.CmpnContactAddress,
                CmpnTax = req.CmpnTax ?? "0312303803",
                CmpnTel = req.CmpnTel,
                CmpnMail = req.CmpnMail,
                CmpnPeople_Sign = req.CmpnPeople_Sign,
                CmpnPosition_BySign = req.CmpnPosition_Sign ?? "Giám Đốc", // Lưu ý: BySign
                CmpnBankAddress = req.CmpnBankAddress,
                CmpnBankNumber = req.CmpnBankNumber,
                FactorID = req.FactorID ?? "EContract",
                SampleID = req.SampleID ?? "0783",
                EntryID = "EC:001",
                SaleEmID = user,
                CusName = req.PartnerName,
                RegionID = "",
                CusAddress = req.PartnerAddress ?? "",
                CusContactAddress = req.PartnerAddress ?? "",
                CusTax = req.PartnerVat ?? "",
                CusTel = req.PartnerPhone ?? "",
                CusEmail = req.PartnerEmail ?? "",
                CusPeople_Sign = req.PartnerContactName, 
                CusPosition_BySign = req.PartnerContactJob ?? "Giám Đốc",
                CusBankAddress = req.PartnerBankAddress ?? "",
                CusBankNumber = req.PartnerBankNo ?? "",
                CustomerID = "",
                Crt_User = user,
                ChgeUser = user,
                IsOnline = true,
                IsTT78 = true,
                PrdcAmnt = 0,
                VAT_Rate = 0,
                VAT_Amnt = 0,
                DscnAmnt = 0,
                Sum_Amnt = 0,
                ODate = DateTime.Now,
                ReferenceDate = DateTime.Now,
                ReferenceID = "",
                HTMLContent = req.HTMLContent ?? "UE9TVCBPSw==",
                Descrip = req.Descrip ?? "Created From NEX-ERP",
                SignDate = DateTime.Now,
                SignNumb = -1,
            };
        }

        private List<EContractDetails> MapToDetails(ContractPreviewRequest req)
        {
            return req.Details.Select((d, index) =>
            {
                decimal.TryParse(d.VAT_Rate, out decimal vatRateValue);

                decimal amount = d.Qtty * d.Price;
                decimal vatAmount = amount * (vatRateValue / 100);
                decimal sumAmount = amount + vatAmount;

                return new EContractDetails
                {
                    ItemID = d.ItemID, 
                    ItemName = d.ItemName,
                    ItemUnit = d.Unit,
                    ItemPrice = d.Price,
                    ItemQtty = d.Qtty,
                    ItemAmnt = amount,
                    VAT_Rate = vatRateValue, 
                    VAT_Amnt = vatAmount,
                    Sum_Amnt = sumAmount,
                    InvcSample = d.InvcSample,
                    InvcSign = d.InvcSign,
                    InvcFrm = d.InvcFrm,
                    InvcEnd = d.InvcEnd,
                    itemUnitName = d.Unit,
                    ItemNo = index + 1,
                    ItemPerBox = 0,
                    isKM = false, 
                    sl_KM = 0
                };
            }).ToList();
        }
        private string DecompressGZip(byte[] gzipData)
        {
            if (gzipData == null || gzipData.Length == 0) return string.Empty;
            try
            {
                using var mStream = new MemoryStream(gzipData);
                using var gStream = new GZipStream(mStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gStream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch { return string.Empty; }
        }

        #endregion
        public async Task<ApiResponse<string>> ProcessSaveContractAsync(ContractPreviewRequest request, string userCode)
        {
            try
            {
                var master = MapToMaster(request, userCode);
                var details = MapToDetails(request);

                await _eContractRepository.SaveFullContractAsync(master, details);

                return ApiResponse<string>.SuccessResponse(
                    master.OID,
                    "Lưu hợp đồng và khởi tạo luồng phê duyệt thành công."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }

        public async Task<ContractStatusResponse> GetContractReviewDataAsync(string oid)
        {
            var raw = await _eContractRepository.GetContractStatusRawAsync(oid);
            if (raw == null || raw.Master == null) return null;

            var response = new ContractStatusResponse
            {
                Oid = oid,
                CustomerName = raw.Master.CustomerName,
                CustomerTaxCode = raw.Master.CustomerTaxCode,
                IsSigned = raw.SignedData != null,

                Master = new ERP_Portal_RC.Application.DTOs.EContractMasterSummary
                {
                    CusAddress = raw.Master.CusAddress,
                    CusWebsite = raw.Master.CusWebsite,
                    CusTel = raw.Master.CusTel,
                    CusEmail = raw.Master.CusEmail,
                    CusPeople_Sign = raw.Master.CusPeople_Sign,
                    CusPosition_BySign = raw.Master.CusPosition_BySign,
                    CusBankAddress = raw.Master.CusBankAddress,
                    CusBankNumber = raw.Master.CusBankNumber,
                    Descrip = raw.Master.Descrip,
                    Crt_Date = raw.Master.Crt_Date,
                    Crt_User = raw.Master.Crt_User,
                    ODate = raw.Master.ODate
                },

                Details = raw.Details.Select(d => new ERP_Portal_RC.Application.DTOs.EContractDetailSummary
                {
                    ItemID = d.ItemID,
                    ItemName = d.ItemName,
                    InvcSign = d.InvcSign,
                    InvcSample = d.InvcSample,
                    InvcFrm = d.InvcFrm,
                    InvcEnd = d.InvcEnd,
                    Price = d.Price,
                    Qtty = d.Qtty,
                    SumAmnt = d.SumAmnt
                }).ToList()
            };

            if (raw.SignedData != null)
            {
                var info = raw.SignedData;
                byte[] compressedData = null;

                // Logic chọn cột: 
                // A=0, B=1 (Chỉ bên B ký) -> Lấy InvcContent
                // A=1, B=1 (Cả hai cùng ký) -> Lấy InvcContent_ByCus
                if (!info.Party_A_IsSigned && info.Party_B_IsSigned)
                    compressedData = info.InvcContent;
                else if (info.Party_A_IsSigned && info.Party_B_IsSigned)
                    compressedData = info.InvcContent_ByCus;

                if (compressedData != null)
                {
                    string base64FromGzip = DecompressGZip(compressedData);
                    if(!string.IsNullOrEmpty(base64FromGzip))
                    {
                        try
                        {
                            byte[] xmlRawBytes = Convert.FromBase64String(base64FromGzip);
                            response.Base64Content = Encoding.UTF8.GetString(xmlRawBytes);
                        }
                        catch (FormatException)
                        {
                            response.Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(base64FromGzip));
                        }
                    }
                    response.SignedInfo = new PublicInfoSummary
                    {
                        InvcDate = info.InvcDate,
                        Party_A_IsSigned = info.Party_A_IsSigned,
                        Party_B_IsSigned = info.Party_B_IsSigned
                    };
                }
            }
            return response;
        }

        public async Task<ApiResponse<object>> DeleteDraftAsync(DeleteEcontractRequest request, string username)
        {
            string oid = request.OID;

            if (string.IsNullOrEmpty(oid))
                return ApiResponse<object>.ErrorResponse("OID không được để trống");

            var (ok, message) = await _eContractRepository.DeleteDraftAsync(oid, username);

            if (ok == 1)
                return ApiResponse<object>.SuccessResponse(null, message);

            return ApiResponse<object>.ErrorResponse(message);
        }

        public async Task<ApiResponse<object>> UnSignAsync(UnSignRequest model)
        {
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                var result = await _eContractRepository.UnSignAsync(model, correlationId);
                return ApiResponse<object>.SuccessResponse(result.Data, result.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Lỗi hệ thống khi hủy ký: " + ex.Message);
            }
        }

        public async Task<ApiResponse<EContractHistoryResponse>> GetJobHistoryAsync(string oid)
        {
            var raw = await _eContractRepository.GetFullHistoryDataAsync(oid);

            if (raw.History == null)
                return ApiResponse<EContractHistoryResponse>.ErrorResponse("Không tìm thấy dữ liệu.");

            var response = new EContractHistoryResponse();

            var historyList = raw.History.OrderBy(s => s.currSignDate).Select(h => new HistoryItemDTO
            {
                OID = h.OID,
                CurrSignNum = h.currSignNum,
                AppvMess = h.appvMess,
                FullName = h.FullName,
                CurrSignDate = h.currSignDate,
                CancelDescript = "" 
            }).ToList();

            foreach (var item in historyList)
            {
                switch (item.CurrSignNum)
                {
                    case StatusSignnum.TRINH_KY:
                        if (string.IsNullOrEmpty(item.AppvMess))
                        {
                            item.CurrSignNum = "Tạo mới hợp đồng";
                            item.AppvMess = "OK";
                        }
                        else
                        {
                            item.CurrSignNum = "Hợp đồng trả về";
                            item.AppvMess = "Lý do: " + item.AppvMess;
                        }
                        break;
                    case StatusSignnum.CHO_KIEM_TRA:
                        item.CurrSignNum = "Đề xuất ký";
                        item.AppvMess = "Trình ký";
                        break;
                    case StatusSignnum.CHO_GD_DUYEN:
                        item.CurrSignNum = "Trình ký Giám đốc";
                        item.FullName = "Hợp đồng trình ký giám đốc";
                        item.AppvMess = "Trình ký";
                        break;
                    case StatusSignnum.HD_DA_DUYET:
                        item.CurrSignNum = "Hợp đồng đã ký";
                        item.FullName = "Hợp đồng đã ký";
                        item.AppvMess = "OK";
                        break;
                    case StatusSignnum.KH_DA_KY:
                        item.CurrSignNum = "Hợp đồng đã được khách hàng ký";
                        item.FullName = "Khách hàng";
                        item.AppvMess = "OK";
                        break;
                    case StatusSignnum.HD_DONG:
                        item.CurrSignNum = "Đóng hợp đồng";
                        item.AppvMess = "OK";
                        break;
                    case StatusSignnum.TRA_VE:
                        item.CurrSignNum = "Hợp đồng bị trả về";
                        item.AppvMess = "Lý do: " + item.AppvMess;
                        break;
                }
            }

            if (raw.Jobs != null && raw.Jobs.Any())
            {
                response.JobList = raw.Jobs.Where(j =>
                    j.currSignNumb != (int)CurrSignNum.TRA_VE &&
                    j.currSignNumb != (int)CurrSignNum.TRA_VE200 &&
                    j.currSignNumb != (int)CurrSignNum.TRA_VE300
                ).ToList();

                foreach (var job in raw.Jobs)
                {
                    if (job.currSignNumb == (int)CurrSignNum.TRA_VE ||
                        job.currSignNumb == (int)CurrSignNum.TRA_VE200 ||
                        job.currSignNumb == (int)CurrSignNum.TRA_VE300)
                        continue;

                    var targetHistory = historyList.FirstOrDefault(h => h.OID == job.OID);

                    if (targetHistory != null)
                    {
                        string jobNote = "";
                        if (job.FactorID == JobFactor.JOB_00004.ToString() && job.EntryID == JobEntry.JB008)
                        {
                            jobNote = !string.IsNullOrEmpty(job.DescriptChange)
                                ? $"Lý do : {job.Reason} - {job.DescriptChange}"
                                : $"Lý do : {job.Reason}";
                        }
                        else if (job.FactorID == JobFactor.JOB_00005.ToString() && !string.IsNullOrEmpty(job.DescriptChange))
                        {
                            jobNote = "Ghi chú bổ sung : " + job.DescriptChange;
                        }

                        if (!string.IsNullOrEmpty(jobNote))
                        {
                            if (string.IsNullOrEmpty(targetHistory.CancelDescript))
                                targetHistory.CancelDescript = jobNote;
                            else
                                targetHistory.CancelDescript += " | " + jobNote;
                        }
                    }
                }
            }
            response.HistoryList = historyList;
            return ApiResponse<EContractHistoryResponse>.SuccessResponse(response, "Lấy lịch sử thành công.");
        }

        public async Task<List<JobEntity>> GetJobKTbyOID(string oid)
        {
            if (string.IsNullOrEmpty(oid)) return new List<JobEntity>();

            var data = await _eContractRepository.GetJobKTbyOID(oid);

            return data.OrderByDescending(x => x.crt_date).ToList();
        }

        public async Task<ApiResponse<List<EContractDetails>>> GetEContractDetailsActionAsync(string oid)
        {
            if (string.IsNullOrEmpty(oid))
                return ApiResponse<List<EContractDetails>>.ErrorResponse("OID không hợp lệ.");

            string cleanOid = oid.Replace("%2F", "/").Replace("%2f", "/");
            cleanOid = System.Net.WebUtility.UrlDecode(cleanOid);

            var details = await _eContractRepository.GetEContractDetailsNewAsync(cleanOid);

            if (details == null || !details.Any())
            {
                return ApiResponse<List<EContractDetails>>.SuccessResponse(new List<EContractDetails>(), "Không tìm thấy dữ liệu.");
            }

            return ApiResponse<List<EContractDetails>>.SuccessResponse(details, "Lấy chi tiết hợp đồng thành công.");
        }

        public async Task<ApiResponse<EContractDetailsViewModel>> GetJobDetailsAsync(string oid, string kt = "0")
        {
            string cleanOid = System.Net.WebUtility.UrlDecode(oid).Replace("%2F", "/");

            if (kt == "1")
            {
                await _eContractRepository.DeleteJob01Async(cleanOid);
            }

            var raw = await _eContractRepository.GetEContractRawDataAsync(cleanOid);
            if(raw == null)
            {
                return ApiResponse<EContractDetailsViewModel>.ErrorResponse("Không tìm thấy dữ liệu hợp đồng.");
            }
            var model = new EContractDetailsViewModel();
            //1.Mapping cơ bản
            MapBasicData(model, raw);
            //2.Xử lý Flag.
            ProcessInterfaceFlags(model, raw);

            await HandleAutomaticJobAsync(model, raw, cleanOid, kt);

            return ApiResponse<EContractDetailsViewModel>.SuccessResponse(model, "Lấy thông tin thành công.");
        }
        #region Handle API get-job-details
        private void ProcessInterfaceFlags(EContractDetailsViewModel model, EContractHistoryRaw2 raw)
        {
            model.IsshowJob = false;
            model.IsshowJobKT = false;
            model.IsshowEntry = false;
            model.IsshowEntryCS = true;

            var jobPostJob = raw.JobPosts.Where(s => s.FactorID == "JOB_00001").ToList();
            if (jobPostJob.Any())
            {
                var currSign = jobPostJob.First().SignNumb;
                if (currSign == "501")
                {
                    model.IsshowJob = true;
                    model.IsshowJobKT = true;
                    model.IsshowEntry = true;
                    model.IsshowEntryCS = false;
                }
            }

            // Check ẩn hiện yêu cầu chỉnh sửa (IsshowYC)
            model.IsshowYC = raw.JobPosts.Any(s => s.FactorID == "JOB_00001" && s.EntryID == "JB:005" && s.SignNumb == "101");

            // Trạng thái hợp đồng đã ký (IsshowYCCS)
            model.EContracts.IsshowYCCS = raw.EContract.CurrSignNumb <= 0;
        }
        private void MapBasicData(EContractDetailsViewModel model, EContractHistoryRaw2 raw)
        {
            model.EContracts = MapToEContractDto(raw.EContract);
            model.Vendor = MapToVendorDto(raw.Vendor);
            model.CustomerTaxCode = new CustomerTaxCodeDTO
            {
                Title = raw.EContract?.CusName ?? string.Empty,
                MaSoThue = raw.EContract?.CusTax ?? string.Empty,
                DiaChiCongTy = raw.EContract?.CusAddress ?? string.Empty
            };

            model.EContractDetails = raw.EContractDetails
                .Where(s => s.UsIN == "JOB_00001")
                .Select(d => MapToDetailDto(d))
                .ToList();

            if (raw.Jobs != null && raw.Jobs.Any())
            {
                model.JobDetail = raw.Jobs.Select(j =>
                {
                    var dto = new JobDetailDTO
                    {
                        OID = j.OID,
                        FactorID = j.FactorID,
                        Crt_Date = j.crt_date,
                        EntryID = j.EntryID,
                        EntryName = j.EntryName,
                        EmplName = j.EmplName,
                        InvcSign = j.InvcSign,
                        InvcFrm = j.InvcFrm,
                        InvcEnd = j.InvcEnd,
                        InvcSample = j.invcSample,
                        Descrip = j.Descrip,
                        DescriptChange = j.DescriptChange?.Replace("\n", "<br/>"),
                        ReferenceInfo = j.ReferenceInfo,


                        IsSave = j.IsSave,
                        IsDesignInvoices = j.isDesignInvoices,

                        CmpnName = raw.EContract?.CmpnName,
                        CusName = raw.EContract?.CusName,
                        CusTax = raw.EContract?.CusTax,
                        CusAddress = raw.EContract?.CusAddress,
                        CusEmail = raw.EContract?.CusEmail,
                        //IsTT78 = raw.EContract?.IsTT78 ?? false,
                        //IsCheckXHD = raw.EContract?.IsCheckXHD ?? false,
                        //IsShowCheckXHD = raw.EContract?.IsCheckXHD ?? false,
                        BankInfo = raw.EContract?.CusBankNumber,
                        PositionName = raw.EContract?.CusPosition_BySign,
                        IsshowYCCS = raw.EContract?.CurrSignNumb <= 0,
                        SignNumb = raw.EContract?.SignNumb
                    };
                    return dto;
                }).ToList();
            }
            var currentJobEntity = raw.Jobs.LastOrDefault();
            if (currentJobEntity != null)
            {
                model.Job = MapToJobDto(currentJobEntity);
            }
        }
        private EContractDTO? MapToEContractDto(EContractMaster? eContract)
        {
            if (eContract == null) return new EContractDTO();

            return new EContractDTO
            {
                OID = eContract.OID,
                CmpnName = eContract.CmpnName,
                CusName = eContract.CusName,
                CusTax = eContract.CusTax,
                CusAddress = eContract.CusAddress,
                CusEmail = eContract.CusEmail,
                CurrSignNumb = eContract.CurrSignNumb,
                IsTT78 = eContract.IsTT78,
                IsCheckXHD = eContract.IsCheckXHD,
                IsShowCheckXHD = eContract.IsCheckXHD, // Logic: isCheckXHD => isShowCheckXHD = true
                PositionName = eContract.CusPosition_BySign,
                BankInfo = eContract.CusBankNumber,
            };
        }
        private EContractDetailDTO MapToDetailDto(EContractDetails d)
        {
            int calculatedInvcEnd = (d.sl_KM > 0 && d.isKM)
                                    ? (d.ItemPerBox + d.sl_KM)
                                    : d.ItemPerBox;

            return new EContractDetailDTO
            {
                ItemID = d.ItemID,
                ItemName = d.ItemName,
                ItemUnit = d.ItemUnit,
                ItemPrice = d.ItemPrice,
                ItemQtty = d.ItemQtty,
                Sum_Amnt = d.Sum_Amnt,
                VAT_Rate = d.VAT_Rate,
                InvcSample = d.InvcSample,
                InvcSign = d.InvcSign,
                InvcFrm = d.InvcFrm,
                InvcEnd = calculatedInvcEnd, 
            };
        }
        private JobDTO MapToJobDto(JobEntity j)
        {
            if (j == null) return new JobDTO();

            return new JobDTO
            {
                OID = j.OID,
                ReferenceID = j.ReferenceID,
                FactorID = j.FactorID,
                EntryID = j.EntryID,
                InvcSign = j.InvcSign,
                InvcFrm = j.InvcFrm,
                InvcEnd = j.InvcEnd,
                InvcSample = j.invcSample,
                Descrip = j.Descrip
            };
        }
        private JobDetailDTO MapJobPostToDetailDto(JobPost jp)
        {
            return new JobDetailDTO
            {
                OID = jp.OID,
                FactorID = jp.FactorID,
                EntryID = jp.EntryID,
                EntryName = jp.EntryName,
                EmplName = jp.EmplName,
                DescriptChange = jp.DescriptChange,
                Crt_Date = jp.Crt_Date,
                //SignNumb = jp.SignNumb
            };
        }
        private VendorDTO MapToVendorDto(VendorEntity v)
        {
            if (v == null) return new VendorDTO();

            return new VendorDTO
            {
                CmpnID = v.cmpnID,
                VName = v.vName,
                Director = v.Director,
                Address = v.Address,
                TaxCode = v.TaxCode,
                BankInfo = v.BankInfo,
                PositionName = v.PositionName,
                //LogoPath = v.LogoPath,
                //SignPath = v.SignPath,
                Tel = v.Tel,
                Email = v.Website,
                Website = v.Website,
            };
        }
        public async Task HandleAutomaticJobAsync(EContractDetailsViewModel model, EContractHistoryRaw2 raw, string oid, string kt)
        {
            var hasJobTM = raw.Jobs.Any(s => s.FactorID == "JOB_00001");
            var hasJobKT = raw.Jobs.Any(s => s.FactorID == "JOB_00006");

            if (!hasJobTM && !hasJobKT)
            {
                var newJob = new JobEntity
                {
                    ReferenceID = oid,
                    FactorID = kt == "1" ? "JOB_00006" : "JOB_00001",
                    EntryID = kt == "1" ? "JB:012" : "JB:001",
                    Crt_User = raw.EContract.Crt_User,
                    cmpnID = raw.EContract.CmpnID
                };

                var insertedJob = await _eContractRepository.InsertJobAsync(newJob);
                model.Job = MapToJobDto(insertedJob);
                model.IsshowEntry = true;
            }
        }

        #endregion


        public async Task<PagedResponse<DepartmentDTO>> GetDepartmentsPagedAsync(string operDeptList, int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(operDeptList))
                return PagedResponse<DepartmentDTO>.Create(new List<DepartmentDTO>(), pageNumber, pageSize, 0);

            var ids = operDeptList.Replace(',', ';')
                                  .Split(';', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(x => x.Trim());

            var allResults = new List<DepartmentsEntity>();

            foreach (var id in ids)
            {
                var data = await _eContractRepository.GetDepartmentsByOidAsync(id);
                allResults.AddRange(data);
            }

            int totalRecords = allResults.Count;
            var pagedData = allResults.OrderBy(x => x.DID)
                                      .Skip((pageNumber - 1) * pageSize)
                                      .Take(pageSize)
                                      .Select(d => new DepartmentDTO
                                      {
                                          DID = d.DID,
                                          DNAME = d.DNAME,
                                          ParentID = d.ParentID,
                                          ROOM = d.ROOM,
                                      }).ToList();

            return PagedResponse<DepartmentDTO>.Create(pagedData, pageNumber, pageSize, totalRecords);
        }

        public async Task<ApiResponse<List<EContractDetailDTO>>> VerifyJobDetailsAsync(string cusTax, string oid)
        {
            try
            {
                var rawDetails = await _eContractRepository.VerifyJobAsync(cusTax, oid);
                var detailDtos = rawDetails.Select(d => MapToDetailDto(d)).ToList();
                return ApiResponse<List<EContractDetailDTO>>.SuccessResponse(detailDtos, "Xác thực Job thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<EContractDetailDTO>>.ErrorResponse("Có lỗi xảy ra trong quá trình xác thực.");
            }
        }

        public async Task<ApiResponse<List<string>>> UploadContractFilesAsync(
    IFormFileCollection files, string oid)
        {
            var fileLinks = new List<string>();
            string baseUrl = _configuration["FileConfig:BaseUrl"];

            foreach (var file in files)
            {
                string relativePath = await _fileStorageService.UploadFileAsync(file, oid);
                if (relativePath != null)
                {
                    string normalizedPath = relativePath.TrimStart('/')
                                                       .Replace("uploads/", "");
                    string fullUrl = $"{baseUrl.TrimEnd('/')}/files/{normalizedPath}";
                    fileLinks.Add(fullUrl);
                }
            }
            return ApiResponse<List<string>>.SuccessResponse(fileLinks, "Upload file thành công.");
        }

        public async Task<ApiResponse<object>> SaveJobAsync(SaveJobRequestDto request, string userCode)
        {
            if (string.IsNullOrEmpty(request.EContractOid) || string.IsNullOrEmpty(request.JobOid))
            {
                return ApiResponse<object>.ErrorResponse("EContractOid và JobOid không được để trống.");
            }

            var rawData = await _eContractRepository.GetEContractRawDataAsync(request.EContractOid);
            var master = rawData.EContract;

            if (master == null) return ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin hợp đồng.");

            int? nextCount = rawData.Jobs.Any(j => j.EntryID == "JB:005")
                             ? (rawData.Jobs.First(j => j.EntryID == "JB:005").CountChange + 1)
                             : 1;

            // Ghép chuỗi Info chuẩn theo logic cũ
            string info = request.Job.IsDesignInvoices
                ? $"{request.EmplName} đề nghị kiểm tra mẫu đã tạo {master.CusTax}-{master.CusName}-{master.CusAddress}"
                : $"{request.EmplName} {request.EntryName} {master.CusTax}-{master.CusName}-{master.CusAddress}";

            // Tính toán sumInvc
            int sumInvc = 0;
            foreach (var item in request.Details)
            {
                if (item.IsKM && item.InvcEnd != item.ItemPerBox)
                {
                    item.InvcEnd -= item.sl_KM;
                }
                sumInvc += item.InvcEnd;
            }
            var firstPack = request.Packs?.FirstOrDefault();
            try
            {
                var jobEntity = new JobEntity
                {
                    OID = request.JobOid, 
                    ReferenceID = request.EContractOid, 
                    FactorID = request.Job.FactorID,
                    EntryID = request.Job.EntryID,
                    TemplateID = request.Job.TemplateID,
                    OperDept = request.Job.OperDept,
                    isDesignInvoices = request.Job.IsDesignInvoices,
                    FileOther = request.Job.FileOther,
                    FileName0 = request.Job.FileName0,
                    FileName1 = request.Job.FileName1,
                    ChangeOption = request.Job.ChangeOption,
                    DescriptChange = request.Job.DescriptChange,
                    Crt_User = userCode,

                    InvcSign = firstPack?.InvcSign ?? string.Empty,
                    InvcFrm = firstPack?.InvcFrm ?? 0,
                    invcSample = firstPack?.InvcSample ?? string.Empty,
                    PackID = firstPack?.ItemID ?? string.Empty, 
                };

                var jobPackEntities = request.Packs.Select(p => new JobPackEntity
                {
                    ItemID = p.ItemID,
                    ItemNo = p.ItemNo,
                    InvcSign = p.InvcSign,
                    invcSample = p.InvcSample,
                    InvcFrm = p.InvcFrm,
                    InvcEnd = p.InvcEnd,
                    PublDate = p.PublDate,
                    Use_Date = p.Use_Date,
                    Descrip = p.Descrip 
                }).ToList();

                await _eContractRepository.UpdateJobSaveAsync(
                    jobEntity,
                    jobPackEntities,
                    sumInvc,
                    nextCount,
                    info,
                    request.Description);

                return ApiResponse<object>.SuccessResponse(null, "Lưu yêu cầu thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Lỗi khi lưu dữ liệu vào hệ thống.");
            }
        }

        public async Task<ApiResponse<object>> ApproveJobNowAsync(ApproveJobRequestDto request, string userCode, string fullName)
        {
            if (string.IsNullOrEmpty(request.Oid) || string.IsNullOrEmpty(request.CmpnId))
            {
                return ApiResponse<object>.ErrorResponse("Dữ liệu Oid và CmpnId không được để trống.");
            }

            var limit = await _eContractRepository.limitcn(request.CmpnId, userCode, "0000006");

            if (limit != null && limit.CONNO > limit.GIOIHANCN)
            {
                return ApiResponse<object>.SuccessResponse(new
                {
                    conNo = limit.CONNO.ToString("N0"),
                    gioiHanCN = limit.GIOIHANCN.ToString("N0")
                }, "Công nợ hiện tại đã vượt quá giới hạn cho phép. Vui lòng thanh toán.", 2);
            }

            var rawData = await _eContractRepository.GetEContractRawDataAsync(request.Oid);
            if (rawData.EContract == null || rawData.Jobs == null)
            {
                return ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin hợp đồng trong hệ thống.");
            }
            var master = rawData.EContract;
            master.SaleFullName = fullName;

            var jobDetailDec = rawData.Jobs.FirstOrDefault(j =>
                j.FactorID == "JOB_00001" && (j.EntryID == "JB:001" || j.EntryID == "JB:002"));

            if (jobDetailDec == null)
            {
                return ApiResponse<object>.ErrorResponse("Yêu cầu không thuộc bước tạo mẫu hoặc đã qua bước này.");
            }

            var zsEntity = new ZsgnEContractJob
            {
                FactorID = jobDetailDec.FactorID,
                OID = jobDetailDec.OID,
                ReferenceID = request.Oid, 
                ODate = DateTime.Now,
                CmpnID = rawData.EContract.CmpnID,
                Crt_User = userCode,
                EntryID = jobDetailDec.EntryID,
                AppvMess = $"{fullName} duyệt yêu cầu từ Portal",
                DataTbl = "EContractJobs",
                Variant30 = "1"
            };

            int currentHoldSign = 0;
            if (jobDetailDec.FactorID == "JOB_00001" && rawData.JobPosts.Any() && rawData.JobPosts[0].SignNumb == "100")
            {
                currentHoldSign = 100;
            }

            if (jobDetailDec.FactorID == "JOB_00003") currentHoldSign = 0;


            try
            {
                bool isSuccess = await _eContractRepository.ApproveContractJobAsync(zsEntity, currentHoldSign, 101);

                if (!isSuccess)
                {
                    return ApiResponse<object>.ErrorResponse("Duyệt thất bại. Trạng thái Job có thể đã thay đổi bởi người khác.");
                }

                var emailData = await _eContractRepository.GetEmailUserDeptAsync(jobDetailDec.OID);
                if (emailData?.EmailUserDept != null)
                {
                    await _mailService.SendApproveNotificationAsync(
                        emailData.EmailUserDept,
                        rawData.EContract,
                        request.Oid,
                        jobDetailDec.FactorID);
                }

                return ApiResponse<object>.SuccessResponse(null, "Duyệt yêu cầu thành công!");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse($"Lỗi hệ thống khi thực thi duyệt: {ex.Message}");
            }
        }

        public async Task<EContractsViewModel> GetContractDetailForDisplayAsync(string oid, string userCode, string grpList, string firstClaimValue)
        {
            var resultRaw = await _eContractRepository.GetEContractRawDataAsync(oid);
            if (resultRaw == null)
            {
                return null;
            }
            var result = new EContractsViewModel
            {
                EContracts = new EContractDTO
                {
                    OID = resultRaw.EContract.OID,
                    CusName = resultRaw.EContract.CusName,
                    CmpnTax = resultRaw.EContract.CmpnTax,
                    CmpnName = resultRaw.EContract.CmpnName,
                    CmpnAddress = resultRaw.EContract.CmpnAddress,
                    ODate = resultRaw.EContract.ODate,
                    CurrSignNumb = resultRaw.EContract.CurrSignNumb,
                    IsTT78 = resultRaw.EContract.IsTT78,
                    Date_BusLicence = DateTime.TryParse(resultRaw.EContract.Date_BusLicence, out var date) ? date : null,
                    RefeContractDate = resultRaw.EContract.RefeContractDate,
                    CusPeople_Sign = resultRaw.EContract.CusPeople_Sign,
                    CusPosition_BySign = resultRaw.EContract.CusPosition_BySign,
                    Description = resultRaw.EContract.Descrip,
                    CusTax = resultRaw.EContract.CusTax,
                    CusTel = resultRaw.EContract.CusTel,
                    CusAddress = resultRaw.EContract.CusAddress,
                    CusEmail = resultRaw.EContract.CusEmail,
                    CusBankAddress = resultRaw.EContract.CusBankAddress,
                    CusBankNumber = resultRaw.EContract.CusBankNumber,
                    BankInfo = $"{resultRaw.EContract.CusBankNumber} - {resultRaw.EContract.CusBankAddress}",
                    PositionName = resultRaw.EContract.CusPosition_BySign
                },

                JobDetail = resultRaw.Jobs.Select(j => new JobDetailDTO
                {
                    OID = j.OID,
                    EntryID = j.EntryID,
                    FactorID = j.FactorID,
                    InvcSign = j.InvcSign,
                    InvcFrm = (int)j.InvcFrm, 
                    InvcEnd = (int)j.InvcEnd,
                    ReferenceInfo = j.ReferenceInfo,
                    Descrip = j.Descrip,
                    FileInvoice = j.FileInvoice, 
                    FileLogo = j.FileLogo, 
                }).ToList(),

                JobPost = resultRaw.JobPosts, 
                EContractDetails = resultRaw.EContractDetails,

                Vendor = new VendorDTO
                {
                    VName = resultRaw.Vendor?.vName,
                    TaxCode = resultRaw.Vendor?.TaxCode,
                    CmpnID = resultRaw.Vendor?.cmpnID,
                    Address = resultRaw.Vendor?.Address,
                    Tel = resultRaw.Vendor?.Tel,
                    Email = resultRaw.Vendor?.Email,
                    Website = resultRaw.Vendor?.Website,
                    PositionName = resultRaw.Vendor?.CmpName_Sign,
                    Director = resultRaw.Vendor?.Director,
                },

                TemplateEcontract = resultRaw.TemplateEcontract,
                ECtr_PublicInfo = resultRaw.ECtr_PublicInfo,
                EmailUser = resultRaw.EmailUser
            };

            if (result.EContracts != null)
            {
                if (result.EContracts.IsTT78 && result.JobPost != null)
                {
                    result.ycTK = result.JobPost.Any(s => s.FactorID == "JOB_00003" && s.EntryID == "JB:003");
                    result.ycTM = result.JobPost.Any(s => s.FactorID == "JOB_00001" && s.EntryID == "JB:001");
                    result.ycDaTM = result.JobPost.Any(s => s.FactorID == "JOB_00001" && s.EntryID == "JB:001" && s.SignNumb == "301");
                    result.ycPH = result.JobPost.Any(s => s.FactorID == "JOB_00002" && s.EntryID == "JB:004");
                    result.ycDaTK = result.JobPost.Any(s => s.FactorID == "JOB_00003" && s.EntryID == "JB:003" && s.SignNumb == "201");
                }
                result.EContracts.Date_BusLicenceFormat = result.EContracts.Date_BusLicence?.ToString("dd/MM/yyyy").Replace("-", "/");
                if (result.EContracts.Date_BusLicenceFormat == "01/01/0001") result.EContracts.Date_BusLicenceFormat = "25/12/2021";

                var urlsign = $"contract;b;{result.EContracts.CmpnTax};{firstClaimValue};{result.EContracts.OID};125.212.205.139;bos;nghe!v@ng2011";
                result.UrlRequest = Sha1.Encrypt(urlsign);
            }

            if (resultRaw.ListFiles != null)
            {
                result.ListFiles = resultRaw.ListFiles.Select(f => new ERP_Portal_RC.Application.DTOs.ListFile
                {
                    AttachFile = f.AttachFile,
                    Crt_User = f.Crt_User,
                    LinkFonder = f.LinkFonder,
                    isdisable = (f.Crt_User != userCode),
                    url = string.IsNullOrEmpty(f.LinkFonder)
                        ? $"{_fileConfig.FileUpload}{f.AttachFile}"
                        : $"{_fileConfig.FileUpload}{f.LinkFonder}/{f.AttachFile}"
                }).ToList();
            }

            var groups = grpList.Replace("'", "").Replace("[", "").Replace("]", "").Split(',');
            result.IsshowReturnSign = groups.Any(g => g == "000.000.2102821152" || g == "00006.00063.00121");

            return result;
        }

        public async Task<bool> CheckIfSubmitted(string oid)
        {
            return await _eContractRepository.CheckIfSubmitted(oid);
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetListFilesByOidAsync(string oid)
        {
            string cleanedOid = System.Net.WebUtility.UrlDecode(oid).Trim();

            var files = await _eContractRepository.GetDocAttachFilesAsync(cleanedOid);

            if (files == null || !files.Any())
            {
                return ApiResponse<IEnumerable<object>>.ErrorResponse("Không tìm thấy file đính kèm nào.");
            }

            return ApiResponse<IEnumerable<object>>.SuccessResponse(files, "Lấy danh sách file thành công.");
        }

        public async Task<ApiResponse<string>> GetNextJobOIDAsync(string mainOid)
        {
            if (string.IsNullOrEmpty(mainOid))
                return ApiResponse<string>.ErrorResponse("OID gốc không được để trống.");
            try
            {
                var nextOid = await _eContractRepository.GetNextJobOIDAsync(mainOid);
                return ApiResponse<string>.SuccessResponse(nextOid, "Lấy OID tiếp theo thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse($"Lỗi khi lấy OID: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<string>> CreateJobAsync(InsertJobRequest request)
        {
            try
            {
                var newId = await _eContractRepository.InsertJobFullAsync(request);

                if (string.IsNullOrEmpty(newId))
                    return ApiResponse<string>.ErrorResponse("Không thể tạo Job mới", 400);

                return ApiResponse<string>.SuccessResponse(newId, "Tạo Job thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<JobStatusResponse>> GetJobStatusAsync(string referenceId, string factorId, string entryId)
        {
            try
            {
                var status = await _eContractRepository.CheckJobStatusAsync(referenceId, factorId, entryId);
                if (status == null)
                    return ApiResponse<JobStatusResponse>.SuccessResponse(null, "Chưa từng tồn tại yêu cầu này");

                return ApiResponse<JobStatusResponse>.SuccessResponse(status, "Lấy trạng thái thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<JobStatusResponse>.ErrorResponse(ex.Message, 500);
            }
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetAttachmentsByOidAsync(string oid)
        {
            var rawFiles = await _eContractRepository.GetRawAttachmentsByOidAsync(oid);
            var baseUrl = _configuration["FileConfig:BaseUrl"];
            var formattedFiles = rawFiles.Select(f => new {
                AttachID = f.AttachID,
                FileName = f.AttachFile,
                Note = f.AttachNote,
                AttachDate = f.AttachDate,
                ViewUrl = $"{baseUrl}/{f.LinkFile}" 
            });
            return ApiResponse<IEnumerable<object>>.SuccessResponse(formattedFiles, "Lấy danh sách file thành công.");
        }

        public async Task<bool> CreateOrderAsync(
            EContractIntegrationRequestDto model,
            string merchantId,
            string orderOid,
            string crtUser)
        {
            var prdcAmnt = model.Details?.Sum(d => d.ItemAmnt) ?? 0;
            var vatAmnt = model.Details?.Sum(d => d.VAT_Amnt) ?? 0;
            var sumAmnt = model.Details?.Sum(d => d.Sum_Amnt) ?? 0;
            var vatRate = model.Details?.FirstOrDefault()?.VAT_Rate ?? 0; 

            var entity = new EContractIntegrationRequest
            {
                OID = model.OrderOID,
                // Header
                MyCmpnID = model.MyCmpnID,
                MyCmpnName = model.MyCmpnName,
                MyCmpnTax = model.MyCmpnTax,
                MyCmpnAddress = model.MyCmpnAddress,
                MyCmpnMail = model.MyCmpnMail,
                MyCmpnTel  = model.MyCmpnTel,
                MyCmpnContactAddress = model.MyCmpnContactAddress,
                MyCmpnPeople_Sign = model.MyCmpnPeople_Sign,
                MyCmpnPosition_Sign = model.MyCmpnPosition_Sign,
                MyCmpnBankNumber = model.CusBankNumber,
                MyCmpnBankAddress = model.MyCmpnBankAddress,
                SaleEmID = model.SaleEmID,
                CusName = model.CusName,
                CusTax = model.CusTax,
                CusAddress = model.CusAddress,
                CusEmail = model.CusEmail,
                CusTel = model.CusTel,
                CusPeople_Sign = model.CusPeople_Sign,
                CusPosition_BySign = model.CusPosition_BySign,
                CusBankAddress = model.CusBankAddress,
                CusBankNumber = model.CusBankNumber,
                SampleID = model.SampleID,
                Descrip = model.Descrip,

                // Tổng tự tính
                PrdcAmnt = prdcAmnt,
                VAT_Rate = vatRate,
                VAT_Amnt = vatAmnt,
                Sum_Amnt = sumAmnt,

                ODate = model.ODate,
                SignDate = model.SignDate,
                HtmlContent = model.HtmlContent,
                OidContract = model.OidContract,
                RefeContractDate = model.RefeContractDate,
                IsCapBu = model.IsCapBu,
                IsGiaHan = model.IsGiaHan,
                IsTT78 = model.IsTT78,
                IsOnline = model.IsOnline,

                // Details
                Details = model.Details?.Select(d => new EContractDetailDto_Incom
                {
                    ItemID = d.ItemID,
                    ItemName = d.ItemName,
                    ItemUnit = d.ItemUnit,
                    ItemPrice = d.ItemPrice,
                    ItemQtty = d.ItemQtty,
                    ItemAmnt = d.ItemAmnt,
                    VAT_Rate = d.VAT_Rate,
                    VAT_Amnt = d.VAT_Amnt,
                    Sum_Amnt = d.Sum_Amnt,
                    Descrip = d.Descrip,
                    InvcSample = d.InvcSample,
                    InvcSign = d.InvcSign,
                    InvcFrm = d.InvcFrm,
                    InvcEnd = d.InvcEnd,
                }).ToList()
            };

            return await _eContractRepository.InsertOrderBasicAsync(entity, merchantId, orderOid, crtUser);
        }

        public async Task<bool> OrderExistsAsync(string orderOid)
        {
            return await _eContractRepository.OrderExistsAsync(orderOid);
        }

        public async Task<OwnerContract> GetOwnerContractAsync(string companyId = "26")
        {
            return await _eContractRepository.GetOwnerContractAsync(companyId);
        }

        public async Task<bool> CheckOrderBySaleAsync(string cusTax, string saleEmID)
        {
            return await _eContractRepository.CheckOrderBySaleAsync(cusTax, saleEmID);
        }

        public async Task<ApiResponse<DeXuatCapTaiKhoanResponseDto>> DeXuatCapTaiKhoanAsync(DeXuatCapTaiKhoanRequestDto request)
        {
            try
            {
                var entity = new ProposeCreateAccount
                {
                    OIDContract = request.OIDContract,
                    CmpnID = request.CmpnID,
                    CrtUser = request.CrtUser,
                    MailAcc = request.MailAcc
                };
                DeXuatCapTaiKhoanResult result = await _eContractRepository.DeXuatAsync(entity);
                var data = new DeXuatCapTaiKhoanResponseDto
                {
                    OIDJob = result.OIDJob,
                    ReferenceInfo = result.ReferenceInfo
                };

                string message = result.IsAlreadyExists
                    ? "Yêu cầu cấp tài khoản đã được tạo trước đó."
                    : "Đề xuất cấp tài khoản thành công. Job đã được trình ký (trạng thái 101).";
                return ApiResponse<DeXuatCapTaiKhoanResponseDto>.SuccessResponse(data, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<DeXuatCapTaiKhoanResponseDto>.ErrorResponse(
                    ex.Message,
                    statusCode: 400);
            }
        }

        public async Task<ApiResponse<InvCounterResponseDto>> GetInvCounterByMSTAsync(
            InvCounterRequestDto request)
        {
            // ── 1. Tìm server chứa MST ───────────────────────────────────────
            var serverIp = _connectionRepo.GetIPServerByMST(request.MST, null, "EVAT");
            if (string.IsNullOrEmpty(serverIp))
                return ApiResponse<InvCounterResponseDto>.ErrorResponse(
                    $"Không tìm thấy server cho MST: {request.MST}", 404);

            // ── 2. Lấy connection string đến server đó ───────────────────────
            var connStr = _connectionRepo.GetCnServerByMST(request.MST, null, "EVAT");
            if (string.IsNullOrEmpty(connStr))
                return ApiResponse<InvCounterResponseDto>.ErrorResponse(
                    $"Không thể kết nối server cho MST: {request.MST}", 500);

            var merchantId = await _eContractRepository.GetMerchantIdAsync(connStr, request.MST);
            if (string.IsNullOrEmpty(merchantId))
                return ApiResponse<InvCounterResponseDto>.ErrorResponse(
                    $"Không tìm thấy MerchantId cho MST: {request.MST}", 404);

            var frmDate = new DateTime(1990, 1, 1);  
            var toDate = DateTime.Today;

            var counter = await _eContractRepository.GetInvCounterAsync(connStr, merchantId, frmDate, toDate);
            if (counter == null)
                return ApiResponse<InvCounterResponseDto>.ErrorResponse(
                    "Không lấy được dữ liệu thống kê hóa đơn.", 500);

            var data = new InvCounterResponseDto
            {
                MST = request.MST,
                MerchantId = merchantId,
                Server = serverIp,
                Used = counter.Used,
                Total = counter.Total,
                Remaining = counter.Remaining
            };

            return ApiResponse<InvCounterResponseDto>.SuccessResponse(
                data, "Lấy thống kê hóa đơn thành công.");
        }
    }
}



