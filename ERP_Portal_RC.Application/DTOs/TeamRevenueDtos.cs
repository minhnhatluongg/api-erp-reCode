using System;
using System.Collections.Generic;

namespace ERP_Portal_RC.Application.DTOs
{
    // ===== RESPONSE DTO (khớp UI "Quản lý team") =====
    // Raw rows (RevenueContractRow / RevenueEmplRow) nằm ở Domain.Entities.

    public class TeamRevenueResponse
    {
        public DateTime FrmDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }   // doanh thu ghi nhận (CurrSignNumb >= 301)
        public int TotalOrders { get; set; }
        public int ManagedCount { get; set; }        // số NV trong cây (trừ chính mình)
        public List<TeamEmployeeRevenue> Employees { get; set; } = new();
        public List<TeamContractItem> Contracts { get; set; } = new();
    }

    /// 1 dòng "Doanh thu theo nhân viên".
    public class TeamEmployeeRevenue
    {
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string Level { get; set; }
        public string ParentId { get; set; }
        public bool IsGroup { get; set; }
        public string SortId { get; set; }
        public int Orders { get; set; }              // số đơn ghi nhận (>= 301)
        public decimal Revenue { get; set; }         // doanh thu ghi nhận
    }

    /// 1 dòng tab "Hợp đồng của team" (tất cả đơn, không lọc trạng thái).
    public class TeamContractItem
    {
        public string Oid { get; set; }
        public string SaleEmId { get; set; }
        public string EmplName { get; set; }
        public DateTime? ODate { get; set; }
        public string CusName { get; set; }
        public string CusTax { get; set; }
        public string CmpnId { get; set; }
        public string CmpnName { get; set; }
        public decimal SumAmnt { get; set; }
        public int CurrSignNumb { get; set; }
        public int IsXHD { get; set; }
    }
}
