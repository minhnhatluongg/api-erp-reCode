using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Interfaces
{
	public interface IRptUsedService
	{
		Task<RptCKSPeriodPagedResponse> GetCKSPeriod(RptPageRequest request);
		Task<RptInvPeriodPagedResponse> GetINVPeriod(RptPageRequest request);
		Task<RptTVANPeriodPagedResponse> GetTVANPeriod(RptPageRequest request);

	}
}
