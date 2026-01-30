using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using static ERP_Portal_RC.Application.DTOs.MenuResponseViewDto;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IAccountService
    {
        Task<MenuResponseDto> GetUserMenuAsync(string userName, string groupList, string cmpnId, string? appSite = null);
        Dictionary<string, AppLoginInfo> ParseApiLoginString(string apiLoginString);
    }
}
