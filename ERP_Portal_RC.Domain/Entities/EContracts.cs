using System;

namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Entity hợp đồng điện tử dùng cho luồng phát hành mẫu / cấp tài khoản.
    /// Map từ bảng BosOnline..EContracts.
    /// </summary>
    public class EContracts
    {
        public string? OID { get; set; }
        public string? CmpnID { get; set; }
        public string? CmpnName { get; set; }
        public string? CmpnAddress { get; set; }
        public string? CmpnTax { get; set; }
        public string? CmpnTel { get; set; }
        public string? CmpnMail { get; set; }
        public string? CmpnPeople_Sign { get; set; }
        public string? CmpnPosition_BySign { get; set; }
        public string? CmpnBankNumber { get; set; }
        public string? CmpnBankAddress { get; set; }

        public string? CusName { get; set; }
        public string? CusAddress { get; set; }
        public string? CusTax { get; set; }
        public string? CusCMND_ID { get; set; }
        public string? CusTel { get; set; }
        public string? CusEmail { get; set; }
        public string? CusPeople_Sign { get; set; }
        public string? CusPosition_BySign { get; set; }
        public string? CusBankNumber { get; set; }
        public string? CusBankAddress { get; set; }
        public string? CusFax { get; set; }
        public string? CusWebsite { get; set; }
        public string? Descrip { get; set; }

        public string? SampleID { get; set; }
        public string? SerialID { get; set; }

        public DateTime? Crt_Date { get; set; }
        public string? Crt_User { get; set; }
    }
}
