using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ICreateAccountRepository
    {
        Task CapTaiKhoanDatabaseAsync(CapTaiKhoanDbParams p, string cnEVATNew);
        Task<string> GetMerchantIDAsync(string maSoThue, string cnEVATNew);
        Task<string> GetUserCodeAsync(string maSoThue, string cnEVATNew);
    }
}
