using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ERP_Portal_RC.Application.Services
{
    /// <summary>
    /// Xử lý toàn bộ business logic cho nghiệp vụ ký số hợp đồng điện tử.
    /// Bao gồm: Server Sign, App Sign (callback) + 4 API phục vụ SignApp.
    ///
    /// Dependencies:
    ///   - IContractSignRepository  → data access cho ký số
    ///   - IEContractRepository     → lấy XML template từ DB
    ///   - IConfiguration           → đọc AppSettings:ApiUser/ApiPassword
    /// </summary>
    public class ContractSignService : IContractSignService
    {
        private readonly IContractSignRepository _signRepo;
        private readonly IEContractRepository _eContractRepo;
        private readonly IConfiguration _configuration;

        public ContractSignService(
            IContractSignRepository signRepo,
            IEContractRepository eContractRepo,
            IConfiguration configuration)
        {
            _signRepo = signRepo;
            _eContractRepo = eContractRepo;
            _configuration = configuration;
        }

        // ─── Main Sign API ──────────────────────────────────────────────────────

        public async Task<SignContractResult> SignContractAsync(SignContractRequestDto request, string userName)
        {
            if (string.IsNullOrEmpty(request.ReqUser))
                request.ReqUser = userName;

            string method = (request.SignMethod ?? "SERVER").ToUpper();

            // Map Application DTO → Domain request
            var domainRequest = new SignContractDomainRequest
            {
                OID        = request.OID,
                SignMethod  = request.SignMethod,
                ReqUser    = request.ReqUser
            };

            return method switch
            {
                "SERVER" => await _signRepo.SignContractServerAsync(domainRequest),
                "APP"    => new SignContractResult
                {
                    IsSuccess = false,
                    Message = "Ký APP: Vui lòng sử dụng SignApp để ký. Gọi API GetInvParam để lấy danh sách OID."
                },
                "HSM"    => new SignContractResult
                {
                    IsSuccess = false,
                    Message = "Ký HSM: Chức năng HSM chưa được hỗ trợ qua API này. Liên hệ quản trị viên."
                },
                _        => new SignContractResult
                {
                    IsSuccess = false,
                    Message = $"Phương thức ký không hợp lệ: {request.SignMethod}. Hỗ trợ: SERVER, APP, HSM."
                }
            };
        }

        public async Task<(bool IsSigned, string Message)> IsSignedAsync(string oid)
            => await _signRepo.IsSignedAsync(oid);

        public async Task<CheckSignStatusResult> CheckSignStatusAsync(string oid, string signMethod)
        {
            string method = (signMethod ?? "SERVER").ToUpper();
            return method switch
            {
                "APP"    => await CheckAppSignStatusAsync(oid),
                "SERVER" => await _signRepo.CheckSignStatusServerAsync(oid),
                _        => await _signRepo.CheckSignStatusServerAsync(oid)
            };
        }

        private async Task<CheckSignStatusResult> CheckAppSignStatusAsync(string oid)
        {
            var (isSigned, _) = await _signRepo.IsSignedAsync(oid);
            if (isSigned)
                return new CheckSignStatusResult { Status = 2, Message = "Đã ký thành công." };

            var payload = await _signRepo.GetPayloadByOidAsync(oid);
            if (payload == null)
                return new CheckSignStatusResult { Status = -1, Message = "Không tìm thấy process ký cho OID này." };

            return new CheckSignStatusResult { Status = 1, Message = "Đang xử lý ký." };
        }

        // ─── App-Sign Callback APIs ──────────────────────────────────────────────

        /// <summary>API 2.a: Tiếp nhận trạng thái từ SignApp.</summary>
        public async Task<(bool IsSuccess, string Message)> ReceiveSignStatusAsync(SignStatusCallbackRequest request)
        {
            if (string.IsNullOrEmpty(request.ProcessName))
                return (false, "ProcessName is required");

            int statusInt = ParseStatusString(request.Status);
            bool updated = await _signRepo.UpdateSignStatusAsync(
                request.ProcessName, statusInt, request.Message ?? "");

            return updated
                ? (true, "Cập nhật trạng thái thành công")
                : (false, "Không tìm thấy process");
        }

        /// <summary>API 2.b: Kiểm tra JWT hợp lệ.</summary>
        public ValidJwtResponse ValidJwt(ValidJwtRequest request)
        {
            string payloadString = JwtHelper.GetPayloadFromJwt(request.Jwt);

            if (string.IsNullOrEmpty(payloadString))
                return new ValidJwtResponse
                {
                    IsSuccess = false,
                    Message = "JWT không hợp lệ hoặc không thể decode",
                    ReturnDate = DateTime.Now
                };

            string apiUser     = _configuration["AppSettings:ApiUser"]     ?? "api_user";
            string apiPassword = _configuration["AppSettings:ApiPassword"] ?? "api_password";
            string apiInfoPlain = $"{apiUser}-_-{apiPassword}";
            string apiInfo = BosEncryptionHelper.BosEncrypt(apiInfoPlain);

            return new ValidJwtResponse
            {
                IsSuccess = true,
                Message = "JWT hợp lệ",
                Data = new ValidJwtData
                {
                    ApiInfo = apiInfo,
                    resJWT  = request.Jwt
                },
                ReturnDate = DateTime.Now
            };
        }

        /// <summary>API 2.c: Lấy danh sách OID cần ký.</summary>
        public async Task<GetInvParamResponse> GetInvParamAsync(GetInvParamRequest request)
        {
            if (string.IsNullOrEmpty(request.KeyID))
                return new GetInvParamResponse
                {
                    IsSuccess = false,
                    Message = "KeyID is required",
                    Data = new List<PendingOidItem>(),
                    ReturnDate = DateTime.Now
                };

            var data = await _signRepo.GetPendingOidsByKeyAsync(request.KeyID);

            return new GetInvParamResponse
            {
                IsSuccess = data.Count > 0,
                Message   = data.Count > 0 ? "OK" : "Không tìm thấy hợp đồng cần ký",
                Data      = data,
                ReturnDate = DateTime.Now
            };
        }

        /// <summary>API 2.d: Lấy XML để ký số (Base64 encoded).</summary>
        public async Task<GetXmlAllResponse> GetXmlAllAsync(GetXmlAllRequest request)
        {
            string? payloadJson = await _signRepo.GetPayloadByOidAsync(request.Oid);
            if (string.IsNullOrEmpty(payloadJson))
                return new GetXmlAllResponse
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy payload data cho hợp đồng",
                    ReturnDate = DateTime.Now
                };

            string? xmlContent = await BuildContractXmlAsync(request.Oid, payloadJson);
            if (string.IsNullOrEmpty(xmlContent))
                return new GetXmlAllResponse
                {
                    IsSuccess = false,
                    Message = "Không thể tạo XML cho hợp đồng",
                    ReturnDate = DateTime.Now
                };

            string xmlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlContent));
            string nodeSignID = "hdong-" + request.Oid.Replace("/", "-").Replace(":", "-");

            return new GetXmlAllResponse
            {
                IsSuccess = true,
                Message = "OK",
                Data = new GetXmlAllData
                {
                    XmlContent  = xmlBase64,
                    IsSigned    = 0,
                    NodeToStore = "//HDong/DSCKS/NBan",
                    NodeToSign  = "//HDong/DLHDong",
                    NodeSignID  = nodeSignID,
                    SignTime    = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                },
                ReturnDate = DateTime.Now
            };
        }

        /// <summary>API 2.e: Nộp XML đã ký lên server.</summary>
        public async Task<SetSignedXmlResponse> SetSignedXmlAsync(SetSignedXmlRequest request)
        {
            if (string.IsNullOrEmpty(request.XmlContentBase64))
                return new SetSignedXmlResponse
                {
                    IsSuccess = false,
                    Message = "Dữ liệu yêu cầu bị rỗng",
                    ReturnDate = DateTime.Now
                };

            byte[] xmlBytes = Convert.FromBase64String(request.XmlContentBase64);
            string signedXmlContent = System.Text.Encoding.UTF8.GetString(xmlBytes);

            // Lấy payload để enrich tham số cho SP
            string? payloadJson = await _signRepo.GetPayloadByOidAsync(request.Oid);
            if (string.IsNullOrEmpty(payloadJson))
                return new SetSignedXmlResponse
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy payload data cho hợp đồng",
                    ReturnDate = DateTime.Now
                };

            var data = JObject.Parse(payloadJson);
            var comp = data["CompanyInfo"];

            DateTime dtOrder = DateTime.Now;
            if (data["OrderDate"] != null)
                DateTime.TryParseExact(data["OrderDate"]!.ToString(), "dd/MM/yyyy",
                    null, System.Globalization.DateTimeStyles.None, out dtOrder);

            // Encode XML đã ký thành Base64 để truyền vào SP
            string xmlBase64ForSp = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(signedXmlContent));

            bool success = await _signRepo.SaveSignedXmlAsync(
                oid:             request.Oid,
                signedXmlBase64: xmlBase64ForSp,
                orderDate:       dtOrder,
                partnerVat:      data["PartnerVAT"]?.ToString()   ?? "",
                partnerName:     data["PartnerName"]?.ToString()  ?? "",
                companyTax:      comp?["TaxCode"]?.ToString()      ?? "",
                companyName:     comp?["CompanyName"]?.ToString()  ?? "");

            if (success)
            {
                await _signRepo.UpdateAppSignStatusByOidAsync(request.Oid, 2, "Đã ký thành công");
                return new SetSignedXmlResponse
                {
                    IsSuccess = true,
                    Message = "Lưu XML đã ký thành công",
                    Data = new SetSignedXmlData { MaTD = "" },
                    ReturnDate = DateTime.Now
                };
            }

            return new SetSignedXmlResponse
            {
                IsSuccess = false,
                Message = "Không thể lưu XML đã ký",
                ReturnDate = DateTime.Now
            };
        }

        // ─── Private Helpers ─────────────────────────────────────────────────────

        /// <summary>Tạo XML hợp đồng từ template DB + payload JSON.</summary>
        private async Task<string?> BuildContractXmlAsync(string oid, string payloadJson)
        {
            try
            {
                var data = JObject.Parse(payloadJson);

                // Lấy template XML từ DB thông qua IEContractRepository (Application → Domain)
                var template = await _eContractRepo.GetTemplateByCodeAsync("TT78_EContract");
                if (template == null || string.IsNullOrEmpty(template.XmlContent))
                    return null;

                string xmlTemplate = template.XmlContent;

                var sbProducts = new System.Text.StringBuilder();
                var products   = data["Products"] as JArray;

                if (products != null)
                {
                    int idx = 1;
                    foreach (var item in products)
                    {
                        decimal qty    = item["Quantity"]?.Value<decimal>() ?? 0;
                        decimal price  = item["Price"]?.Value<decimal>()    ?? 0;
                        decimal amount = item["Amount"]?.Value<decimal>()   ?? 0;

                        string name    = Escape(item["Name"]?.ToString());
                        string unit    = (item["Unit"]?.ToString() ?? "").Trim();
                        string sSample = (item["invcSample"] ?? item["InvcSample"])?.ToString() ?? "";
                        string sSign   = (item["invcSign"]   ?? item["InvcSign"])?.ToString()   ?? "";
                        int invcFrm    = (item["invcFrm"]    ?? item["InvcFrm"])?.Value<int>()  ?? 0;
                        int invcEnd    = (item["invcEnd"]    ?? item["InvcEnd"])?.Value<int>()  ?? 0;
                        string sFrm    = invcFrm > 0 ? invcFrm.ToString() : "";
                        string sEnd    = invcEnd > 0 ? invcEnd.ToString() : "";

                        sbProducts.Append("<HHDVu>");
                        sbProducts.Append($"<STT>{idx++}</STT>");
                        sbProducts.Append($"<THHDVu>{name}</THHDVu><ItemName>{name}</ItemName><itemName>{name}</itemName>");
                        sbProducts.Append($"<DVTinh>{unit}</DVTinh><itemUnitName>{unit}</itemUnitName><ItemUnit>{unit}</ItemUnit>");
                        sbProducts.Append($"<SLuong>{qty}</SLuong><ItemQtty>{qty}</ItemQtty><itemQtty>{qty}</itemQtty>");
                        sbProducts.Append($"<DGia>{price}</DGia><ItemPrice>{price}</ItemPrice><itemPrice>{price}</itemPrice>");
                        sbProducts.Append($"<ThTien>{amount}</ThTien><Sum_Amnt>{amount}</Sum_Amnt><sum_Amnt>{amount}</sum_Amnt>");
                        sbProducts.Append($"<MSo>{sSample}</MSo><invcSample>{sSample}</invcSample><InvcSample>{sSample}</InvcSample>");
                        sbProducts.Append($"<KHieu>{sSign}</KHieu><InvcSign>{sSign}</InvcSign><invcSign>{sSign}</invcSign>");
                        sbProducts.Append($"<TSo>{sFrm}</TSo><InvcFrm>{sFrm}</InvcFrm><invcFrm>{sFrm}</invcFrm>");
                        sbProducts.Append($"<DSo>{sEnd}</DSo><InvcEnd>{sEnd}</InvcEnd><invcEnd>{sEnd}</invcEnd>");
                        sbProducts.Append($"<Description>{name}</Description>");
                        sbProducts.Append("</HHDVu>");
                    }
                }

                if (!DateTime.TryParse(data["OrderDate"]?.ToString(), out DateTime orderDate))
                    orderDate = DateTime.Now;

                var c = data["CompanyInfo"];
                decimal totalAmount = data["TotalAmount"]?.Value<decimal>() ?? 0;

                string finalXml = xmlTemplate
                    .Replace("{order_code}",            oid ?? data["OrderCode"]?.ToString() ?? "ĐANG TẠO")
                    .Replace("{order_date_day}",         orderDate.Day.ToString("00"))
                    .Replace("{order_date_month}",       orderDate.Month.ToString("00"))
                    .Replace("{order_date_year}",        orderDate.Year.ToString())
                    .Replace("{partner_name}",           Escape(data["PartnerName"]?.ToString()))
                    .Replace("{partner_vat}",            data["PartnerVAT"]?.ToString()          ?? "")
                    .Replace("{partner_address}",        Escape(data["PartnerAddress"]?.ToString()))
                    .Replace("{partner_bank_no}",        data["PartnerBankAccount"]?.ToString()   ?? "")
                    .Replace("{partner_bank_title}",     Escape(data["PartnerBankName"]?.ToString()))
                    .Replace("{partner_contact_name}",   Escape(data["PartnerRepresentative"]?.ToString()))
                    .Replace("{partner_legal_value}",    Escape(data["PartnerLegalValue"]?.ToString()))
                    .Replace("{partner_contact_job}",    data["PartnerPosition"]?.ToString()      ?? "")
                    .Replace("{partner_phone}",          data["PartnerPhone"]?.ToString()         ?? "")
                    .Replace("{partner_email}",          data["PartnerEmail"]?.ToString()         ?? "")
                    .Replace("{company_name}",           Escape(c?["CompanyName"]?.ToString()     ?? "CÔNG TY TNHH WIN TECH SOLUTION"))
                    .Replace("{company_vat}",            c?["TaxCode"]?.ToString()                ?? "0312303803")
                    .Replace("{company_address}",        Escape(c?["Address"]?.ToString()         ?? "59B Bình Giã, Phường Tân Bình, Tp.HCM"))
                    .Replace("{company_representative}", Escape(c?["Representative"]?.ToString()  ?? "LÊ TRẦN THANH DUY"))
                    .Replace("{company_position}",       c?["Position"]?.ToString()               ?? "Giám đốc")
                    .Replace("{company_phone}",          c?["Phone"]?.ToString()                  ?? "19001129")
                    .Replace("{company_email}",          c?["Email"]?.ToString()                  ?? "info@win-tech.vn")
                    .Replace("{company_bank_no}",        c?["BankAccount"]?.ToString()            ?? "68968888")
                    .Replace("{company_bank_title}",     Escape(c?["BankName"]?.ToString()        ?? "Ngân hàng TMCP Á Châu - Chi nhánh Bảy Hiền"))
                    .Replace("{ProductLines}",           sbProducts.ToString())
                    .Replace("{TgTTTBSo}",               totalAmount.ToString("0"))
                    .Replace("{TgTTTBChu}",              NumberToTextHelper.Convert(totalAmount));

                return finalXml;
            }
            catch
            {
                return null;
            }
        }

        private static string Escape(string? value)
            => System.Security.SecurityElement.Escape(value ?? "") ?? "";

        private static int ParseStatusString(string? status)
        {
            if (string.IsNullOrEmpty(status)) return 0;
            if (int.TryParse(status, out int n)) return n;
            return status.ToUpper() switch
            {
                "PENDING"    => 0,
                "PROCESSING" => 1,
                "SUCCESS"    => 2,
                "ERROR"      => -1,
                "FAILED"     => -1,
                _            => 0
            };
        }
    }
}
