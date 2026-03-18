using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class InsertJobRequest
{
    [Required] public string ReferenceID { get; set; }
    [Required] public string EntryID { get; set; }
    [Required] public string FactorID { get; set; }
    [Required] public string OperDept { get; set; }
    [Required] public string CusTax { get; set; }
    [Required] public string CusName { get; set; }
    [Required] public string ItemID { get; set; }
    [Required] public string InvcSign { get; set; }
    [Required] public int InvcFrm { get; set; }
    [Required] public int InvcEnd { get; set; }
    [Required] public DateTime ReferenceDate { get; set; }
    [Required] public string InvcSample { get; set; }

    public string? Descrip { get; set; }

    public string FileInvoice { get; set; } = "";
    public string FileOther { get; set; } = "";

    [JsonIgnore] public string CmpnID { get; set; } = "26";
    [JsonIgnore] public string? Crt_User { get; set; }
    [JsonIgnore] public string? EntryName { get; set; }
    [JsonIgnore] public string? ReferenceInfo { get; set; }
}