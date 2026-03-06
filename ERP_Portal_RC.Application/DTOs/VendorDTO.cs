using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class VendorDTO
    {
        public string? CmpnID { get; set; }
        public string? VName { get; set; }      
        public string? Director { get; set; }   
        public string? Address { get; set; }
        public string? TaxCode { get; set; }
        public string? Tel { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? BankInfo { get; set; }   
        public string? PositionName { get; set; }

        // Các cấu hình file
        [JsonIgnore]
        public string? LogoPath { get; set; }
        [JsonIgnore]
        public string? SignPath { get; set; }
    }
}
