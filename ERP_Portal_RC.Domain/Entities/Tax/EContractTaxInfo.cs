using System;

namespace ERP_Portal_RC.Domain.Entities.Tax
{
    /// <summary>
    /// Raw row trả về từ SP:
    ///  - BosOnline..Get_Info_byMST_V25
    ///  - BosOnline..Get_Econtract_ByOID_V25
    /// </summary>
    public class EContractTaxInfo
    {
        public string? OID { get; set; }
        public string? CusTax { get; set; }
        public string? CusCMND_ID { get; set; }
        public string? CusEmail { get; set; }
        public string? CusTel { get; set; }
        public string? CusBankNumber { get; set; }
        public string? CusBankAddress { get; set; }
        public string? CusFax { get; set; }
        public string? CusWebsite { get; set; }
        public string? InvcSign { get; set; }
        public string? InvcSample { get; set; }
        public DateTime? ODate { get; set; }
        public string? Descript_Cus { get; set; }
        public string? CusPeople_Sign { get; set; }
        public bool IsToKhai { get; set; }
    }
}
