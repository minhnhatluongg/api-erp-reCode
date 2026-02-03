namespace ERP_Portal_RC.Domain.Entities
{
    public class EContract_Monitor
    {
        public string? OID { get; set; }
        public DateTime ODATE { get; set; }
        public string? CustomerID { get; set; }
        public string? CusName { get; set; }
        public string? CusTax { get; set; }
        public string? DESCRIP { get; set; }
        public string? SaleEmID { get; set; }
        public int CurrSignNumb { get; set; }
        public int currSignNumbJobKT { get; set; }
        public bool isKTTT_1 { get; set; }
        public bool is_NopHSS { get; set; }
        public DateTime date_NopHSS { get; set; }
        public DateTime date_HSSComplete { get; set; }
        public bool isKTTT_2 { get; set; }
        public bool isCheckXHD { get; set; }
        public bool isCheck { get; set; }
        public bool isBPGN { get; set; }
        public byte IsINS { get; set; }
        public DateTime Crt_Date { get; set; }
        public string? StepName { get; set; }
        public string? TT1 { get; set; }
        public string? TT2 { get; set; }
        public string? TT3 { get; set; }
        public string? TT4 { get; set; }
        public string? TT5 { get; set; }
        public string? TT6 { get; set; }
        public string? TT8 { get; set; }
        public string? EmplName { get; set; }
        public string? CmpnName { get; set; }
        public string? CmpnID { get; set; }
        public string? CmpnNameComp { get; set; }
        public string? SiteName { get; set; }
        public string? Descript_Cus { get; set; }
        public string? urlDownload { get; set; }
        public string? CusAddress { get; set; }
        public string? ItemName { get; set; }
        public string? InvcSign { get; set; }
        public string? invcSample { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public decimal ItemPerBox { get; set; }
        public decimal ItemPrice { get; set; }
        public bool ischeckTK { get; set; }
        public bool ischeckPH { get; set; }
        public bool ischeckKNV { get; set; }
        public bool IsDisiable { get; set; }
        public bool isChoose { get; set; }
        public bool isPLHD { get; set; }
        public string Crt_User { get; set; }
        public bool isContractPaper { get; set; }
        public string? IsServiceOther { get; set; }
        public string? CsBrifName { get; set; }
        public string? RegisTypeID { get; set; }
        public string? optioncomplete { get; set; }
        public bool isComplete { get; set; }
        public bool isDesignInvoice { get; set; }
        public bool ispay { get; set; } = true;
        public bool isCheckedShow { get; set; } = false;
        public string? XHD { get; set; }
        public bool isTT78 { get; set; } = false;
        public bool isTool { get; set; } = false;
        public bool isGiaHan { get; set; } = false;
    }
}