namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Response tổng hợp cho API kỹ thuật — gồm cả file đính kèm lẫn file mẫu.
    /// </summary>
    public class ContractAllFilesResponse
    {
        public string Oid { get; set; } = "";

        /// <summary>File đính kèm user upload (Attachments folder).</summary>
        public List<ContractFileItem> Attachments { get; set; } = new();

        /// <summary>File mẫu kỹ thuật (KyThuatMau folder).</summary>
        public List<ContractFileItem> Templates { get; set; } = new();

        public int TotalAttachments => Attachments.Count;
        public int TotalTemplates   => Templates.Count;
        public int TotalAll         => TotalAttachments + TotalTemplates;
    }

    public class ContractFileItem
    {
        public string FileName     { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string Url          { get; set; } = "";
        public string Extension    { get; set; } = "";
        public long   SizeBytes    { get; set; }
        public DateTime UploadedAt { get; set; }
        /// <summary>attach / template</summary>
        public string Category     { get; set; } = "";
    }
}
