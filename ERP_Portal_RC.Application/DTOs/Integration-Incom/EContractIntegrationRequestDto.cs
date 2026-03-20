using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.Integration_Incom
{
    public class EContractIntegrationRequestDto
    {
        // --- NHÓM 1: THÔNG TIN KHÁCH HÀNG (BÊN A) ---
        [Required]
        public string CusTax { get; set; }           // Mã số thuế khách hàng
        [Required]
        public string OrderOID { get; set; }           //Mã số hợp đồng - Tạo thành UserCode / zzzz
        [Required]
        public string CusName { get; set; }          // Tên công ty khách hàng
        public string CusAddress { get; set; }       // Địa chỉ trên GPKD
        public string? CusEmail { get; set; }        // Email nhận thông báo/hóa đơn
        public string? CusTel { get; set; }          // Số điện thoại liên hệ
        public string? CusPeople_Sign { get; set; }  // Người đại diện pháp luật (Tên Giám đốc)
        public string? CusPosition_BySign { get; set; } // Chức vụ người ký - ( Giám đốc )
        public string? CusBankNumber { get; set; }   // Số tài khoản ngân hàng khách
        public string? CusBankAddress { get; set; }  // Tên ngân hàng khách
        public string? CusWebsite { get; set; }

        // --- NHÓM 2: THÔNG TIN CÔNG TY MÌNH (BÊN B) ---
        [JsonIgnore]
        public string? MyCmpnID { get; set; } = "26"; // Mặc định ID công ty tổng
        [JsonIgnore]
        public string? MyCmpnName { get; set; }
        [JsonIgnore]
        public string? MyCmpnTax { get; set; }
        [JsonIgnore]
        public string? MyCmpnAddress { get; set; }
        [JsonIgnore]
        public string? MyCmpnMail { get; set; }
        [JsonIgnore]
        public string? MyCmpnTel { get; set; }
        [JsonIgnore]
        public string? MyCmpnContactAddress { get; set; }
        [JsonIgnore]
        public string? MyCmpnPeople_Sign { get; set; }
        [JsonIgnore]
        public string? MyCmpnPosition_Sign { get; set; }
        public string? MyCmpnBankNumber { get; set; }
        public string? MyCmpnBankAddress { get; set; }
        // --- NHÓM 3: THÔNG TIN ĐƠN HÀNG (MASTER) ---
        [JsonIgnore]
        public string FactorID { get; set; } = "EContract";
        [JsonIgnore]
        public string EntryID { get; set; } = "EC:001";
        [JsonIgnore]
        public string? SaleEmID { get; set; }         // Mã nhân viên kinh doanh

        public string? SampleID { get; set; }         // Mã gói/Mẫu hóa đơn khách chọn
        public string? Descrip { get; set; }         // Ghi chú đơn hàng

        public string? OidContract { get; set; }
        public string HtmlContent { get; set; } = "INCOM-MINI-APP";
        public DateTime? ODate { get; set; } = DateTime.Now;
        public DateTime? SignDate { get; set; } = DateTime.Now;
        public DateTime? ReferenceDate { get; set; } = DateTime.Now;
        public DateTime? RefeContractDate { get; set; }

        //Flag Trạng thái 
        public bool IsCapBu { get; set; }
        public bool IsGiaHan { get; set; }
        public bool IsTT78 { get; set; } = true;
        public bool IsOnline { get; set; } = true;
        [JsonIgnore]
        public decimal PrdcAmnt { get; set; }

        [JsonIgnore]
        public decimal VAT_Rate { get; set; }

        [JsonIgnore]
        public decimal VAT_Amnt { get; set; }

        [JsonIgnore]
        public decimal Sum_Amnt { get; set; }

        // --- NHÓM 4: CHI TIẾT GÓI DỊCH VỤ ---
        public List<EContractDetailDTO> Details { get; set; } = new List<EContractDetailDTO>();
    }
}
