using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.AccountKeToan;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class SalesHierarchyService : ISalesHierarchyService
    {
        private readonly ISalesHierarchyRepository _salesHierarchyRepository;
        private readonly ICustomStore _customStore;
        private readonly IRegistrationCodeService _registrationCodeService;
        private const string AccountingGrpCode = "00006.00063.00121";

        public SalesHierarchyService(ISalesHierarchyRepository salesHierarchyRepository, ICustomStore customStore, IRegistrationCodeService registrationCodeService)
        {
            _salesHierarchyRepository = salesHierarchyRepository;
            _customStore = customStore;
            _registrationCodeService = registrationCodeService;
        }
        public async Task<List<ManagerDto>> GetManagerTreeAsync(string clnID, bool isManager)
        {
            var rawData = await _salesHierarchyRepository.GetRawSalesTreeAsync(clnID);
            var filteredData = isManager ? rawData.Where(x => x.IsGroup) : rawData;
            var processedList = filteredData
                .GroupBy(x => x.ItemID)
                .Select(g => g.First())
                .ToList();
            var loginMap = await _salesHierarchyRepository
                .GetLoginNameBatchAsync(processedList.Select(x => x.ItemID));
            var map = processedList.ToDictionary(x => x.ItemID, x =>
            {
                loginMap.TryGetValue(x.ItemID, out var loginName);
                return new ManagerDto
                {
                    Id = x.ItemID,
                    Name = x.ItemName,
                    Level = x.LEVEL_VAL,
                    SortID = x.SortID,
                    IsGroup = x.IsGroup,
                    LoginName = loginName ?? ""
                };
            });

            var tree = new List<ManagerDto>();
            foreach (var item in processedList)
            {
                var dto = map[item.ItemID];
                string parentId = GetImmediateParentId(item.ParentIDSortID);

                if (!string.IsNullOrEmpty(parentId) && map.ContainsKey(parentId))
                    map[parentId].Children.Add(dto);
                else if (string.IsNullOrEmpty(item.PARENTID) || item.PARENTID == "*****")
                    tree.Add(dto);
                else if (!map.ContainsKey(parentId))
                    tree.Add(dto);
            }
            return tree;
        }

        public async Task<AccountingRegistrationResultDto> HandleAccountingRegistrationAsync(AccountingRegistrationRequestDto request)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(request.LoginName))
                throw new ArgumentException("LoginName không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password không được để trống.");
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("FullName không được để trống.");

            // Bước 1: Tạo ERP account qua bosInsertUserOnApp
            var userCode = await _salesHierarchyRepository.CreateERPAccountOnlyAsync(
                loginName: request.LoginName,
                password: request.Password,
                fullName: request.FullName,
                email: request.Email,
                emplId: request.EmplId  
            );

            if (string.IsNullOrWhiteSpace(userCode))
                throw new Exception("Tạo tài khoản thất bại: SP không trả về UserCode.");

            // Bước 2: Gán vào group Kế toán
            await _salesHierarchyRepository.AssignUserToGroupAsync(userCode, AccountingGrpCode);

            return new AccountingRegistrationResultDto
            {
                UserCode = userCode,
                LoginName = request.LoginName,
                GrpCode = AccountingGrpCode,
                Message = $"Tạo tài khoản kế toán thành công. UserCode: {userCode}"
            };
        }

        public async Task<RegistrationResultDto> HandleSaleRegistrationAsync(SaleRegistrationModel request)
        {
            if (string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Email))
                throw new ArgumentException("FullName, Email là bắt buộc.");

            // ManagerEmplID bắt buộc — thiếu thì HmrWorkingProcess không insert được
            // → nhân viên không có cây ASM → không thấy hợp đồng nào
            if (string.IsNullOrWhiteSpace(request.ManagerEmplID))
                throw new ArgumentException("ManagerEmplID (mã quản lý trực tiếp) là bắt buộc. Không thể đăng ký nhân sự khi chưa chỉ định quản lý.");
            //var isValid = await _registrationCodeService.ValidateCodeAsync(request.RegistrationCode);
            //if (!isValid)
            //{
            //    throw new ArgumentException("Mã đăng ký không hợp lệ, đã được sử dụng hoặc hết hạn.");
            //}
            //await _registrationCodeService.ValidateAndUseCodeAsync(request.RegistrationCode, request.Email);
            if (request.IsCreateAccount)
            {
                if (request.LoginName?.Length < 5) throw new Exception("Tên đăng nhập phải từ 5 ký tự.");
                if (request.Password?.Length < 6) throw new Exception("Mật khẩu phải từ 6 ký tự.");

                // ── Check LoginName, tự sinh hậu tố nếu trùng ──────────────
                string baseLoginName = request.LoginName!;
                int suffix = 1;
                while (await _customStore.ChkUser(request.LoginName!) > 0)
                {
                    if (suffix > 10)
                        throw new Exception($"Không thể sinh tên đăng nhập cho '{baseLoginName}'. Vui lòng chọn tên khác.");
                    request.LoginName = $"{baseLoginName}_{suffix++}";
                }

                // ── Check Email: bosInsertUserOnApp có thể match theo email
                //    → nếu email đã tồn tại, SP trả về UserCode cũ thay vì tạo mới
                int emailExists = await _customStore.ChkUserByEmail(request.Email!);
                if (emailExists > 0)
                    throw new Exception($"Email '{request.Email}' đã được đăng ký trong hệ thống. Vui lòng dùng email khác.");
            }

            string newEmplId = await _salesHierarchyRepository.RegisterSaleHierarchyAsync(request, "000642");

            string? newUserCode = null;

            // Tạo tk ERP nếu tick sử dụng
            if (request.IsCreateAccount)
            {
                // CreateERPAccountOnlyAsync tự check bosUser trước khi gọi SP
                // → luôn trả về emplId (dù SP có match sai cũng không ảnh hưởng)
                newUserCode = await _salesHierarchyRepository.CreateERPAccountOnlyAsync(
                    request.LoginName!,
                    request.Password!,
                    request.FullName!,
                    request.Email!,
                    newEmplId);

                // Thêm vào group theo emplId
                await _customStore.AddUserToGroup(newEmplId);
            }

            // Gọi API tạo TK trên hệ thống bên ngoài
            string? externalWarning = null;
            if (request.IsCreateAccount)
            {
                try
                {
                    var (success, errorMessage) = await _salesHierarchyRepository.CreateHRAccountAsync(
                        request.FullName!,
                        request.Email!,
                        request.Phone ?? "",
                        request.LoginName!,
                        request.Password!,
                        newEmplId);

                    if (!success)
                    {
                        externalWarning = $"Tạo TK hệ thống ngoài thất bại: {errorMessage}";
                    }
                }
                catch (Exception ex)
                {
                    externalWarning = $"Tạo TK hệ thống ngoài thất bại: {ex.Message}";
                }
            }

            return new RegistrationResultDto
            {
                NewEmployeeID      = newEmplId,
                NewUserCode        = newUserCode,
                LoginNameUsed      = request.IsCreateAccount ? request.LoginName : null,
                ExternalApiWarning = externalWarning,
            };
        }

        private string GetImmediateParentId(string parentIDSortID)
        {
            if (string.IsNullOrEmpty(parentIDSortID)) return "";
            var parts = parentIDSortID.Split('.');
            return parts.Last();
        }

        public async Task<ApiResponse<object>> RetryCreateHrAccountAsync(CreateHrAccountRequest request)
        {
            if (request == null) return ApiResponse<object>.ErrorResponse("Thiếu dữ liệu.");
            if (string.IsNullOrWhiteSpace(request.EmplId)) return ApiResponse<object>.ErrorResponse("Thiếu mã nhân viên (EmplId).");
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
                return ApiResponse<object>.ErrorResponse("Thiếu Họ tên / Email.");
            if ((request.LoginName ?? "").Trim().Length < 5) return ApiResponse<object>.ErrorResponse("Tài khoản đăng nhập phải ≥ 5 ký tự.");
            if ((request.Password ?? "").Length < 6) return ApiResponse<object>.ErrorResponse("Mật khẩu phải ≥ 6 ký tự.");

            try
            {
                // 1) Đảm bảo TK ERP (bosUser) tồn tại — idempotent, đã có thì trả về luôn.
                try
                {
                    await _salesHierarchyRepository.CreateERPAccountOnlyAsync(
                        request.LoginName.Trim(), request.Password, request.FullName, request.Email, request.EmplId);
                }
                catch { /* không chặn bước tạo TK ngoài */ }

                // 2) Tạo TK hệ thống ngoài (LOT ERP) — đây là bước hay bị lỗi đồng bộ.
                var (success, err) = await _salesHierarchyRepository.CreateHRAccountAsync(
                    request.FullName, request.Email, request.Phone ?? "",
                    request.LoginName.Trim(), request.Password, request.EmplId);

                if (!success)
                    return ApiResponse<object>.ErrorResponse("Tạo TK hệ thống ngoài thất bại: " + err, 502);

                return ApiResponse<object>.SuccessResponse(
                    new { emplId = request.EmplId, loginName = request.LoginName.Trim() },
                    "Tạo lại TK hệ thống ngoài (LOT ERP) thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Lỗi tạo lại TK hệ thống ngoài: " + ex.Message, 500);
            }
        }
    }
}
