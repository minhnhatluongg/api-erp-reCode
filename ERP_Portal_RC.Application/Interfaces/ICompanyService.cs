using ERP_Portal_RC.Domain.EntitiesIntergration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyInitResult> CreateCompanyAsync(CompanyInitRequest request);
    }
}
