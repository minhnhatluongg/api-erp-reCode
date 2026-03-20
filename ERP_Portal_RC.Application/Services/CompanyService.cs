using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.EntitiesIntergration;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepo;
        public CompanyService(ICompanyRepository companyRepository)
        {
            _companyRepo = companyRepository;
        }
        public async Task<CompanyInitResult> CreateCompanyAsync(CompanyInitRequest request)
        {
            var result = await _companyRepo.InitCompanyV2Async(request);

            if (result == null)
            {
                throw new Exception("Khởi tạo công ty thất bại: Store Procedure không trả về kết quả.");
            }

            if (result.CmpnID == "" && result.CmpnKey == "-1")
            {
                throw new Exception($"Mã định danh (CmpnKey) '{request.CmpnKey}' đã tồn tại trong hệ thống.");
            }

            if (result.CmpnID == "" && result.TaxCode == "-1")
            {
                throw new Exception($"Mã số thuế '{request.TaxCode}' đã tồn tại trong hệ thống.");
            }

            return result;
        }
    }
}
