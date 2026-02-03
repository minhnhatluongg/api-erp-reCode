using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IDSignaturesRepository
    {
        Task<DSMenuViewModel> GetDSMenuByID(string loginname, string grp_code);
        Task<DigitalSignaturesResult> CountCKS(string search, string crtUser, string dateStart, string dateEnd);
    }
}
