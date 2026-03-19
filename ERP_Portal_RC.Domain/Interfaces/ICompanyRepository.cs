using ERP_Portal_RC.Domain.EntitiesIntergration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ICompanyRepository
    {
        Task<CompanyInitResult> InitCompanyV2Async(CompanyInitRequest request);
        //Task<bool> UpdateCompanyV2Async(CompanyInitRequest request);
    }
}
