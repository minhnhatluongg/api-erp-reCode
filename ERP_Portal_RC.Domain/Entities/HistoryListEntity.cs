namespace ERP_Portal_RC.Domain.Entities
{
    public class HistoryListEntity
    {
        public string? OID { get; set; }
        public DateTime currSignDate { get; set; }
        public string? currSignNum { get; set; }
        public string? FullName { get; set; }
        public string? appvMess { get; set; }
        public string? cancelDescript { get; set; }
    }
}