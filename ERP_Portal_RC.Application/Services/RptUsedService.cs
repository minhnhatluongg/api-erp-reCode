using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Application.Services
{
	public class RptUsedService : IRptUsedService
	{
		private readonly IRptUsedRepository _connectionRepo;
		private readonly IConfiguration _configuration;

		public RptUsedService(
			IRptUsedRepository connectionRepo,
			IConfiguration configuration)
		{
			_connectionRepo = connectionRepo;
			_configuration = configuration;
		}

		public Task<RptCKSPeriodPagedResponse> GetCKSPeriod(RptPageRequest request)
		{
			return _connectionRepo.GetCKSPeriod(request);
		}

		public Task<RptInvPeriodPagedResponse> GetINVPeriod(RptPageRequest request)
		{
			return _connectionRepo.GetInvPeriod(request);
		}

		public Task<RptTVANPeriodPagedResponse> GetTVANPeriod(RptPageRequest request)
		{
			return _connectionRepo.GetTVANPeriod(request);
		}
	}

}


