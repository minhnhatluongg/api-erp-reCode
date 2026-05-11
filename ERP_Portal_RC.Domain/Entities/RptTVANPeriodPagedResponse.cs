namespace ERP_Portal_RC.Domain.Entities
{
	public class RptTVANPeriodPagedResponse
	{
		public List<RptTVANPeriodDTO> Data { get; set; } = new();
		public int TotalCount { get; set; }
	}
}
