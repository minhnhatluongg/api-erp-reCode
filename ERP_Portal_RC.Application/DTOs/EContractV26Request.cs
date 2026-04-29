namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Request cho API GetAllEcontract_v26 — dùng SP wspList_EContracts_PagedV26.
    /// Tất cả filter là tuỳ chọn.
    /// </summary>
    public class EContractV26Request
    {
        /// <summary>
        /// Từ ngày (yyyy-MM-dd). Mặc định: 2010-01-01.
        /// </summary>
        public string? FrmDate { get; set; }

        /// <summary>
        /// Đến ngày (yyyy-MM-dd). Mặc định: ngày mai.
        /// </summary>
        public string? ToDate { get; set; }

        /// <summary>
        /// Tìm kiếm theo tên KH (CusName), MST (CusTax), hoặc mã HĐ (OID).
        /// SP dùng LIKE nên không cần nhập đầy đủ.
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Filter theo trạng thái ký (SignNumb). Null = lấy tất cả.
        /// </summary>
        public int? StatusFilter { get; set; }

        /// <summary>
        /// Lọc theo SaleEmID cụ thể.
        /// Null / trống = lấy cả team của CrtUser.
        /// Truyền usercode (VD: "000642") = chỉ lấy HĐ của nhân viên đó.
        /// </summary>
        public string? FilterSaleEmID { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
