using Microsoft.AspNetCore.Http;

namespace ERP_Portal_RC.Domain.Entities
{
    public class JobEntity
    {
        public string? ReferenceID { get; set; }
        public string? ReferenceInfo { get; set; }
        public string? FactorID { get; set; }
        public string? EntryID { get; set; }
        public string? Descrip { get; set; }
        public string? SuplID { get; set; }
        public string? CmpnID { get; set; } = "26";
        public string? FileLogo { get; set; }
        public string? FileInvoice { get; set; }
        public string? FileOther { get; set; }
        public string? Crt_User { get; set; }
        public string? OID { get; set; }
        public string? PackID { get; set; }
        public string? InvcSign { get; set; }
        public int? InvcFrm { get; set; }
        public int? InvcEnd { get; set; }
        public string? invcSample { get; set; }
        public DateTime crt_date { get; set; }
        public DateTime SignDate { get; set; }
        public string? cmpnID { get; set; }
        public string? FactorIDAtt { get; set; }
        public string? FileName { get; set; }
        public string? AttachType { get; set; }
        public string? ConvertFile { get; set; }
        public string? AttachNote { get; set; }
        public string? FileUrl { get; set; }
        public string? SignNumb { get; set; }
        public string? FileName0 { get; set; }
        public string? FileName1 { get; set; }
        public string? FileName2 { get; set; }
        public string? FileName3 { get; set; }
        public string? FileName4 { get; set; }
        public string? FileName5 { get; set; }
        public string? FileName6 { get; set; }
        public string? FileName7 { get; set; }
        public string? FileName8 { get; set; }
        public string? FileName9 { get; set; }
        public string? EmplID { get; set; }
        public IFormCollection? UploadedFiles { get; set; }
        public int ItemNo { get; set; }
        public DateTime? PublDate { get; set; }
        public DateTime? Use_Date { get; set; }
        public int? CountChange { get; set; }
        public string? Reason { get; set; }
        public string? exeEmplID { get; set; }
        public string? exeEmplName { get; set; }
        public string? exeDescrip { get; set; }
        public string? DescriptChange { get; set; }
        public string? ChangeOption { get; set; }
        public string? TemplateID { get; set; }
        public string? MailAcc { get; set; }
        public bool IsSave { get; set; }
        public string? OperDept { get; set; }
        public bool isAuto_InvcNumb { get; set; } = false;
        public string? sName { get; set; }
        public string? cusTax { get; set; }
        public string? cusName { get; set; }
        public bool isSubmit { get; set; } = false;
        public string? statusinvoice { get; set; }
        public string? devicesAddress { get; set; }
        public int curSign { get; set; } //signNumr Zgn_EcontractJob
        public bool isDesignInvoices { get; set; }
        public int currSignNumb { get; set; }
        public string? EntryName { get; set; }
        public string? EmplName { get; set; }
    }
}