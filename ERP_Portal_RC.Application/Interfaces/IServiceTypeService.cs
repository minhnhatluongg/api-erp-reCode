using ERP_Portal_RC.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IServiceTypeService
    {
        Task<IEnumerable<ServiceTypeDto>> GetAllAsync();
        Task<IEnumerable<ServiceTypeDto>> GetAllActiveAsync();
        Task<ServiceTypeDto?> GetByIdAsync(int serviceTypeId);
        Task<int> CreateAsync(CreateServiceTypeDto dto, string crtUser);
        Task<bool> UpdateAsync(int serviceTypeId, UpdateServiceTypeDto dto, string chgeUser);
        Task<bool> DeactivateAsync(int serviceTypeId);
    }
}
