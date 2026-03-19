using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesIntergration
{
    public class CompanyInitRequest
    {
        // --- NHÓM 1: BẮT BUỘC TỪ API (User nhập) ---
        public string TaxCode { get; set; }
        public string CompName { get; set; }
        public string Address { get; set; }
        public string Director { get; set; }
        public string Tel { get; set; }
        public string Email { get; set; }
        public string CrtUser { get; set; } // User thực hiện tạo (ví dụ: nhân viên kinh doanh)

        // --- NHÓM 2: THÔNG TIN BỔ SUNG (Có thể null) ---
        public string? Website { get; set; }
        public string? Fax { get; set; }
        public string? BankNumber { get; set; }
        public string? BankName { get; set; }
        public string? SaleID { get; set; }

        // --- NHÓM 3: THAM SỐ TỰ SINH / CẤU HÌNH (Sẽ ẩn khỏi Swagger/API) ---
        [JsonIgnore]
        public string CmpnKey { get; set; } = "";

        [JsonIgnore]
        public string Password { get; set; } = "";

        [JsonIgnore]
        public string BosGroupTemplate { get; set; } = "005.001.00221";

        [JsonIgnore]
        public int IsSite { get; set; } = 0;

        [JsonIgnore]
        public int IsGroup { get; set; } = 0;

        [JsonIgnore]
        public string ParentSite { get; set; } = "21";

        [JsonIgnore]
        public decimal QttyInv { get; set; } = 0;

        [JsonIgnore]
        public bool IsCheckQttyInv { get; set; } = false;
    }
}
