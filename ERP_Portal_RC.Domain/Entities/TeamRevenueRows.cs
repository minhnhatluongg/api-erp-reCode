using System;

namespace ERP_Portal_RC.Domain.Entities
{
    /// Result 1 của SP wspRevenue_SubEmpl_ByOrder: từng đơn + tổng tiền + trạng thái.
    public class RevenueContractRow
    {
        public string OID { get; set; }
        public string SaleEmID { get; set; }
        public string EmplName { get; set; }
        public DateTime? ODate { get; set; }
        public string CusName { get; set; }
        public string CusTax { get; set; }
        public string CmpnID { get; set; }
        public string CmpnName { get; set; }
        public decimal SumAmnt { get; set; }
        public int CurrSignNumb { get; set; }   // 301 = đã duyệt
        public int IsXHD { get; set; }           // 1 = đã xuất hóa đơn
    }

    /// Result 2: cây cấp dưới (đầy đủ).
    public class RevenueEmplRow
    {
        public string ParentEmployeeID { get; set; }
        public string EmployeeID { get; set; }
        public string hoten_V { get; set; }
        public string LevelVal { get; set; }
        public bool IsGroup { get; set; }
        public string SortID { get; set; }
    }
}
