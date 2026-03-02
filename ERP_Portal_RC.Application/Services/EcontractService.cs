using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Enum;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security;
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

        public EcontractService(IEContractRepository econtractRepo, IMailService mailService)
        {
            _eContractRepository = econtractRepo;
            _mailService = mailService;
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
                CurrSignDate = h.currSignDate
            }).ToList();

            foreach (var item in historyList)
            {
                switch (item.CurrSignNum)
                {
                    case StatusSignnum.TRINH_KY: // "0"
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

                    case StatusSignnum.CHO_KIEM_TRA: // "101"
                        item.CurrSignNum = "Đề xuất ký";
                        item.AppvMess = "Trình ký";
                        break;

                    case StatusSignnum.CHO_GD_DUYEN: // "201"
                        item.CurrSignNum = "Trình ký Giám đốc";
                        item.FullName = "Hợp đồng trình ký giám đốc";
                        item.AppvMess = "Trình ký";
                        break;

                    case StatusSignnum.HD_DA_DUYET: // "301"
                        item.CurrSignNum = "Hợp đồng đã ký";
                        item.FullName = "Hợp đồng đã ký";
                        item.AppvMess = "OK";
                        break;

                    case StatusSignnum.KH_DA_KY: // "501"
                        item.CurrSignNum = "Hợp đồng đã được khách hàng ký";
                        item.FullName = "Khách hàng";
                        item.AppvMess = "OK";
                        break;

                    case StatusSignnum.HD_DONG: // "1001"
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
                foreach (var job in raw.Jobs)
                {
                    // Kiểm tra điều kiện loại biên (Job đã hoàn tất thì bỏ qua)
                    if (job.currSignNumb == (int)CurrSignNum.TRA_VE ||
                        job.currSignNumb == (int)CurrSignNum.TRA_VE200 ||
                        job.currSignNumb == (int)CurrSignNum.TRA_VE300)
                        continue;

                    var targetHistory = historyList.FirstOrDefault(h =>
                        h.OID == job.OID && h.AppvMess == "Trình ký");

                    if (targetHistory != null)
                    {
                        // Sử dụng JobFactorID và JobEntry constants để so sánh
                        // Logic Job 004: Thay đổi thông tin (JB:008)
                        if (job.FactorID == JobFactor.JOB_00004.ToString() && job.EntryID == JobEntry.JB008)
                        {
                            targetHistory.CancelDescript = !string.IsNullOrEmpty(job.DescriptChange)
                                ? $"Lý do : {job.Reason} - {job.DescriptChange}"
                                : $"Lý do : {job.Reason}";
                        }
                        // Logic Job 005: Báo xuất hóa đơn / Thanh toán
                        else if (job.FactorID == JobFactor.JOB_00005.ToString())
                        {
                            if (!string.IsNullOrEmpty(job.DescriptChange))
                            {
                                targetHistory.CancelDescript = "Ghi chú bổ sung : " + job.DescriptChange;
                            }
                        }
                    }
                }
            }

            response.HistoryList = historyList;
            return ApiResponse<EContractHistoryResponse>.SuccessResponse(response, "Lấy lịch sử thành công.");
        }
    }
}


