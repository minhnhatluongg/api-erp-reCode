using ERP_Portal_RC.Application.DTOs;

namespace ERP_Portal_RC.Domain.Entities
{
	public class RptInvPeriodPagedResponse
	{
		public List<RptInvPeriodDTO> Data { get; set; } = new();
		public int TotalCount { get; set; }
	}
}
