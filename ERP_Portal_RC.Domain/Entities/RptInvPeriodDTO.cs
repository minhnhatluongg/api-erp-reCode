using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.DTOs
{
	public class RptInvPeriodDTO: CompanyContactDTO
	{
		public string? SampleSign { get; set; }		
		public string? InvcSign { get; set; }		
		public int? InvcTotal { get; set; }		 
		public int? InvcUsed { get; set; }		 
		public int? InvcRemain { get; set; }		 
	
	}
}
