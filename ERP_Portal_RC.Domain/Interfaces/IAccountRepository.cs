using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IAccountRepository
    {
        /// <summary>
        /// Lấy thông tin user từ BosOnline database theo LoginName
        /// </summary>
        /// <param name="loginName">Tên đăng nhập</param>
        /// <returns>Danh sách UserOnAp</returns>
        Task<IEnumerable<UserOnAp>> GetUserByLoginNameAsync(string loginName);

        /// <summary>
        /// Lấy menu applications theo group user
        /// </summary>
        /// <param name="groupList">Danh sách group của user</param>
        /// <returns>Danh sách menu</returns>
        Task<IEnumerable<ApplicationToolMenu>> GetApplicationMenuByGroupAsync(string groupList);

        /// <summary>
        /// Lấy menu applications theo group user và app site
        /// </summary>
        /// <param name="groupList">Danh sách group</param>
        /// <param name="appSite">Application site (Bos, EContract, etc.)</param>
        /// <returns>Danh sách menu</returns>
        Task<IEnumerable<ApplicationToolMenu>> GetApplicationMenuByGroupAndSiteAsync(string groupList, string appSite);
    }
}
