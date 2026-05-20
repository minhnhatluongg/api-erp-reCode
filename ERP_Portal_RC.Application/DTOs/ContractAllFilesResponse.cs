namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Response tổng hợp file của 1 user — group theo folder (contract).
    /// </summary>
    public class ContractAllFilesResponse
    {
        public string Oid { get; set; } = "";

        /// <summary>File đính kèm — group theo folder HĐ.</summary>
        public List<ContractFolderGroup> Attachments { get; set; } = new();

        /// <summary>File mẫu kỹ thuật — group theo folder HĐ.</summary>
        public List<ContractFolderGroup> Templates   { get; set; } = new();

        public int TotalAttachments => Attachments.Sum(g => g.Files.Count);
        public int TotalTemplates   => Templates.Sum(g => g.Files.Count);
        public int TotalAll         => TotalAttachments + TotalTemplates;
    }

    /// <summary>1 folder (= 1 hợp đồng) chứa danh sách file.</summary>
    public class ContractFolderGroup
    {
        /// <summary>Tên folder trên disk (VD: 000642_260511_153203963).</summary>
        public string Folder      { get; set; } = "";

        /// <summary>File list bên trong folder.</summary>
        public List<ContractFileItem> Files { get; set; } = new();

        public int TotalFiles => Files.Count;
    }

    public class ContractFileItem
    {
        public string   FileName     { get; set; } = "";
        public string   OriginalName { get; set; } = "";
        public string   Url          { get; set; } = "";
        public string   Extension    { get; set; } = "";
        public long     SizeBytes    { get; set; }
        public DateTime UploadedAt   { get; set; }
        /// <summary>attach | template-view | template-download</summary>
        public string   Category     { get; set; } = "";
    }
}
