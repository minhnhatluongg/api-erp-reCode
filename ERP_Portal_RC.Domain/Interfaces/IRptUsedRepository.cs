using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
	public interface IRptUsedRepository
	{
		Task<RptCKSPeriodPagedResponse> GetCKSPeriod(RptPageRequest request);
		Task<RptInvPeriodPagedResponse> GetInvPeriod(RptPageRequest request);
		Task<RptTVANPeriodPagedResponse> GetTVANPeriod(RptPageRequest request);
	}
}
