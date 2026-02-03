using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IEcontractService
    {
        // Lấy dữ liệu thống kê Dashboard cho EContract
        Task<DashboardStatsDto> GetContractDashboardAsync(
            string userCode,
            string userName,
            string grpList,
            bool isManager,
            ContractSearchRequest request);
        //Lấy danh sách hợp đồng đã qua lọc trạng thái cho Table
        Task<ListEcontractViewModel> GetContractListAsync(
            string userCode,
            string userName,
            string grpList,
            bool isManager,
            ContractSearchRequest request);
    }
}
