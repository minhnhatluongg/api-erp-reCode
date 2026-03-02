using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Linq;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Interfaces;
using ERP_Portal_RC.Application.DTOs;

namespace Interface.ReleaseInvoice.Services
{
    public class InvoicePreviewService : IInvoicePreviewService
    {
        private readonly ITemplateRepository _templateRepo;
        private readonly IRuleRepository _ruleRepo;
        private readonly IXmlDataRepository _Repo;
        private readonly IContractCheckRepository _checkEcontract;

        private const string BASELINK_VIEN = "http://cdn.evat.vn/imgs/";

        public InvoicePreviewService(
            ITemplateRepository templateRepo,
            IContractCheckRepository checkContractRepository,
            IXmlDataRepository xmlDataRepository,
            IRuleRepository ruleRepo)
        {
            _templateRepo = templateRepo;
            _ruleRepo = ruleRepo;
            _Repo = xmlDataRepository;
            _checkEcontract = checkContractRepository;
        }

        public async Task<string> BuildInvoiceHtmlAsync(InvoiceBuildRequest req)
        {
            string xmlDataCode = "einvoice_template_tax"; 
            
            if (req.Options.IsVCNB)
            {
                xmlDataCode = "einvoice_template_tax78_VCNB";
            }
            else if (req.Options.IsTaxDocument)
            {
                xmlDataCode = "sys_template_TNCN_ND70";
            }
            else if (req.Options.isHangGuiDaiLy)
            {
                xmlDataCode = "einvoice_template_tax78_HGDL";
            }
            else
            {
                Console.WriteLine("[DEBUG] Hóa đơn thông thường, sử dụng XML: " + xmlDataCode);
            }

            var xmlRecord = await _Repo.GetByCodeAsync(xmlDataCode);
            if (xmlRecord == null) throw new Exception($"Không tìm thấy cấu trúc XML: {xmlDataCode}");

            string rawXml = Decode(xmlRecord.XmlContent);
            
            // Chọn hàm GenerateXml phù hợp với loại hóa đơn
            string finalXml;
            if (req.Options.IsVCNB)
                finalXml = GenerateXmlData_VCNB(req, rawXml);
            else if (req.Options.IsTaxDocument)
                finalXml = GenerateXmlData_TaxDocument(req, rawXml);
            else if (req.Options.isHangGuiDaiLy)
                finalXml = GenerateXmlData_HGDL(req, rawXml);
            else
                finalXml = GenerateXmlData(req, rawXml); // Hóa đơn thông thường

            // --- PHẦN 2: XỬ LÝ GIAO DIỆN (XSLT + RULES) ---
            string rawXslt = "";
            // 2.1. Kiểm tra HÓA ĐƠN ĐẶC BIỆT (có tích checkbox "Hóa đơn được cấu hình sẵn")
            bool isSpecialInvoice = req.Options.IsVCNB || req.Options.IsTaxDocument || req.Options.isHangGuiDaiLy;
            
            if (!string.IsNullOrEmpty(req.Options.CustomXsltContent))
            {
                rawXslt = req.Options.CustomXsltContent;
            }
            else if (isSpecialInvoice)
            {
                string xsltFileName = DetermineSpecialXsltFileName(req.Options);
                var specialTemplate = await _templateRepo.GetByFileNameAsync(xsltFileName);
                
                if (specialTemplate == null)
                {
                    specialTemplate = await _templateRepo.GetByFileNameAsync(xsltFileName + ".xslt");
                }
                
                if (specialTemplate == null)
                    throw new Exception($"Không tìm thấy mẫu đặc biệt cho loại hóa đơn này! " +
                        $"Đã tìm: '{xsltFileName}' và '{xsltFileName}.xslt' trong bảng InvoiceTemplates. " +
                        $"Vui lòng kiểm tra trường FileName trong DB có khớp không.");
                
                rawXslt = Decode(specialTemplate.InvoiceContent);
            }
            else
            {
                var template = await _templateRepo.GetByIdAsync(req.TemplateId);
                
                if (template == null)
                    throw new Exception($"Không tìm thấy mẫu hóa đơn (TemplateId: {req.TemplateId})!");
                rawXslt = Decode(template.InvoiceContent);
            }

            var rules = await _ruleRepo.GetAllActiveRulesAsync();

            // 2.2.5. REPLACE PLACEHOLDERS TRONG XSLT (giống tool cũ)
            rawXslt = ReplaceXsltPlaceholders(rawXslt, req);
            // 2.3. ÁP DỤNG TẤT CẢ RULES (Cố định & Custom từ React/Tool)
            string finalXslt = ApplyLegacyAndCustomRules(rawXslt, rules, req);
            // --- PHẦN 3: TẠO HTML ---
            string htmlOutput = XsltTransform(finalXml, finalXslt);
            // --- PHẦN 4: POST-PROCESS HTML (Replace placeholders còn lại) ---
            htmlOutput = ReplaceHtmlPlaceholders(htmlOutput, req);
            // --- PHẦN 5: FALLBACK - Inject CSS vào HTML nếu chưa có trong XSLT ---
            if (req.Options?.AdjustConfig != null)
            {
                string customCss = CompileCustomCss(req.Options.AdjustConfig, BASELINK_VIEN);
                
                if (!string.IsNullOrEmpty(customCss))
                {
                    // CSS chưa được inject vào XSLT, inject trực tiếp vào HTML
                    if (htmlOutput.Contains("</style>"))
                    {
                        int lastStylePos = htmlOutput.LastIndexOf("</style>");
                        htmlOutput = htmlOutput.Insert(lastStylePos, "\n/* Custom CSS Injected */\n" + customCss + "\n");
                    }
                }
                
                // QUAN TRỌNG: Thêm class "vienhd" vào <div id="main"> để CSS viền có hiệu lực
                if (req.Options.AdjustConfig.IsThayDoiVien && htmlOutput.Contains("<div id=\"main\" class=\"container\">"))
                {
                    htmlOutput = htmlOutput.Replace(
                        "<div id=\"main\" class=\"container\">", 
                        "<div id=\"main\" class=\"container vienhd\">");
                }
            }
            
            return htmlOutput;
        }
        private string DetermineSpecialXsltFileName(InvoiceConfigDto options)
        {
            if (options.IsVCNB)
                return "VCNB_New.xslt"; 
            
            if (options.IsTaxDocument)
                return "TNCN_70.xslt"; 
            
            if (options.isHangGuiDaiLy)
                return "HGDL_TT78.xslt"; 
            throw new Exception("Không xác định được loại hóa đơn đặc biệt!");
        }

        private string CompileCustomCss(AdjustConfigDto config, string baseLink)
        {
            if (config == null) return string.Empty;
            var sbCss = new StringBuilder();
            
            // 1. CSS VIỀN (Thay đổi viền)
            if (config.IsThayDoiVien && config.VienConfig != null && !string.IsNullOrEmpty(config.VienConfig.SelectedVien))
            {
                string urlImage = baseLink + config.VienConfig.SelectedVien;
                sbCss.Append($"\n.vienhd,.page{{border-spacing: 0px!important;");
                sbCss.Append($"border: 22px solid transparent!important;");
                sbCss.Append($"border-image: url('{urlImage}') {config.VienConfig.DoManh}% round!important;}}");
            }
            else
            {
                sbCss.Append("\n.vienhd,.page{}");
            }
            
            // 2. CSS ẨN/HIỆN (Fax, Email, Website, SĐT, Bank, Song ngữ)
            if (!config.IsSongNgu) sbCss.Append("\n.en{display: none;}");
            else sbCss.Append("\n.en{}");

            if (!config.IsSoDT) sbCss.Append("\n#_NBSDT{display: none;}");
            else sbCss.Append("\n#_NBSDT{}");

            if (!config.IsFax) sbCss.Append("\n#_NBFax{display: none;}");
            else sbCss.Append("\n#_NBFax{}");

            if (!config.IsEmail) sbCss.Append("\n#_NBEmail{display: none;}");
            else sbCss.Append("\n#_NBEmail{}");

            if (!config.IsTaiKhoanNganHang) sbCss.Append("\n#_NBSTK{display: none;}");
            else sbCss.Append("\n#_NBSTK{}");

            if (!config.IsWebsite) sbCss.Append("\n#_NBWebsite{display: none;}");
            else sbCss.Append("\n#_NBWebsite{}");

            // 3. CSS VỊ TRÍ & KÍCH THƯỚC LOGO/BACKGROUND
            if (config.LogoPos != null)
            {
                if (config.LogoPos.Width > 0) 
                    sbCss.Append($"\n.invoice_logo{{width:{config.LogoPos.Width}px!important;}}");
                if (config.LogoPos.Top != 0) 
                    sbCss.Append($"\n.invoice_logo{{top:{config.LogoPos.Top}%!important;}}");
                if (config.LogoPos.Left != 0) 
                    sbCss.Append($"\n.invoice_logo{{left:{config.LogoPos.Left}%!important;}}");
            }

            if (config.BackgroundPos != null)
            {
                if (config.BackgroundPos.Width > 0) 
                    sbCss.Append($"\n.invoice_background{{width:{config.BackgroundPos.Width}px!important;}}");
                if (config.BackgroundPos.Top != 0) 
                    sbCss.Append($"\n.invoice_background{{top:{config.BackgroundPos.Top}%!important;}}");
            }
            return sbCss.ToString();
        }
        public async Task<InvoiceSampleResult> BuildSampleFilesAsync(InvoiceBuildRequest req, string saleId)
        {
            var cusTax = req.Company?.MerchantID;
            var invcSample = req.Company?.SampleSerial;
            var invcSerial = req.Company?.SampleID;
            var checkEcontract = await _checkEcontract.CheckContractAsync(cusTax, invcSample, invcSerial);

            var existingContract = checkEcontract.FirstOrDefault();

            if (existingContract == null)
            {
                throw new Exception("Mã số thuế này chưa có hợp đồng, vui lòng tạo hợp đồng!.");
            }
            else
            {
                if (existingContract.Crt_User != saleId)
                {
                    throw new Exception("Hợp đồng này được tạo bởi người dùng khác. Bạn không có quyền xác nhận mẫu.");
                }
            }

            // === KHAI BÁO BIẾN LƯU TÊN FILE ===
            string resultXsltFileName = "";
            string resultXmlFileName = $"{invcSample}_{invcSerial}.xml"; // Mặc định tên XML theo Mẫu số_Ký hiệu
            string resultLogoFileName = req.LogoFileName ?? "logo.png"; // Lấy từ Request hoặc mặc định
            string resultBgFileName = req.BackgroundFileName ?? "background.png"; // Lấy từ Request hoặc mặc định

            // --- PHẦN 1: XỬ LÝ XML DATA ---
            string xmlDataCode = "einvoice_template_tax";

            if (req.Options.IsVCNB)
            {
                xmlDataCode = "einvoice_template_tax78_VCNB";
                resultXmlFileName = $"VCNB_{invcSample}_{invcSerial}.xml"; // Đặt tên riêng cho VCNB
            }
            else if (req.Options.IsTaxDocument)
            {
                xmlDataCode = "sys_template_TNCN_ND70";
                resultXmlFileName = $"TNCN_{invcSample}_{invcSerial}.xml"; // Đặt tên riêng cho TNCN
            }
            else if (req.Options.isHangGuiDaiLy)
            {
                xmlDataCode = "einvoice_template_tax78_HGDL";
                resultXmlFileName = $"HGDL_{invcSample}_{invcSerial}.xml"; // Đặt tên riêng cho HGDL
            }

            var xmlRecord = await _Repo.GetByCodeAsync(xmlDataCode);
            if (xmlRecord == null) throw new Exception($"Không tìm thấy cấu trúc XML: {xmlDataCode}");

            string rawXmlTemplate = Decode(xmlRecord.XmlContent);

            // Chọn hàm GenerateXml phù hợp
            string finalXml;
            if (req.Options.IsVCNB)
                finalXml = GenerateXmlData_VCNB(req, rawXmlTemplate);
            else if (req.Options.IsTaxDocument)
                finalXml = GenerateXmlData_TaxDocument(req, rawXmlTemplate);
            else if (req.Options.isHangGuiDaiLy)
                finalXml = GenerateXmlData_HGDL(req, rawXmlTemplate);
            else
                finalXml = GenerateXmlData(req, rawXmlTemplate);

            // --- PHẦN 2: XỬ LÝ GIAO DIỆN (XSLT + RULES) ---
            string rawXslt = "";
            bool isSpecialInvoice = req.Options.IsVCNB || req.Options.IsTaxDocument || req.Options.isHangGuiDaiLy;

            if (!string.IsNullOrEmpty(req.Options.CustomXsltContent))
            {
                // Case 1: User upload XSLT custom
                rawXslt = req.Options.CustomXsltContent;
                // Lấy tên file từ request nếu có, hoặc đặt tên mặc định
                resultXsltFileName = !string.IsNullOrEmpty(req.XsltFileName) ? req.XsltFileName : "Custom_Template.xslt";
            }
            else if (isSpecialInvoice)
            {
                // Case 2: Mẫu đặc biệt (VCNB, TNCN...)
                string xsltFileName = DetermineSpecialXsltFileName(req.Options);
                var specialTemplate = await _templateRepo.GetByFileNameAsync(xsltFileName);

                if (specialTemplate == null)
                    throw new Exception($"Không tìm thấy mẫu đặc biệt: {xsltFileName}");

                rawXslt = Decode(specialTemplate.InvoiceContent);

                // [LOGIC MỚI] Gán tên file từ template đặc biệt
                resultXsltFileName = specialTemplate.FileName ?? xsltFileName;
            }
            else
            {
                // Case 3: HÓA ĐƠN BÌNH THƯỜNG (TemplateId)
                var template = await _templateRepo.GetByIdAsync(req.TemplateId);

                if (template == null)
                    throw new Exception($"Không tìm thấy mẫu hóa đơn (TemplateId: {req.TemplateId})!");

                rawXslt = Decode(template.InvoiceContent);

                // [LOGIC MỚI] Gán tên file từ DB Template
                resultXsltFileName = template.FileName ?? $"Template_{req.TemplateId}.xslt";
            }

            // 2.1. Lấy Rules
            var rules = await _ruleRepo.GetAllActiveRulesAsync();

            // 2.2. Replace Placeholders
            string xsltWithPlaceholdersReplaced = ReplaceXsltPlaceholders(rawXslt, req);

            // 2.3. Áp dụng Rules
            string finalXslt = ApplyLegacyAndCustomRules(xsltWithPlaceholdersReplaced, rules, req);

            // --- PHẦN 3: TẠO HTML ---
            string htmlOutput = XsltTransform(finalXml, finalXslt);

            // --- PHẦN 4: POST-PROCESS HTML ---
            htmlOutput = ReplaceHtmlPlaceholders(htmlOutput, req);

            // --- PHẦN 5: FALLBACK CSS ---
            if (req.Options?.AdjustConfig != null)
            {
                string customCss = CompileCustomCss(req.Options.AdjustConfig, BASELINK_VIEN);

                if (!string.IsNullOrEmpty(customCss))
                {
                    if (htmlOutput.Contains("</style>"))
                    {
                        int lastStylePos = htmlOutput.LastIndexOf("</style>");
                        htmlOutput = htmlOutput.Insert(lastStylePos, "\n/* Custom CSS Injected */\n" + customCss + "\n");
                    }
                }
                if (req.Options.AdjustConfig.IsThayDoiVien && htmlOutput.Contains("<div id=\"main\" class=\"container\">"))
                {
                    htmlOutput = htmlOutput.Replace(
                        "<div id=\"main\" class=\"container\">",
                        "<div id=\"main\" class=\"container vienhd\">");
                }
            }

            // --- TRẢ VỀ KẾT QUẢ KÈM FILE NAME ---
            return new InvoiceSampleResult
            {
                FinalXmlData = finalXml,
                RawXsltContent = rawXslt,
                ConfiguredXslt = finalXslt,
                FinalHtmlOutput = htmlOutput,

                // [LOGIC MỚI] Bổ sung các tên file để trả về FE/Odoo
                XsltFileName = resultXsltFileName,
                XmlFileName = resultXmlFileName,
                LogoFileName = resultLogoFileName,
                BackgroundFileName = resultBgFileName
            };
        }
        private string ApplyLegacyAndCustomRules(string rawXslt, Dictionary<string, string> rules, InvoiceBuildRequest req)
        {
            var logMessages = new List<string>();
            string xslt = rawXslt;
            int replaceCount = 0;
            int injectionCount = 0;

            try
            {
                var options = req.Options;
                
                // --- KHỞI TẠO BIẾN TỪ RULES CỐ ĐỊNH (Sử dụng null làm mặc định an toàn) ---
                string baseCssRules = rules.ContainsKey("Css") ? rules["Css"] : "";
                string issuedDateContent = rules.ContainsKey("ClassissuedDate") ? rules["ClassissuedDate"] : null;
                string strFooter = options.IsVCNB && rules.ContainsKey("tblFooterVCNB") ? rules["tblFooterVCNB"] : (rules.ContainsKey("tblFooter78") ? rules["tblFooter78"] : null);
                string filenoteFooter = rules.ContainsKey("noteFooter78") ? rules["noteFooter78"] : null;
                string DTS = rules.ContainsKey("DTS") ? rules["DTS"] : null;
                string attribute = rules.ContainsKey("attribute") ? rules["attribute"] : null;

                // Các biến cần bảo vệ chống lỗi zero length
                string STKNMuaOld = rules.ContainsKey("STKNMuaOld") ? rules["STKNMuaOld"] : null;
                string STKNMuaOldv1 = rules.ContainsKey("STKNMua_v1") ? rules["STKNMua_v1"] : null;
                string STKNMuaNew = rules.ContainsKey("STKNMuaNew") ? rules["STKNMuaNew"] : "";
                string HDM = rules.ContainsKey("ImageHDM") ? rules["ImageHDM"] : null;
                string HDM_v1 = rules.ContainsKey("ImageHDM_V1") ? rules["ImageHDM_V1"] : null;

                // --- BƯỚC 1: XÓA BIẾN XSLT GỐC VÀ CHUẨN HÓA XPATH/LOGIC (CONVERT SXLT) ---
                // Đây là bước quan trọng nhất mà Winform làm trong ConvertSXLT

                // 1.1. XÓA KHAI BÁO BIẾN GỐC (FIX lỗi duplicated variables)
                // Dùng Regex mạnh mẽ hơn để xóa cả khai báo multiline và self-closing
                logMessages.Add("[STEP 1] Removing original XSLT variable declarations to prevent duplicates.");
                
                // List các biến cần xóa
                string[] variablesToRemove = { "moneyType", "issuedDate", "isAdjustInfo", "convertDate", "invDate" };
                
                foreach (var varName in variablesToRemove)
                {
                    int countBefore = System.Text.RegularExpressions.Regex.Matches(xslt, $@"<xsl:variable\s+name=[""']{varName}[""']").Count;
                    
                    // Pattern 1: Self-closing tag (có thể multiline)
                    xslt = System.Text.RegularExpressions.Regex.Replace(xslt, 
                        $@"<xsl:variable\s+name=[""']{varName}[""'][^>]*/>", 
                        "", 
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    // Pattern 2: Opening và closing tag riêng (multiline)
                    xslt = System.Text.RegularExpressions.Regex.Replace(xslt, 
                        $@"<xsl:variable\s+name=[""']{varName}[""'][^>]*>.*?</xsl:variable>", 
                        "", 
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    int countAfter = System.Text.RegularExpressions.Regex.Matches(xslt, $@"<xsl:variable\s+name=[""']{varName}[""']").Count;
                    
                    if (countBefore > 0)
                    {
                        Console.WriteLine($"[DEBUG] Variable '{varName}': {countBefore} found → {countAfter} after removal");
                    }
                    
                    if (countAfter > 0)
                    {
                        Console.WriteLine($"[WARNING] Still have '{varName}' variable(s) after removal!");
                        var match = System.Text.RegularExpressions.Regex.Match(xslt, $@"(.{{0,150}}<xsl:variable\s+name=[""']{varName}[""'].{{0,150}})");
                        if (match.Success)
                        {
                            Console.WriteLine($"[DEBUG] Context: {match.Groups[1].Value}");
                        }
                    }
                }

                // --- BƯỚC 2: INJECT TẤT CẢ CSS (Cố định & Tùy chỉnh) ---
                logMessages.Add("[STEP 2] Injecting combined CSS rules.");

                string customCssRules = CompileCustomCss(options.AdjustConfig, BASELINK_VIEN);
                string allInjectedCss = baseCssRules + customCssRules; // Gộp CSS cố định và Custom

                if (!string.IsNullOrEmpty(allInjectedCss))
                {
                    string xsltBefore = xslt;
                    xslt = SafeInjectCssIntoXslt(xslt, allInjectedCss);
                    
                    if (xslt == xsltBefore)
                    {
                        logMessages.Add("[CSS] WARNING: CSS injection failed - XSLT unchanged. No suitable injection point found.");
                    }
                    else
                    {
                        logMessages.Add($"[CSS] Successfully injected {allInjectedCss.Length} chars of CSS");
                    }
                }

                // --- BƯỚC 3 & 4: INJECT BLOCKS & IMAGES (Giữ nguyên logic bảo vệ null) ---
                logMessages.Add("[STEP 3] Injecting content placeholders.");

                // ClassissuedDate
                if (!string.IsNullOrEmpty(issuedDateContent))
                {
                    xslt = xslt.Replace(@"<div id=""main"">", @"<div id=""main"">" + issuedDateContent);
                    xslt = xslt.Replace(@"<div id=""main"" class=""container"">", @"<div id=""main""  class=""container"">" + issuedDateContent);
                    // ... (log messages cho injection)
                }

                // Footer, Attribute, DTS, STKNMuaOld, HDM (Logic thay thế an toàn đã được áp dụng)
                if (!string.IsNullOrEmpty(strFooter)) xslt = xslt.Replace("<div>@@@@@</div>", strFooter);
                if (!string.IsNullOrEmpty(attribute)) xslt = xslt.Replace("<div>@@@@@attribute</div>", attribute);
                // ... (Các lệnh Replace cho các Rules khác) ...


                // Xử lý Base64 cho ảnh nền (ưu tiên từ Options, fallback sang Company)
                string backgroundBase64 = options.BackgroundBase64;
                if (string.IsNullOrEmpty(backgroundBase64) && req.Company != null)
                {
                    backgroundBase64 = req.Company.BackgroundBase64;
                }
                
                if (!string.IsNullOrEmpty(backgroundBase64))
                {
                    xslt = xslt.Replace("@IMAGE_BACKGROUND@", backgroundBase64);
                    logMessages.Add($"[IMAGE] Injected background image (Base64 length: {backgroundBase64.Length} chars)");
                }
                else
                {
                    xslt = xslt.Replace("@IMAGE_BACKGROUND@", "");
                    logMessages.Add("[IMAGE] No background image");
                }

                // Xử lý Base64 cho Logo (ưu tiên từ Options, fallback sang Company)
                string logoBase64 = options.LogoBase64;
                if (string.IsNullOrEmpty(logoBase64) && req.Company != null)
                {
                    logoBase64 = req.Company.LogoBase64;
                }
                
                if (!string.IsNullOrEmpty(logoBase64))
                {
                    xslt = xslt.Replace("@COMPANY_LOGO@", logoBase64);
                    logMessages.Add($"[IMAGE] Injected logo image (Base64 length: {logoBase64.Length} chars)");
                }
                else
                {
                    xslt = xslt.Replace("@COMPANY_LOGO@", "");
                    logMessages.Add("[IMAGE] No logo image");
                }

                logMessages.Add($"[END] XSLT processing complete. Final length: {xslt.Length} chars");

                return xslt;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"XSLT Rule application failed. Log: \n{string.Join("\n", logMessages)}", ex);
            }
        }

        #region HELPER METHODS
        private string SafeInjectCssIntoXslt(string xslt, string cssToInject)
        {
            if (string.IsNullOrEmpty(cssToInject)) return xslt;
            
            // Clean CSS - remove any existing CDATA markers to avoid nesting
            cssToInject = cssToInject.Replace("<![CDATA[", "")
                                     .Replace("]]>", "")
                                     .Replace("/*<![CDATA[*/", "")
                                     .Replace("/*]]>*/", "")
                                     .Trim();
            
            // Try method 1: Look for /**/ marker (safest - no wrapping needed)
            if (xslt.Contains("/**/"))
            {
                return xslt.Replace("/**/", cssToInject);
            }
            
            // Try method 2: Look for xsl:text with disable-output-escaping
            // Pattern: <style>...<xsl:text disable-output-escaping="yes">CSS_HERE</xsl:text></style>
            var xslTextPattern = @"(<xsl:text[^>]*disable-output-escaping\s*=\s*[""']yes[""'][^>]*>)(.*?)(</xsl:text>)";
            var xslTextMatch = System.Text.RegularExpressions.Regex.Match(xslt, xslTextPattern, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (xslTextMatch.Success)
            {
                string openTag = xslTextMatch.Groups[1].Value;
                string existingCss = xslTextMatch.Groups[2].Value;
                string closeTag = xslTextMatch.Groups[3].Value;
                
                // Check if this xsl:text is inside a <style> tag
                int xslTextPos = xslTextMatch.Index;
                int styleStartPos = xslt.LastIndexOf("<style", xslTextPos);
                int styleEndPos = xslt.IndexOf("</style>", xslTextPos);
                
                if (styleStartPos >= 0 && styleEndPos > xslTextPos)
                {
                    // This xsl:text is inside style tag, inject CSS before closing xsl:text
                    string replacement = openTag + existingCss + "\n" + cssToInject + "\n" + closeTag;
                    return System.Text.RegularExpressions.Regex.Replace(xslt, xslTextPattern, replacement, 
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                }
            }
            
            // Try method 3: Look for any xsl:text inside style (without disable-output-escaping)
            var anyXslTextPattern = @"(<style[^>]*>.*?)(<xsl:text[^>]*>)(.*?)(</xsl:text>)(.*?</style>)";
            var anyXslTextMatch = System.Text.RegularExpressions.Regex.Match(xslt, anyXslTextPattern, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (anyXslTextMatch.Success)
            {
                string beforeXslText = anyXslTextMatch.Groups[1].Value;
                string xslTextOpen = anyXslTextMatch.Groups[2].Value;
                string existingCss = anyXslTextMatch.Groups[3].Value;
                string xslTextClose = anyXslTextMatch.Groups[4].Value;
                string afterXslText = anyXslTextMatch.Groups[5].Value;
                
                // Inject CSS before closing xsl:text
                string replacement = beforeXslText + xslTextOpen + existingCss + "\n" + cssToInject + "\n" + xslTextClose + afterXslText;
                return System.Text.RegularExpressions.Regex.Replace(xslt, anyXslTextPattern, replacement, 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
            }
            
            // Method 4: Simple fallback - find </style> and inject before it (no xsl:text wrapper to avoid nesting)
            // This only works if the XSLT doesn't have xsl:text at all
            if (xslt.Contains("<style") && xslt.Contains("</style>") && !xslt.Contains("<xsl:text"))
            {
                int lastStylePos = xslt.LastIndexOf("</style>");
                if (lastStylePos > 0)
                {
                    // Wrap in xsl:text since there's no existing xsl:text
                    string wrappedCss = "<xsl:text disable-output-escaping=\"yes\">\n" + cssToInject + "\n</xsl:text>";
                    return xslt.Insert(lastStylePos, wrappedCss);
                }
            }
            
            // If all else fails, return unchanged to avoid breaking XSLT
            return xslt;
        }
        #endregion
       
        private string Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            try
            {
                var gzipBytes = Convert.FromBase64String(input);
                using (var ms = new MemoryStream(gzipBytes))
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                using (var outMs = new MemoryStream())
                {
                    gzip.CopyTo(outMs);
                    var resultString = Encoding.UTF8.GetString(outMs.ToArray());

                    try
                    {
                        if (!resultString.TrimStart().StartsWith("<"))
                        {
                            var innerBytes = Convert.FromBase64String(resultString);
                            return Encoding.UTF8.GetString(innerBytes);
                        }
                    }
                    catch
                    {
                        // Ignore
                    }

                    return resultString;
                }
            }
            catch
            {
                try
                {
                    var bytes = Convert.FromBase64String(input);
                    return Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    return input;
                }
            }
        }

        private string GenerateXmlData(InvoiceBuildRequest req, string xmlTemplate)
        {
            if (req.Company == null) return xmlTemplate;

            string xml = xmlTemplate;
            var company = req.Company;
            string EscapeXml(string text) => string.IsNullOrEmpty(text) ? "" : 
                System.Security.SecurityElement.Escape(text);
            // Thông tin người bán (Seller)
            xml = xml.Replace("@sellerLegalName@", EscapeXml(company.SName));
            xml = xml.Replace("@sellerTaxCode@", EscapeXml(company.MerchantID));
            xml = xml.Replace("@sellerAddressLine@", EscapeXml(company.Address));
            xml = xml.Replace("@SELLER_PHONE@", EscapeXml(company.Tel));
            xml = xml.Replace("@SELLER_FAX@", EscapeXml(company.Fax));
            xml = xml.Replace("@SELLER_EMAIL@", EscapeXml(company.Email));
            xml = xml.Replace("@SELLER_WEBSITE@", EscapeXml(company.Website));
            xml = xml.Replace("@SELLER_BANK_ACCOUNT@", EscapeXml(company.BankNumber));
            xml = xml.Replace("@SELLER_BANK_NAME@", EscapeXml(company.BankInfo));
            xml = xml.Replace("@SELLER_PERSON@", EscapeXml(company.PersonOfMerchant));

            // Mẫu số và ký hiệu
            xml = xml.Replace("@TEMPLATE_CODE@", EscapeXml(company.SampleID));
            xml = xml.Replace("@INVOICE_SERIES@", EscapeXml(company.SampleSerial));

            // Ngày lập (mặc định hôm nay nếu không có)
            xml = xml.Replace("@ISSUED_DATE@", DateTime.Now.ToString("yyyy-MM-dd"));

            // Đơn vị tiền tệ
            xml = xml.Replace("@CURRENCY_CODE@", "VND");

            // Các trường mặc định khác (có thể customize sau)
            xml = xml.Replace("@INVOICE_NUMBER@", "00000000");
            xml = xml.Replace("@BUYER_NAME@", "");
            xml = xml.Replace("@BUYER_TAX@", "");
            xml = xml.Replace("@BUYER_ADDRESS@", "");
            xml = xml.Replace("@TOTAL_AMOUNT@", "0");
            xml = xml.Replace("@VAT_AMOUNT@", "0");
            xml = xml.Replace("@TOTAL_WITH_VAT@", "0");
            xml = xml.Replace("@AMOUNT_IN_WORDS@", "");

            return xml;
        }

        private string GenerateXmlData_VCNB(InvoiceBuildRequest req, string xmlTemplate)
        {
            if (req.Company == null) return xmlTemplate;

            string xml = xmlTemplate;
            var company = req.Company;

            string EscapeXml(string text) => string.IsNullOrEmpty(text) ? "" : 
                System.Security.SecurityElement.Escape(text);

            // === THÔNG TIN CHUNG ===
            xml = xml.Replace("@invoiceName@", "PHIẾU XUẤT KHO KIÊM VẬN CHUYỂN NỘI BỘ");
            xml = xml.Replace("@templateCode@", EscapeXml(company.SampleID ?? ""));
            xml = xml.Replace("@invoiceSeries@", EscapeXml(company.SampleSerial ?? ""));
            xml = xml.Replace("@invoiceNumber@", "00000000");
            xml = xml.Replace("@maHoSo@", "");
            xml = xml.Replace("@invoiceIssuedDate@", DateTime.Now.ToString("yyyy-MM-dd"));
            xml = xml.Replace("@currencyCode@", "VND");
            xml = xml.Replace("@ExchangeRate@", "1");
            xml = xml.Replace("@htThanhToan@", "");
            xml = xml.Replace("@mstTCGP@", "");

            // === THÔNG TIN NGƯỜI BÁN (NBan) ===
            xml = xml.Replace("@sellerLegalName@", EscapeXml(company.SName ?? ""));
            xml = xml.Replace("@sellerTaxCode@", EscapeXml(company.MerchantID ?? ""));
            xml = xml.Replace("@sellerAddressLine@", EscapeXml(company.Address ?? ""));
            xml = xml.Replace("@sellerPhoneNumber@", EscapeXml(company.Tel ?? ""));
            xml = xml.Replace("@sellerFaxNumber@", EscapeXml(company.Fax ?? ""));
            xml = xml.Replace("@sellerEmail@", EscapeXml(company.Email ?? ""));
            xml = xml.Replace("@sellerWebsite@", EscapeXml(company.Website ?? ""));
            xml = xml.Replace("@sellerBankAccount@", EscapeXml(company.BankNumber ?? ""));
            xml = xml.Replace("@sellerBankName@", EscapeXml(company.BankInfo ?? ""));

            // === CÁC TRƯỜNG ĐẶC BIỆT CỦA VCNB ===
            xml = xml.Replace("@lenhDieuDongNB@", ""); // Lệnh điều động nội bộ
            xml = xml.Replace("@hsSo@", ""); // Hộ số
            xml = xml.Replace("@hoVaTenNguoiXuatHH@", ""); // Họ và tên người xuất hàng hóa
            xml = xml.Replace("@tenNguoiVanChuyen@", ""); // Tên người vận chuyển
            xml = xml.Replace("@phuongTienVanChuyen@", ""); // Phương tiện vận chuyển
            xml = xml.Replace("@diaChiKhoXuat@", EscapeXml(company.Address ?? "")); // Địa chỉ kho xuất
            xml = xml.Replace("@THKDoanh@", ""); // Tên hộ kinh doanh

            // === THÔNG TIN NGƯỜI MUA (NMua) ===
            xml = xml.Replace("@buyerLegalName@", "");
            xml = xml.Replace("@buyerTaxCode@", "");
            xml = xml.Replace("@buyerAddressLine@", "");
            xml = xml.Replace("@buyerPhoneNumber@", "");
            xml = xml.Replace("@buyerCode@", "");
            xml = xml.Replace("@buyerDisplayName@", "");
            xml = xml.Replace("@buyerBankAccount@", "");
            xml = xml.Replace("@buyerBankName@", "");
            xml = xml.Replace("@diaChiKhoNhap@", ""); // Địa chỉ kho nhập
            xml = xml.Replace("@hoTenNguoiNhanHang@", ""); // Họ tên người nhận hàng

            // === THÔNG TIN THANH TOÁN ===
            xml = xml.Replace("@discountAmount@", "0");
            xml = xml.Replace("@totalAmountWithVAT@", "0");
            xml = xml.Replace("@totalAmountWithVATInWords@", "");

            // === THÔNG TIN HÓA ĐƠN LIÊN QUAN ===
            xml = xml.Replace("@refInvActionType@", "");
            xml = xml.Replace("@refInvType@", "");
            xml = xml.Replace("@refInvSample@", "");
            xml = xml.Replace("@refInvSign@", "");
            xml = xml.Replace("@refInvCode@", "");
            xml = xml.Replace("@refInvDate@", "");

            // === CÁC TRƯỜNG KHÁC ===
            xml = xml.Replace("@privateCode@", "");
            xml = xml.Replace("@cmpnKey@", EscapeXml(company.MerchantID ?? ""));
            xml = xml.Replace("@vatNone@", "");
            xml = xml.Replace("@invoiceNote@", "");
            xml = xml.Replace("@isConvert@", "0");
            xml = xml.Replace("@convertDate@", "");
            xml = xml.Replace("@signedDate@", "");
            xml = xml.Replace("@paymentMethodName@", "");
            xml = xml.Replace("@isAdjustInfo@", "0");
            xml = xml.Replace("@referenceNo@", "");
            xml = xml.Replace("@contractNo@", "");
            xml = xml.Replace("@contractDate@", "");
            xml = xml.Replace("@exciseTaxRate@", "0");
            xml = xml.Replace("@exciseTaxAmnt@", "0");
            xml = xml.Replace("@totalQtty@", "0");
            xml = xml.Replace("@adjCode@", "");
            xml = xml.Replace("@numStatus@", "1");
            xml = xml.Replace("@moneyTypeNote@", "");
            xml = xml.Replace("@descrip@", "");
            xml = xml.Replace("@buyerTaxCodeEx@", "");
            xml = xml.Replace("@referenceDate@", "");
            xml = xml.Replace("@deliverDate@", "");
            xml = xml.Replace("@otherField1@", "");
            xml = xml.Replace("@showFullDetail@", "0");
            xml = xml.Replace("@showNone@", "0");

            return xml;
        }
        private string GenerateXmlData_HGDL(InvoiceBuildRequest req, string xmlTemplate)
        {
            if (req.Company == null) return xmlTemplate;

            string xml = xmlTemplate;
            var company = req.Company;

            string EscapeXml(string text) => string.IsNullOrEmpty(text) ? "" : 
                System.Security.SecurityElement.Escape(text);

            // === THÔNG TIN CHUNG ===
            xml = xml.Replace("@invoiceName@", "PHIẾU XUẤT KHO HÀNG GỬI ĐẠI LÝ");
            xml = xml.Replace("@templateCode@", EscapeXml(company.SampleID ?? ""));
            xml = xml.Replace("@invoiceSeries@", EscapeXml(company.SampleSerial ?? ""));
            xml = xml.Replace("@invoiceNumber@", "00000000");
            xml = xml.Replace("@maHoSo@", "");
            xml = xml.Replace("@invoiceIssuedDate@", DateTime.Now.ToString("yyyy-MM-dd"));
            xml = xml.Replace("@currencyCode@", "VND");
            xml = xml.Replace("@ExchangeRate@", "1");
            xml = xml.Replace("@htThanhToan@", "");
            xml = xml.Replace("@mstTCGP@", "");

            // === THÔNG TIN NGƯỜI BÁN (NBan) ===
            xml = xml.Replace("@sellerLegalName@", EscapeXml(company.SName ?? ""));
            xml = xml.Replace("@sellerTaxCode@", EscapeXml(company.MerchantID ?? ""));
            xml = xml.Replace("@sellerAddressLine@", EscapeXml(company.Address ?? ""));
            xml = xml.Replace("@sellerPhoneNumber@", EscapeXml(company.Tel ?? ""));
            xml = xml.Replace("@sellerFaxNumber@", EscapeXml(company.Fax ?? ""));
            xml = xml.Replace("@sellerEmail@", EscapeXml(company.Email ?? ""));
            xml = xml.Replace("@sellerWebsite@", EscapeXml(company.Website ?? ""));
            xml = xml.Replace("@sellerBankAccount@", EscapeXml(company.BankNumber ?? ""));
            xml = xml.Replace("@sellerBankName@", EscapeXml(company.BankInfo ?? ""));

            // === CÁC TRƯỜNG ĐẶC BIỆT CỦA HGDL ===
            xml = xml.Replace("@HDKTSo@", ""); // Hóa đơn kèm theo số
            xml = xml.Replace("@HDKTNgay@", ""); // Hóa đơn kèm theo ngày
            xml = xml.Replace("@diaChiKhoXuatHang@", EscapeXml(company.Address ?? ""));
            xml = xml.Replace("@hoVaTenNguoiXuatHH@", "");
            xml = xml.Replace("@tenNguoiVanChuyen@", "");
            xml = xml.Replace("@hsSo@", "");
            xml = xml.Replace("@phuongTienVanChuyen@", "");
            xml = xml.Replace("@staffSeller@", EscapeXml(company.PersonOfMerchant ?? ""));
            xml = xml.Replace("@THKDoanh@", "");
            xml = xml.Replace("@IsHKDoanh@", "");

            // === THÔNG TIN NGƯỜI MUA (NMua) ===
            xml = xml.Replace("@buyerLegalName@", "");
            xml = xml.Replace("@buyerTaxCode@", "");
            xml = xml.Replace("@diaChiKhoNhanHang@", "");
            xml = xml.Replace("@hoTenNguoiNhanHang@", "");

            // === THÔNG TIN THANH TOÁN ===
            xml = xml.Replace("@totalAmountWithVAT@", "0");
            xml = xml.Replace("@totalAmountWithVATInWords@", "");

            // === THÔNG TIN HÓA ĐƠN LIÊN QUAN ===
            xml = xml.Replace("@refInvActionType@", "");
            xml = xml.Replace("@refInvType@", "");
            xml = xml.Replace("@refInvSample@", "");
            xml = xml.Replace("@refInvSign@", "");
            xml = xml.Replace("@refInvCode@", "");
            xml = xml.Replace("@refInvDate@", "");
            xml = xml.Replace("@refInvNote@", "");

            // === CÁC TRƯỜNG KHÁC ===
            xml = xml.Replace("@privateCode@", "");
            xml = xml.Replace("@cmpnKey@", EscapeXml(company.MerchantID ?? ""));
            xml = xml.Replace("@isConvert@", "0");
            xml = xml.Replace("@convertDate@", "");
            xml = xml.Replace("@signedDate@", "");
            xml = xml.Replace("@isAdjustInfo@", "0");
            xml = xml.Replace("@referenceNo@", "");
            xml = xml.Replace("@contractNo@", "");
            xml = xml.Replace("@contractDate@", "");
            xml = xml.Replace("@totalQtty@", "0");
            xml = xml.Replace("@adjCode@", "");
            xml = xml.Replace("@numStatus@", "1");
            xml = xml.Replace("@moneyTypeNote@", "");
            xml = xml.Replace("@descrip@", "");
            xml = xml.Replace("@buyerTaxCodeEx@", "");
            xml = xml.Replace("@referenceDate@", "");
            xml = xml.Replace("@deliverDate@", "");
            xml = xml.Replace("@otherField1@", "");
            xml = xml.Replace("@showFullDetail@", "0");
            xml = xml.Replace("@showNone@", "0");

            return xml;
        }
        private string GenerateXmlData_TaxDocument(InvoiceBuildRequest req, string xmlTemplate)
        {
            if (req.Company == null) return xmlTemplate;

            string xml = xmlTemplate;
            var company = req.Company;

            string EscapeXml(string text) => string.IsNullOrEmpty(text) ? "" : 
                System.Security.SecurityElement.Escape(text);

            // === THÔNG TIN CHUNG (TTChung) ===
            xml = xml.Replace("@DLHDon_Id@", "DLCTu");
            xml = xml.Replace("@PBan@", "2.0.0");
            xml = xml.Replace("@invoiceSeries@", EscapeXml(company.SampleSerial ?? ""));
            xml = xml.Replace("@invoiceNumber@", "00000000");
            xml = xml.Replace("@invoiceIssuedDate@", DateTime.Now.ToString("yyyy-MM-dd"));
            xml = xml.Replace("@mstTCGP@", "");
            
            // === HÓA ĐƠN LIÊN QUAN ===
            xml = xml.Replace("@refInvActionType@", "");
            xml = xml.Replace("@refInvType@", "");
            xml = xml.Replace("@refInvSample@", "");
            xml = xml.Replace("@refInvSign@", "");
            xml = xml.Replace("@refInvCode@", "");
            xml = xml.Replace("@refInvDate@", "");
            xml = xml.Replace("@GChu@", "");

            // === THÔNG TIN KHÁC (TTKhac trong TTChung) ===
            xml = xml.Replace("@DDanh@", ""); // Địa danh
            xml = xml.Replace("@privateCode@", "");
            xml = xml.Replace("@cmpnKey@", EscapeXml(company.MerchantID ?? ""));
            xml = xml.Replace("@tChat@", "1");
            xml = xml.Replace("@TTNCTDTra@", "0");
            xml = xml.Replace("@STNCNCDNhan@", "0");
            xml = xml.Replace("@NoteCT@", "");
            xml = xml.Replace("@BookNo@", "");

            // === TỔ CHỨC TRẢ THU NHẬP (TCTTNhap) ===
            xml = xml.Replace("@TTCTTNhap@", EscapeXml(company.SName ?? ""));
            xml = xml.Replace("@MSTTCTTNhap@", EscapeXml(company.MerchantID ?? ""));
            xml = xml.Replace("@DChiTCTTNhap@", EscapeXml(company.Address ?? ""));
            xml = xml.Replace("@DThoaiTCTTNhap@", EscapeXml(company.Tel ?? ""));

            // === NGƯỜI NỘP THUẾ (NNT) ===
            xml = xml.Replace("@HVTenNNThue@", ""); // Họ và tên người nộp thuế
            xml = xml.Replace("@MSTNNThue@", ""); // MST người nộp thuế
            xml = xml.Replace("@TTLHeNNThue@", ""); // Thông tin liên hệ
            xml = xml.Replace("@QTichNNThue@", ""); // Quốc tịch
            xml = xml.Replace("@CNCTruNNThue@", ""); // Cơ quan chi trả
            xml = xml.Replace("@CMNDNNThue@", ""); // CMND
            xml = xml.Replace("@SDThoaiNNThue@", ""); // Số điện thoại

            // === THÔNG TIN THU NHẬP KHẤU TRỪ (TTNCNKTru) ===
            xml = xml.Replace("@KTNhapNNThue@", ""); // Khoản thu nhập
            xml = xml.Replace("@TThang@", ""); // Từ tháng
            xml = xml.Replace("@DThang@", ""); // Đến tháng
            xml = xml.Replace("@Nam@", DateTime.Now.Year.ToString()); // Năm
            xml = xml.Replace("@KDBHBBuoc@", ""); // Kinh doanh bảo hiểm bắt buộc
            xml = xml.Replace("@TThien@", ""); // Từ thiện
            xml = xml.Replace("@TTNCTPKTruNThue@", "0"); // Tổng thu nhập chịu thuế
            xml = xml.Replace("@TTNTThueNThue@", "0"); // Tổng thuế thu nhập
            xml = xml.Replace("@STTNCNDKTruNThue@", "0"); // Số thuế TNCN đã khấu trừ

            return xml;
        }

        public string XsltTransform(string xml, string xslt)
        {
            var xsltDoc = new XslCompiledTransform();
            var settings = new XsltSettings(true, true);
            var resolver = new XmlUrlResolver();

            try
            {
                // Validate XSLT structure before loading
                using (var reader = XmlReader.Create(new StringReader(xslt)))
                {
                    xsltDoc.Load(reader, settings, resolver);
                }

                using (var xmlReader = XmlReader.Create(new StringReader(xml)))
                using (var sw = new StringWriter())
                {
                    xsltDoc.Transform(xmlReader, null, sw);
                    return sw.ToString();
                }
            }
            catch (XsltCompileException xsltEx)
            {
                string errorMessage = $"Lỗi biên dịch XSLT: {xsltEx.Message}";
                if (xsltEx.LineNumber > 0)
                {
                    errorMessage += $"\n>> Tại dòng: {xsltEx.LineNumber}, cột: {xsltEx.LinePosition}";
                    errorMessage += GetXsltContextLines(xslt, xsltEx.LineNumber);
                }

                if (xsltEx.InnerException != null)
                {
                    errorMessage += $"\n>> Chi tiết (Inner Exception): {xsltEx.InnerException.Message}";
                }

                throw new Exception(errorMessage, xsltEx);
            }
            catch (XmlException xmlEx)
            {
                string errorMessage = $"Lỗi định dạng XML/XSLT: {xmlEx.Message}";
                if (xmlEx.LineNumber > 0)
                {
                    errorMessage += $"\n>> Tại dòng: {xmlEx.LineNumber}, cột: {xmlEx.LinePosition}";
                    errorMessage += GetXsltContextLines(xslt, xmlEx.LineNumber);
                }
                throw new Exception(errorMessage, xmlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xử lý mẫu hóa đơn (Transform): {ex.Message}", ex);
            }
        }

        private string ReplaceXsltPlaceholders(string xslt, InvoiceBuildRequest req)
        {
            if (req.Company == null) return xslt;

            var company = req.Company;
            
            // Helper để escape XML (vì đang trong XSLT - là XML)
            string EscapeForXslt(string text) => string.IsNullOrEmpty(text) ? "" : EscapeXml(text);

            // Seller Information
            xslt = xslt.Replace("@sellerLegalName@", EscapeForXslt(company.SName));
            xslt = xslt.Replace("@sellerTaxCode@", EscapeForXslt(company.MerchantID));
            xslt = xslt.Replace("@sellerAddressLine@", EscapeForXslt(company.Address));
            xslt = xslt.Replace("@sellerPhoneNumber@", EscapeForXslt(company.Tel));
            xslt = xslt.Replace("@sellerFaxNumber@", EscapeForXslt(company.Fax));
            xslt = xslt.Replace("@sellerEmail@", EscapeForXslt(company.Email));
            xslt = xslt.Replace("@sellerWebsite@", EscapeForXslt(company.Website));
            xslt = xslt.Replace("@sellerBankAccount@", EscapeForXslt(company.BankNumber));
            xslt = xslt.Replace("@sellerBankName@", EscapeForXslt(company.BankInfo));
            
            // Uppercase variants
            xslt = xslt.Replace("@SELLER_LEGAL_NAME@", EscapeForXslt(company.SName));
            xslt = xslt.Replace("@SELLER_TAX@", EscapeForXslt(company.MerchantID));
            xslt = xslt.Replace("@SELLER_ADDRESS@", EscapeForXslt(company.Address));
            xslt = xslt.Replace("@SELLER_PHONE@", EscapeForXslt(company.Tel));
            xslt = xslt.Replace("@SELLER_FAX@", EscapeForXslt(company.Fax));
            xslt = xslt.Replace("@SELLER_EMAIL@", EscapeForXslt(company.Email));
            xslt = xslt.Replace("@SELLER_WEBSITE@", EscapeForXslt(company.Website));
            xslt = xslt.Replace("@SELLER_BANK_ACCOUNT@", EscapeForXslt(company.BankNumber));
            xslt = xslt.Replace("@SELLER_BANK_NAME@", EscapeForXslt(company.BankInfo));

            // Invoice Information
            xslt = xslt.Replace("@invoiceName@", "HÓA ĐƠN GIÁ TRỊ GIA TĂNG");
            xslt = xslt.Replace("@invoiceNumber@", "00000000");
            xslt = xslt.Replace("@templateCode@", EscapeForXslt(company.SampleID ?? "1"));
            xslt = xslt.Replace("@invoiceSeries@", EscapeForXslt(company.SampleSerial ?? ""));
            xslt = xslt.Replace("@currencyCode@", "VND");

            // Buyer Information (empty for preview)
            xslt = xslt.Replace("@buyerLegalName@", "");
            xslt = xslt.Replace("@buyerTaxCode@", "");
            xslt = xslt.Replace("@buyerAddressLine@", "");
            xslt = xslt.Replace("@buyerBankAccount@", "");
            xslt = xslt.Replace("@buyerBankName@", "");
            xslt = xslt.Replace("@paymentMethodName@", "");

            // Amounts (empty for preview)
            xslt = xslt.Replace("@totalAmountWithVATInWords@", "");
            xslt = xslt.Replace("@vatNone@", "");
            xslt = xslt.Replace("@privateCode@", "");
            xslt = xslt.Replace("@cmpnKey@", "");

            return xslt;
        }

        private string ReplaceHtmlPlaceholders(string html, InvoiceBuildRequest req)
        {
            if (req.Company == null) return html;

            var company = req.Company;
            var options = req.Options?.AdjustConfig;
            
            string Escape(string text) => string.IsNullOrEmpty(text) ? "" : 
                System.Net.WebUtility.HtmlEncode(text);

            html = html.Replace("<span>NaN</span>", "<span></span>");
            html = html.Replace(">NaN<", "><");
            
            if (options != null)
            {
                // Điện thoại/Tel - wrap entire phone section with _NBSDT
                // Pattern: Điện thoại/<span class="en">Tel</span>: <span>PHONE_VALUE</span>
                html = System.Text.RegularExpressions.Regex.Replace(html, 
                    @"(Điện thoại/<span class=""en"">Tel</span>:[\s\r\n]*<span>[\s\r\n]*[^<]+[\s\r\n]*</span>)",
                    @"<span id=""_NBSDT"">$1</span>",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // Fax - wrap both the label span and value span with id="_NBFax"
                // Pattern: <span ...>Fax: </span><span>FAX_VALUE</span>
                html = System.Text.RegularExpressions.Regex.Replace(html,
                    @"<span class=""inl""[^>]*>[\s\r\n]*Fax:[\s\r\n]*</span>[\s\r\n]*<span>[\s\r\n]*([^<]+)[\s\r\n]*</span>",
                    @"<span id=""_NBFax"" class=""inl"" style=""width: 100px; text-align:right"">Fax:  </span><span id=""_NBFax"">$1</span>",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // Email - add id to email value span
                // Pattern: Email: </span><span>EMAIL_VALUE</span>
                html = System.Text.RegularExpressions.Regex.Replace(html,
                    @"(Email:[\s\r\n]*</span>)[\s\r\n]*<span>[\s\r\n]*([^<]+)[\s\r\n]*</span>",
                    @"$1<span id=""_NBEmail"">$2</span>",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // Website - add id to website value span
                // Pattern: Website: </span><span>WEBSITE_VALUE</span>
                html = System.Text.RegularExpressions.Regex.Replace(html,
                    @"(Website:[\s\r\n]*</span>)[\s\r\n]*<span>[\s\r\n]*([^<]+)[\s\r\n]*</span>",
                    @"$1<span id=""_NBWebsite"">$2</span>",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // Số tài khoản - wrap entire account section with _NBSTK
                // Pattern: Số tài khoản/<span class="en">A/C No</span>: <span>ACCOUNT_INFO</span>
                html = System.Text.RegularExpressions.Regex.Replace(html,
                    @"(Số tài khoản/<span class=""en"">A/C No</span>:[\s\r\n]*<span>[\s\r\n]*[^<]+[\s\r\n]*</span>)",
                    @"<span id=""_NBSTK"">$1</span>",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
            }
            
            // --- FIX 3: Fix invoice number alignment (remove space after colon) ---
            html = System.Text.RegularExpressions.Regex.Replace(html,
                @"(Số/<span class=""en"">Inv\.No</span>\s*:\s*</span>)",
                @"Số/<span class=""en"">Inv.No</span>:</span>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Seller Information
            html = html.Replace("@sellerLegalName@", Escape(company.SName));
            html = html.Replace("@sellerTaxCode@", Escape(company.MerchantID));
            html = html.Replace("@sellerAddressLine@", Escape(company.Address));
            html = html.Replace("@sellerPhoneNumber@", Escape(company.Tel));
            html = html.Replace("@sellerFaxNumber@", Escape(company.Fax));
            html = html.Replace("@sellerEmail@", Escape(company.Email));
            html = html.Replace("@sellerWebsite@", Escape(company.Website));
            html = html.Replace("@sellerBankAccount@", Escape(company.BankNumber));
            html = html.Replace("@sellerBankName@", Escape(company.BankInfo));

            // Invoice Information
            html = html.Replace("@invoiceName@", "HÓA ĐƠN GIÁ TRỊ GIA TĂNG");
            html = html.Replace("@invoiceNumber@", "00000000");
            html = html.Replace("@templateCode@", Escape(company.SampleID ?? "1"));
            html = html.Replace("@invoiceSeries@", Escape(company.SampleSerial ?? ""));
            html = html.Replace("@currencyCode@", "VND");

            // Buyer Information (empty for preview)
            html = html.Replace("@buyerLegalName@", "");
            html = html.Replace("@buyerTaxCode@", "");
            html = html.Replace("@buyerAddressLine@", "");
            html = html.Replace("@buyerBankAccount@", "");
            html = html.Replace("@buyerBankName@", "");
            html = html.Replace("@paymentMethodName@", "");

            // Amounts (empty for preview)
            html = html.Replace("@totalAmountWithVATInWords@", "");
            html = html.Replace("@vatNone@", "");
            html = html.Replace("@privateCode@", "");
            html = html.Replace("@cmpnKey@", "");

            return html;
        }

        private string GetXsltContextLines(string xslt, int errorLine, int contextLines = 3)
        {
            try
            {
                var lines = xslt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (errorLine <= 0 || errorLine > lines.Length) return "";

                var sb = new StringBuilder("\n--- Context ---");
                int start = Math.Max(0, errorLine - contextLines - 1);
                int end = Math.Min(lines.Length - 1, errorLine + contextLines - 1);

                for (int i = start; i <= end; i++)
                {
                    string marker = (i == errorLine - 1) ? " >>> " : "     ";
                    sb.AppendLine($"\n{marker}Line {i + 1}: {lines[i]}");
                }
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        private string EscapeXml(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}