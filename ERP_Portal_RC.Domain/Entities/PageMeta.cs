namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>Pagination metadata trả về từ SP (resultset 3).</summary>
    public class PageMeta
    {
        public int Page       { get; set; }
        public int PageSize   { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
