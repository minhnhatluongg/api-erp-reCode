namespace ERP_Portal_RC.Application.Interfaces.Admin
{
    public record LogFileInfo(
        string FileName,
        string FilePath,
        string Category,
        long   SizeBytes,
        DateTime LastModified);

    public record LogReadResult(
        string   FileName,
        string   Category,
        DateTime LastModified,
        long     TotalLines,
        int      Page,
        int      PageSize,
        int      TotalPages,
        IEnumerable<string> Lines);

    public interface IAdminLogService
    {
        /// <summary>Liệt kê tất cả file log theo category (econtract / externalapi / stdout).</summary>
        IEnumerable<LogFileInfo> ListFiles(string? category = null, string? date = null);

        /// <summary>Đọc nội dung 1 file log — có phân trang theo dòng.</summary>
        Task<LogReadResult?> ReadFileAsync(string category, string fileName, int page, int pageSize);

        /// <summary>Tìm kiếm từ khoá trong 1 file log.</summary>
        Task<IEnumerable<string>> SearchAsync(string category, string fileName, string keyword, int maxLines);
    }
}
