using ERP_Portal_RC.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ISalesHierarchyService
    {
        Task<List<ManagerDto>> GetManagerTreeAsync(string clnID, bool isManager);
    }
}
