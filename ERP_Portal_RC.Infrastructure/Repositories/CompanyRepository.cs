using Dapper;
using ERP_Portal_RC.Domain.EntitiesIntergration;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BOSCONFIGURE = "BosConfigure";
        public CompanyRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task<CompanyInitResult> InitCompanyV2Async(CompanyInitRequest request)
        {
            using var connection = _dbConnectionFactory.GetConnection(BOSCONFIGURE);

            var p = new DynamicParameters();

            // Các tham số chính
            p.Add("@cmpnKey", request.CmpnKey ?? "");
            p.Add("@password", request.Password);
            p.Add("@compName", request.CompName);
            p.Add("@director", request.Director);
            p.Add("@tel", request.Tel);
            p.Add("@fax", request.Fax);
            p.Add("@address", request.Address);
            p.Add("@email", request.Email);
            p.Add("@website", request.Website);
            p.Add("@taxCode", request.TaxCode);
            p.Add("@bankNumber", request.BankNumber);
            p.Add("@bankName", request.BankName);
            p.Add("@crtUser", request.CrtUser);
            p.Add("@bosGroupTemplate", request.BosGroupTemplate);

            // Các tham số điều khiển logic (mặc định cho tạo mới)
            p.Add("@isUpdate", "0");
            p.Add("@cmpnIDToUpdate", ""); 
            p.Add("@isSite", request.IsSite);
            p.Add("@ParentSite", request.ParentSite ?? "21");
            p.Add("@isGroup", request.IsGroup);
            p.Add("@SaleID", request.SaleID ?? "");
            p.Add("@QttyInv", request.QttyInv);
            p.Add("@IsCheckQttyInv", request.IsCheckQttyInv);

            return await connection.QueryFirstOrDefaultAsync<CompanyInitResult>(
                "[dbo].[InitCompany_V2]",
                p,
                commandType: CommandType.StoredProcedure
            );

        }

        //public async Task<bool> UpdateCompanyV2Async(CompanyInitRequest request)
        //{
        //    using var connection = _dbConnectionFactory.GetConnection(BOSCONFIGURE);

        //    var p = new DynamicParameters();

        //    // Các tham số chính
        //    p.Add("@cmpnKey", request.CmpnKey ?? "");
        //    p.Add("@password", request.Password);
        //    p.Add("@compName", request.CompName);
        //    p.Add("@director", request.Director);
        //    p.Add("@tel", request.Tel);
        //    p.Add("@fax", request.Fax);
        //    p.Add("@address", request.Address);
        //    p.Add("@email", request.Email);
        //    p.Add("@website", request.Website);
        //    p.Add("@taxCode", request.TaxCode);
        //    p.Add("@bankNumber", request.BankNumber);
        //    p.Add("@bankName", request.BankName);
        //    p.Add("@crtUser", request.CrtUser);
        //    p.Add("@bosGroupTemplate", request.BosGroupTemplate);

        //    // Các tham số điều khiển logic (mặc định cho tạo mới)
        //    p.Add("@isUpdate", "1");
        //    p.Add("@cmpnIDToUpdate", ""); //req.CmpnIDToUpdate -> Tạo entity update sau.
        //    p.Add("@isSite", request.IsSite);
        //    p.Add("@ParentSite", request.ParentSite ?? "21");
        //    p.Add("@isGroup", request.IsGroup);
        //    p.Add("@SaleID", request.SaleID ?? "");
        //    p.Add("@QttyInv", request.QttyInv);
        //    p.Add("@IsCheckQttyInv", request.IsCheckQttyInv);

        //    return await connection.QueryFirstOrDefaultAsync<CompanyInitResult>(
        //        "[dbo].[InitCompany_V2]",
        //        p,
        //        commandType: CommandType.StoredProcedure
        //    );
        //}
    }
}
