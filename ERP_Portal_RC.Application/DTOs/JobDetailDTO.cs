using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class JobDetailDTO
    {
        public string? OID { get; set; }
        public string? CmpnName { get; set; }
        public string? CusName { get; set; }
        public string? CusTax { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public string? EntryName { get; set; }
        public string? EmplName { get; set; }
        public string? CusAddress { get; set; }
        public string? DescriptChange { get; set; }
        public string? CusEmail { get; set; }
        public int CurrSignNumb { get; set; }
        [JsonIgnore]
        public bool IsTT78 { get; set; }
        [JsonIgnore]
        public bool IsCheckXHD { get; set; }
        [JsonIgnore]
        public bool IsShowCheckXHD { get; set; }
        public string? PositionName { get; set; }
        public string? BankInfo { get; set; }
        [JsonIgnore]
        public bool IsshowYCCS { get; set; }
        public DateTime? Crt_Date { get; set; }
        public int? SignNumb { get; set; }
        public string? InvcSign { get; set; }
        public int? InvcFrm { get; set; }
        public int? InvcEnd{ get; set; }
        public string? InvcSample { get; set; }
        public string? Descrip  { get; set; }
        public string? ReferenceInfo { get; set; }
        public string? FileLogo { get; set; }
        public string? FileInvoice{ get; set; }

        public bool IsSave { get; set; }
        public bool IsDesignInvoices { get; set; }

    }
}
