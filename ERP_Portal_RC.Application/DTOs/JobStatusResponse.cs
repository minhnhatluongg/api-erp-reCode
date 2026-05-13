using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Response phân loại trạng thái Job của hợp đồng.
    /// JobStatusItem nằm ở Domain.Entities (Dapper map trực tiếp từ SP).
    /// </summary>
    public class ContractJobStatusResponse
    {
        /// <summary>Job đã hoàn thành (SignNumb = 201 hoặc 301).</summary>
        public List<JobStatusItem> JobDone     { get; set; } = new();

        /// <summary>Job đang chờ duyệt (SignNumb = 101).</summary>
        public List<JobStatusItem> JobWaiting  { get; set; } = new();

        /// <summary>Job bị trả về (SignNumb = 100).</summary>
        public List<JobStatusItem> JobReturned { get; set; } = new();

        /// <summary>Job chưa bắt đầu (chưa có dòng duyệt nào).</summary>
        public List<JobStatusItem> JobPending  { get; set; } = new();
    }
}
