namespace ERP_Portal_RC.Domain.Entities
{
    public class ApprovalWorkflowRequest
    {
        public string OID { get; set; } = string.Empty;
        public string? AppvMess { get; set; }
        public string? SampleID { get; set; }
    }
}
