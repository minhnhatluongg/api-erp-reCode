using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class JobPackEntity
    {
        public string? OID { get; set; }
        public string? ItemID { get; set; }
        public DateTime ODate { get; set; }
        public string? EntryID { get; set; }
        public string? FactorID { get; set; }
        public string? ReferenceID { get; set; }
        public string? ReferenceDate { get; set; }
        public string? Descrip { get; set; }
        public string? FileLogo { get; set; }
        public string? FileInvoice { get; set; }
        public string? FileOther { get; set; }
        public DateTime exeDate { get; set; }
        public string? exeEmplID { get; set; }
        public string? fnlDate { get; set; }
        public string? fnlDescrip { get; set; }
        public string? InvcSign { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string? invcSample { get; set; }
        public int SignNumb { get; set; }
        public DateTime SignDate { get; set; }
        public string? Crt_User { get; set; }
        public string? Crt_Date { get; set; }
        public string? ChgeUser { get; set; }
        public string? FNAME { get; set; }
        public string? EntryName { get; set; }
        public string? EmplName { get; set; }
        public DateTime ChgeDate { get; set; }
        public string? FileInput { get; set; }
        public string? FileOutput { get; set; }
        public int ItemNo { get; set; }
        public DateTime? PublDate { get; set; }
        public DateTime? Use_Date { get; set; }
    }
}
