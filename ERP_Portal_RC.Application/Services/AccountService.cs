using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using static ERP_Portal_RC.Application.DTOs.MenuResponseViewDto;
namespace ERP_Portal_RC.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ICustomStore _customStore;

        public AccountService(IAccountRepository accountRepository, ICustomStore customStore)
        {

            _accountRepository = accountRepository;
            _customStore = customStore;
        }

        public async Task<MenuResponseDto> GetUserMenuAsync(string userName, string groupList, string cmpnId, string? appSite = null)
        {
            var response = new MenuResponseDto { IsShowMenu = true };

            // 1. Lấy thông tin user
            var userInfoList = await _customStore.GetUserByLoginNameAsync(userName, cmpnId);
            var userInfo = userInfoList.FirstOrDefault();

            // 2. Logic làm sạch GroupList (Theo API gốc)
            var cleanedGroupList = groupList?.Replace("'", string.Empty) ?? string.Empty;
            if (cleanedGroupList == "E15.067.09732") cleanedGroupList = string.Empty; //

            // 3. Parse API login config
            Dictionary<string, AppLoginInfo> appConfigs = new();
            if (userInfo?.APIlogin != null)
            {
                appConfigs = ParseApiLoginString(userInfo.APIlogin);
            }

            if (!string.IsNullOrEmpty(appSite) && appSite != "undefined")
            {
                if (appSite == "EContract")
                {
                    cleanedGroupList = string.Empty;
                }
            }

            // 5. Thiết lập Permissions và Links (Dựa trên logic cũ)
            SetAppPermissionsAndLinks(response, appConfigs, userInfo, appSite, ref cleanedGroupList);

            // 6. Lấy Menu từ Database sau khi đã làm sạch GroupList
            var rawMenu = await _accountRepository.GetApplicationMenuByGroupAsync(cleanedGroupList);

            // 7. Lọc dữ liệu theo AppSite (Nếu có)
            var filteredMenu = rawMenu;
            if (!string.IsNullOrEmpty(appSite) && appSite != "undefined")
            {
                filteredMenu = rawMenu.Where(m =>
                    (m.AppID != null && m.AppID.Equals(appSite, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Param01 != null && m.Param01.Contains(appSite, StringComparison.OrdinalIgnoreCase)) ||
                    (string.IsNullOrEmpty(m.ParentID) || m.ParentID == "00")
                ).ToList();
            }

            // 8. Mapping sang DTO từ danh sách ĐÃ LỌC (filteredMenu)
            bool isManager = rawMenu.Any(m => m.AcssForm == "WebContractFrom");

            var flatMenu = filteredMenu.Select(m => {
                var dto = new MenuDto
                {
                    MenuID = m.MenuID,
                    ParentID = m.ParentID ?? string.Empty,
                    MenuDscpt = m.MenuDscpt,
                    MenuIcon = m.MenuIcon,
                    AcssForm = m.AcssForm,
                    IsGroup = m.IsGroup,   
                    IsFunct = m.IsFunct,
                    InToolBar = m.InToolBar,
                    MnCtType = m.MnCtType,
                    AcssRght = m.AcssRght,
                    ViewRght = m.ViewRght,
                    Params = new Dictionary<string, string>(),
                    Variants = new Dictionary<string, string>() // Thêm Dictionary cho Variants
                };

                // Gom Params có giá trị
                for (int i = 1; i <= 50; i++)
                {
                    var val = m.GetType().GetProperty($"Param{i:D2}")?.GetValue(m)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) dto.Params.Add($"Param{i:D2}", val);
                }

                // Gom Variants có giá trị [CẬP NHẬT MỚI]
                for (int i = 1; i <= 30; i++)
                {
                    var val = m.GetType().GetProperty($"Variant{i:D2}")?.GetValue(m)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) dto.Variants.Add($"Variant{i:D2}", val);
                }

                return dto;
            }).ToList();

            // 9. Trồng cây
            response.Menu = BuildMenuTree(flatMenu);
            response.TotalMenuItems = flatMenu.Count;
            response.IsManager = isManager;
            return response;
        }

        public Dictionary<string, AppLoginInfo> ParseApiLoginString(string apiLoginString)
        {
            var result = new Dictionary<string, AppLoginInfo>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(apiLoginString))
                return result;

            try
            {
                var appBlocks = apiLoginString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var block in appBlocks)
                {
                    var appInfo = new AppLoginInfo { IsEnabled = true };
                    var parts = block.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var keyValue = part.Split(new[] { '=' }, 2);
                        if (keyValue.Length < 2) continue;

                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        switch (key)
                        {
                            case "App":
                                appInfo.AppName = value;
                                break;
                            case "AppUrl":
                                appInfo.AppUrl = value;
                                break;
                            case "AppLoginName":
                                appInfo.LoginName = value;
                                break;
                            case "AppLoginPassword":

                                appInfo.Password = Sha1.Decrypt(value);
                                appInfo.Password = value;
                                break;
                        }
                    }

                    // 3. Chỉ add vào Dictionary nếu có AppName hợp lệ
                    if (!string.IsNullOrEmpty(appInfo.AppName))
                    {
                        result[appInfo.AppName] = appInfo;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        #region HelperMethod
        private void SetAppPermissionsAndLinks(MenuResponseDto response, Dictionary<string, AppLoginInfo> appConfigs, UserOnAp? userInfo, string? appSite, ref string cleanedGroupList)
        {
            bool winBos = appConfigs.ContainsKey("WINBOS");
            bool winEContract = appConfigs.ContainsKey("WINECONTRACT");
            bool winEco = appConfigs.ContainsKey("WINECO");
            bool winInvoiceIn = appConfigs.ContainsKey("WININVOICE_IN");
            bool winInvoice = appConfigs.ContainsKey("WININVOICE");

            response.IsBos = winBos;
            response.IsWINECONTRACT = winEContract;
            response.IsINVOICE_IN = winInvoiceIn;
            response.IsINVOICE = winInvoice;

            // Logic xử lý Links
            if (winInvoiceIn && appConfigs.TryGetValue("WININVOICE_IN", out var invcInInfo))
            {
                var decryptedPassword = Sha1.TryDecrypt(invcInInfo.Password) ?? invcInInfo.Password;
                response.LinkInvc_In = $"{invcInInfo.LoginName}|-|-|{decryptedPassword}|-|-|";
                if (!winEco && !winBos && !winEContract) response.IsDirect_In = true;
            }

            if (winInvoice && appConfigs.TryGetValue("WININVOICE", out var invcInfo))
            {
                var decryptedPassword = Sha1.TryDecrypt(invcInfo.Password) ?? invcInfo.Password;
                response.LinkInvc = $"{invcInfo.LoginName}|-|-|{decryptedPassword}|-|-|";
                if (!winEco && !winBos && !winEContract) response.IsDirect = true;
            }

            if (!string.IsNullOrEmpty(appSite))
            {
                if (appSite == "EContract")
                {
                    response.IsWINECONTRACT = true;
                    response.IsShowECLi = true;
                    response.IsShowMenuE = true;
                    response.IsShowSign = true;
                    cleanedGroupList = string.Empty;
                }
                else if (appSite == "Bos")
                {
                    response.IsBos = true;
                    if (cleanedGroupList == "E15.067.09729") response.IsShowMenu = false;
                    response.IsShowType = true;
                }
            }
        }
        private List<MenuDto> BuildMenuTree(List<MenuDto> flatMenu)
        {
            if (flatMenu == null || !flatMenu.Any()) return new List<MenuDto>();

            var map = flatMenu.GroupBy(m => m.MenuID)
                              .ToDictionary(g => g.Key, g => g.First());

            var tree = new List<MenuDto>();

            foreach (var item in flatMenu)
            {
                if (!string.IsNullOrEmpty(item.ParentID) && map.ContainsKey(item.ParentID))
                {
                    var parent = map[item.ParentID];
                    if (!parent.Children.Any(c => c.MenuID == item.MenuID))
                        parent.Children.Add(item);
                }
                else
                {
                    tree.Add(item);
                }
            }
            return tree;
        }

        #endregion
    }
}
