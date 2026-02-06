using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
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
        Task<RegistrationResultDto> HandleSaleRegistrationAsync(SaleRegistrationModel request);
    }
}
