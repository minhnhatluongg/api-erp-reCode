using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class CreateAccountService : ICapTaiKhoanService
    {
        private readonly IConnectionRepository _connectionRepo;
        private readonly ICreateAccountRepository _capTKRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<CreateAccountService> _logger;

        private readonly string _webAppBaseUrl;
        private readonly string _webAppUser;
        private readonly string _webAppPassword;
        private readonly string _dbCredUser;
        private readonly string _dbCredPassword;

        public CreateAccountService(
            IConnectionRepository connectionRepo,
            ICreateAccountRepository capTKRepo,
            IConfiguration config,
            ILogger<CreateAccountService> logger)
        {
            _connectionRepo = connectionRepo;
            _capTKRepo = capTKRepo;
            _config = config;
            _logger = logger;
            _webAppBaseUrl = _config["WebApp:BaseUrl"] ?? "";
            _webAppUser = _config["WebApp:AuthUser"] ?? "";
            _webAppPassword = _config["WebApp:AuthPass"] ?? "";
            _webAppPassword = _config["DbCredentials:User"] ?? "";
            _webAppPassword = _config["DbCredentials:Password"] ?? "";
        }
        public async Task<CreateAccountResponseDto> CapTaiKhoanAsync(CreateAccountRequestDto request)
        {
            var result = new CreateAccountResponseDto();
            string mst = request.MaSoThue.Replace(" ", "");

            _logger.LogInformation("[CapTK] Bắt đầu — MST={MST}", mst);

            // ── Bước 1: CHECK ─────────────────────────────────────────────────
            var checkResult = await StepCheckAsync(mst, request.CMND_CCCD, result);
            if (!checkResult.ok)
                return result;
            string cnEVATNew = checkResult.cnEVATNew;
            string ipEVATNew = checkResult.ipEVATNew;
            string ipERP = checkResult.ipERP;
            string cn234 = checkResult.cn234;
            result.CheckOK = true;
            // ── Bước 2: DATABASE (ImportTools_V1 trên Server234) ──────────────
            var dbOk = await StepDatabaseAsync(mst, request, cnEVATNew, result);
            if (!dbOk) return result;
            result.DatabaseOK = true;
            // ── Bước 3: WEBAPP (quanly.wininvoice.vn/api/init_cmpn) ──────────
            var webOk = await StepWebAppAsync(mst, request, cnEVATNew, ipEVATNew, ipERP, result);
            if (!webOk) return result;
            result.WebAppOK = true;
            result.IsSuccess = true;
            result.Message = "Tài khoản khách hàng đã tạo thành công!";
            _logger.LogInformation("[CapTK] Hoàn thành — MST={MST}", mst);
            return result;
        }
        #region Helper Step
        private async Task<(bool ok, string cnEVATNew, string ipEVATNew, string ipERP, string cn234)>
            StepCheckAsync(string mst, string cccd, CreateAccountResponseDto result)
        {
            var empty = (false, "", "", "", "");

            // 1a. Gọi SP 1 lần duy nhất
            var raw = _connectionRepo.GetServerInfo(mst, cccd);
            if (raw == null)
            {
                result.Message = "Không thể kết nối tới bosConfigure. Hãy liên hệ kỹ thuật.";
                result.ErrorDetail = "GetServerInfo trả về null — kiểm tra ConnectionStrings:BosConfigure.";
                _logger.LogWarning("[CapTK][Check] SP unreachable — MST={MST}", mst);
                return empty;
            }

            // 1b. Kiểm tra MST đã có tài khoản chưa
            if (raw.IsExistingCustomer)
            {
                result.Message = $"MST {mst} đã có tài khoản trên server {raw.SideServer}.";
                result.ErrorDetail = "Nếu muốn cập nhật, dùng AllowUpdate = \"1\".";
                _logger.LogWarning("[CapTK][Check] MST đã tồn tại — MST={MST} Server={Server}", mst, raw.SideServer);
                return empty;
            }

            // 1c. Decrypt password từ keyWork
            string password = Sha1.Decrypt(raw.KeyWork);
            if (string.IsNullOrEmpty(password))
            {
                result.Message = "Không thể giải mã keyWork từ server. Hãy liên hệ kỹ thuật.";
                result.ErrorDetail = "Sha1.Decrypt(keyWork) trả về rỗng.";
                _logger.LogWarning("[CapTK][Check] Decrypt keyWork failed — MST={MST}", mst);
                return empty;
            }

            // 1d. Validate các server cần thiết đều có giá trị
            if (string.IsNullOrEmpty(raw.INVnew))
            {
                result.Message = "Không xác định được server cấp tài khoản (INVnew). Hãy liên hệ kỹ thuật.";
                result.ErrorDetail = "INVnew rỗng trong kết quả SP.";
                _logger.LogWarning("[CapTK][Check] INVnew empty — MST={MST}", mst);
                return empty;
            }

            if (string.IsNullOrEmpty(raw.ERP))
            {
                result.Message = "Không xác định được server ERP. Hãy liên hệ kỹ thuật.";
                result.ErrorDetail = "ERP rỗng trong kết quả SP.";
                _logger.LogWarning("[CapTK][Check] ERP empty — MST={MST}", mst);
                return empty;
            }

            // 1e. Server234 — chạy ImportTools_V1
            string cn234 = _connectionRepo.GetConnectionStringServer234();
            if (string.IsNullOrEmpty(cn234))
            {
                result.Message = "Chưa cấu hình Server234. Hãy kiểm tra appsettings.";
                result.ErrorDetail = "ConnectionStrings:Server234 rỗng.";
                _logger.LogWarning("[CapTK][Check] Server234 missing.");
                return empty;
            }

            // Khách mới → luôn dùng INVnew (đúng case EVATNEW trong file gốc)
            string cnEVATNew = BuildConnectionString(raw.INVnew, "BosEVAT", password);

            _logger.LogInformation(
                "[CapTK][Check] OK — MST={MST} | INVnew={INVnew} | ERP={ERP}",
                mst, raw.INVnew, raw.ERP);

            return (true, cnEVATNew, raw.INVnew, raw.ERP, cn234);
        }

        private async Task<bool> StepDatabaseAsync(
            string mst,
            CreateAccountRequestDto request,
            string cn234,
            CreateAccountResponseDto result)
        {
            try
            {
                var dbParams = new CapTaiKhoanDbParams
                {
                    MaSoThue = mst,
                    TenCongTy = request.TenCongTy,
                    DiaChi = request.DiaChi,
                    SoTaiKhoanNH = request.SoTaiKhoanNH,
                    TenNganHang = request.TenNganHang,
                    SoDienThoai = request.SoDienThoai,
                    UyQuyen = request.UyQuyen,
                    Email = request.Email,
                    Website = request.Website,
                    Password = Sha1.Encrypt(mst)
                };

                await _capTKRepo.CapTaiKhoanDatabaseAsync(dbParams, cn234);
                _logger.LogInformation("[CapTK][Database] ImportTools_V1 OK — MST={MST}", mst);
                return true;
            }
            catch (Exception ex)
            {
                result.Message = "Lỗi ở bước cấp tài khoản database (ImportTools_V1).";
                result.ErrorDetail = ex.Message;
                _logger.LogError(ex, "[CapTK][Database] ImportTools_V1 FAIL — MST={MST}", mst);
                return false;
            }
        }

        private async Task<bool> StepWebAppAsync(
            string mst,
            CreateAccountRequestDto request,
            string cnEVATNew,
            string ipEVATNew,
            string ipERP,
            CreateAccountResponseDto result)
        {
            try
            {
                // 3a. Lấy MerchantID
                string merchantId = await _capTKRepo.GetMerchantIDAsync(mst, cnEVATNew);
                if (string.IsNullOrEmpty(merchantId))
                {
                    result.Message = "Không tìm thấy MerchantID sau khi cấp TK database. Hãy kiểm tra lại.";
                    result.ErrorDetail = "BosEVAT..EVat_CompanyInfo không có record với TaxNumber = " + mst;
                    _logger.LogWarning("[CapTK][WebApp] MerchantID not found — MST={MST}", mst);
                    return false;
                }

                // 3b. Lấy UserCode từ bosConfigure..bosUser
                string userCode = await _capTKRepo.GetUserCodeAsync(mst, cnEVATNew);

                // 3c. Build credentials
                string userName = Sha1.Encrypt(mst);
                string password = Sha1.Encrypt(mst);
                string secret = Sha1.Encrypt(Guid.NewGuid().ToString().Replace("-", ""));

                // 3d. Build JSON payload
                var dbPublic = new JObject
                {
                    ["server"] = Sha1.Encrypt(ipEVATNew),   // IP EVATNEW
                    ["user"] = _dbCredUser,
                    ["password"] = _dbCredPassword,
                    ["dbName"] = "BosControlEVAT"
                };

                var dbEInvoice = new JObject
                {
                    ["server"] = Sha1.Encrypt(ipEVATNew),   // IP EVATNEW
                    ["user"] = _dbCredUser,
                    ["password"] = _dbCredPassword,
                    ["dbName"] = Sha1.Encrypt("BosEVAT")    
                };

                var dbEContract = new JObject
                {
                    ["server"] = Sha1.Encrypt(ipERP),       // IP ERP
                    ["user"] = _dbCredUser,
                    ["password"] = _dbCredPassword,
                    ["dbName"] = "BosControlEVAT"
                };

                var payload = new JObject
                {
                    ["username"] = userName,
                    ["password"] = password,
                    ["taxcode"] = mst,
                    ["cmpnName"] = request.TenCongTy.ToUpper(),
                    ["cmpnKey"] = userName,
                    ["clientSecret"] = secret,
                    ["allowUpdate"] = request.AllowUpdate,
                    ["cmpnID"] = merchantId,
                    ["descript"] = $"Create by: BosEVAT -| UserCode: {userCode}",
                    ["userCode"] = userCode,
                    ["isUseWeb"] = "1",
                    ["dbPublic"] = dbPublic,
                    ["dbEInvoice"] = dbEInvoice,
                    ["dbEContract"] = dbEContract
                };
                // 3e. Gọi REST API
                var clientOptions = new RestClientOptions(_webAppBaseUrl)
                {
                    Authenticator = new HttpBasicAuthenticator(_webAppUser, _webAppPassword)
                };
                using var restClient = new RestClient(clientOptions);
                var restRequest = new RestRequest("/api/init_cmpn", Method.Post);
                restRequest.AddStringBody(payload.ToString(), ContentType.Json);
                RestResponse response = await restClient.ExecuteAsync(restRequest);
                // 3f. Parse kết quả
                return ParseWebAppResponse(response, mst, result);
            }
            catch (Exception ex)
            {
                result.Message = "Lỗi ở bước tạo tài khoản WebApp.";
                result.ErrorDetail = ex.Message;
                _logger.LogError(ex, "[CapTK][WebApp] Exception — MST={MST}", mst);
                return false;
            }
        }

        private bool ParseWebAppResponse(RestResponse response, string mst, CreateAccountResponseDto result)
        {
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                result.Message = "WebApp không trả về dữ liệu.";
                result.ErrorDetail = $"HTTP {(int?)response.StatusCode} — {response.ErrorMessage}";
                return false;
            }
            try
            {
                var parsed = JObject.Parse(response.Content);
                bool isOk = parsed["isSuccess"]?.ToString().ToLower() == "true";

                if (isOk)
                {
                    _logger.LogInformation("[CapTK][WebApp] isSuccess=true — MST={MST}", mst);
                    return true;
                }
                string errMsg = parsed["errorMessage"]?.ToString() ?? "";
                if (errMsg.ToLower().Contains("username existed"))
                {
                    _logger.LogInformation("[CapTK][WebApp] Username existed — coi như OK — MST={MST}", mst);
                    return true;
                }

                result.Message = "Tạo tài khoản WebApp không thành công.";
                result.ErrorDetail = $"{errMsg} | raw: {response.Content}";
                _logger.LogWarning("[CapTK][WebApp] FAIL — MST={MST} | {Err}", mst, errMsg);
                return false;
            }
            catch (Exception ex)
            {
                result.Message = "Không thể đọc kết quả từ WebApp.";
                result.ErrorDetail = $"Parse error: {ex.Message} | raw: {response.Content}";
                return false;
            }
        }
        public CheckServerResponseDto CheckServer(string mst, string? cccd)
        {
            mst = mst.Replace(" ", "");

            var raw = _connectionRepo.GetServerInfo(mst, cccd);
            string cn234 = _connectionRepo.GetConnectionStringServer234();

            if (raw == null)
                return new CheckServerResponseDto
                {
                    MST = mst,
                    SPReachable = false,
                    Server234_OK = !string.IsNullOrEmpty(cn234)
                };

            return new CheckServerResponseDto
            {
                MST = mst,
                SPReachable = true,
                IsExistingCustomer = raw.IsExistingCustomer,
                SideServer = raw.SideServer,
                INVnew = raw.INVnew,
                TVAN = raw.TVAN,
                ERP = raw.ERP,
                Server234_OK = !string.IsNullOrEmpty(cn234)
            };
        }
        private static string BuildConnectionString(string server, string catalog, string password) =>
            $"Server={server};" +
            $"Initial Catalog={catalog};" +
            $"Persist Security Info=False;" +
            $"User ID=bosR;" +
            $"Password={password};" +
            $"MultipleActiveResultSets=False;" +
            $"Encrypt=True;" +
            $"TrustServerCertificate=True;" +
            $"Connection Timeout=3600;";
        #endregion
    }
}
