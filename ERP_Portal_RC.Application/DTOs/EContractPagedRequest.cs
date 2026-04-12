using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractPagedRequest
    {
        public string? FrmDate { get; set; }
        public string? ToDate { get; set; }

        // Search theo thông tin khách hàng -> truyền vào SP @strSearch
        public string? CusTName { get; set; }
        public string? CusTTax { get; set; }
        public string? NCC { get; set; }
        public string? Kinhdoanh { get; set; }

        // Filter trạng thái -> xử lý sau khi SP trả về (như code cũ)
        public string? Status { get; set; }

        public bool isManager { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Chỉ dùng nội bộ - không phải search keyword
        public string? SearchKeyword => CusTName ?? CusTTax ?? NCC ?? Kinhdoanh;
    }
}
