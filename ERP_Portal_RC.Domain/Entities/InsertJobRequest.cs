using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class InsertJobRequest
{
    [Required] public string ReferenceID { get; set; }



    /// <summary>Mã job xử lý</summary>
    /// <example>JB:001</example>
    [SwaggerSchema(Description =
        "Mã job xử lý:\n" +
        "- JB:001 → Tạo mẫu có sẵn      (JOB_00001)\n" +
        "- JB:002 → Tạo mẫu thiết kế    (JOB_00001)\n" +
        "- JB:003 → Kích hoạt tài khoản  (JOB_00003)\n" +
        "- JB:004 → Phát hành hóa đơn   (JOB_00002)\n" +
        "- JB:005 → Điều chỉnh mẫu      (JOB_00001)\n" +
        "- JB:006 → Đề xuất chỉnh sửa   (JOB_00003)\n" +
        "- JB:012 → Kiểm tra mẫu        (JOB_00006)")]
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