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

        /// <summary>
        /// Từ khoá tìm kiếm duy nhất — SP sẽ tìm trên CusName / CusTax / OID.
        /// Bind trực tiếp từ query string: ?SearchKeyword=...
        /// </summary>
        public string? SearchKeyword { get; set; }

        // Filter trạng thái
        public string? Status { get; set; }

        public bool isManager { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
