using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IEContractRepository
    {
        Task<DSMenuViewModel> GetDSMenuByID(string loginName, string grpCode);
        Task<ListEcontractViewModel> Search(string search, string crtUser, string dateStart, string dateEnd);
        Task<ListEcontractViewModel> GetAllList(string crtUser, string dateStart, string dateEnd);
        Task<ListEcontractViewModel> CountList(string crtUser, string dateStart, string dateEnd);
        Task CreateLog(string message, string userCode);
    }
}
