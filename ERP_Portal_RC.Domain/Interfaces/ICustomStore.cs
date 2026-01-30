using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ICustomStore
    {
        /// Tìm ApplicationUser theo LoginName
        Task<IEnumerable<ApplicationUser>> FindByLoginNameAsync(string loginName);
        /// Lấy UserOnAp theo LoginName
        Task<IEnumerable<UserOnAp>> GetUserByLoginNameAsync(string loginName, string cmpnId);
        /// Check user tồn tại hay không
        Task<IEnumerable<int>> CheckUserByLoginNameAsync(string loginName);
        /// Check user tồn tại (sync version)
        int ChkUser(string loginName);
        /// Lấy menu theo group
        Task<IEnumerable<web_bosMenu_ByGroup>> GetApplicationToolsByGroupAsync(string groupList);
        /// Tạo user mới
        int CreateUser(ApplicationUser user);
        /// Thêm user vào group
        int AddUserToGroup(ApplicationUser user);
    }
}
