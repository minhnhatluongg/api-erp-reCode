using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IEvatRepository
    {
        Task<EvatAccountInfo?> GetAccountByTaxcodeAsync(string connectionString, string mst, string? cccd);
    }
}
