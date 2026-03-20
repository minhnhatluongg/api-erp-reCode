using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class CapTaiKhoanRepository : ICreateAccountRepository
    {
        public async Task CapTaiKhoanDatabaseAsync(CapTaiKhoanDbParams p, string cnEVATNew)
        {
            using var con = new SqlConnection(cnEVATNew);
            await con.OpenAsync();

            var param = new DynamicParameters();
            param.Add("@CsTaxCode", p.MaSoThue);
            param.Add("@CsName", p.TenCongTy);
            param.Add("@CsNameBrif", "");            
            param.Add("@CsDirector", "");            
            param.Add("@CsAddress", p.DiaChi);
            param.Add("@CsBankNumber", p.SoTaiKhoanNH);
            param.Add("@CsBankAddress", p.TenNganHang);
            param.Add("@CsTel", p.SoDienThoai);
            param.Add("@CsFax", p.UyQuyen);    
            param.Add("@CsEmail", p.Email);
            param.Add("@CsWebSite", p.Website);
            param.Add("@Password", p.Password);

            await con.ExecuteAsync(
                "BosCataloge..ImportTools_V1",
                param,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 3600);
        }

        public async Task<string> GetMerchantIDAsync(string maSoThue, string cnEVATNew)
        {
            using var con = new SqlConnection(cnEVATNew);

            const string sql = @"
                SELECT TOP 1 MerchantID
                FROM   BosEVAT..EVat_CompanyInfo WITH (NOLOCK)
                WHERE  TaxNumber = @TaxNumber";

            var result = await con.QueryFirstOrDefaultAsync<string>(
                sql, new { TaxNumber = maSoThue });
            return result ?? "";
        }

        public async Task<string> GetUserCodeAsync(string maSoThue, string cnEVATNew)
        {
            using var con = new SqlConnection(cnEVATNew);
            const string sql = @"
                SELECT TOP 1 UserCode
                FROM   bosConfigure..bosUser WITH (NOLOCK)
                WHERE  LoginName = @LoginName";
            var result = await con.QueryFirstOrDefaultAsync<string>(
                sql, new { LoginName = maSoThue });
            return result ?? "";
        }
    }
}
