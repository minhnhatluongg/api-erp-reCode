using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IConnectionRepository
    {
        string GetCnServerByMST(string mst, string? cccd, string system);
        string GetIPServerByMST(string mst, string? cccd, string system);
    }
}
