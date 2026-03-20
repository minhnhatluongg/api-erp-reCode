using ERP_Portal_RC.Application.DTOs.Integration_Incom;
using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IIntegrationService
    {
        Task<ApiResponse<IntegrationResult>> ProcessEContractIntegrationAsync(EContractIntegrationRequestDto model, string crtUser);
    }
}
