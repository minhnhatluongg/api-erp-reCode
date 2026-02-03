using ERP_Portal_RC.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IDSignaturesService
    {
        Task<DigitalSignaturesDashboardDto> GetCountDigitalSignaturesAsync(string loginName, string userCode, string groupList, bool isManager);
    }
}
