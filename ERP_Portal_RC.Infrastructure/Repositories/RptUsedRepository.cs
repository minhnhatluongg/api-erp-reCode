using System.Data;
using Dapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
	public class RptUsedRepository : IRptUsedRepository
	{
		private readonly IDbConnectionFactory _dbConnectionFactory;

		private readonly IConfiguration _configuration;
		private const string BosEVATDB = "BosEVAT";

		public RptUsedRepository(IDbConnectionFactory dbConnectionFactory, IConfiguration configuration)
		{
			_dbConnectionFactory = dbConnectionFactory;
			_configuration = configuration;
		}
		public async Task<RptCKSPeriodPagedResponse> GetCKSPeriod(RptPageRequest request)
		{
			using var conn = _dbConnectionFactory.GetConnection(BosEVATDB);
			var parameters = new DynamicParameters();
			parameters.Add("@PageNumber", request.Page);
			parameters.Add("@PageSize", request.PageSize);
			parameters.Add("@SearchText", request.SearchKeyword);

			var model = new RptCKSPeriodPagedResponse();
			using (var result = await conn.QueryMultipleAsync("GetRPT_CKSPeriod_Paged", parameters, commandType: CommandType.StoredProcedure))
			{
				model.Data = (await result.ReadAsync<RptCKSPeriodDTO>()).ToList();
				model.TotalCount = await result.ReadSingleAsync<int>();
			}
			return model;
		}

		public async Task<RptInvPeriodPagedResponse> GetInvPeriod(RptPageRequest request)
		{
			using var conn = _dbConnectionFactory.GetConnection(BosEVATDB);
			var parameters = new DynamicParameters();
			parameters.Add("@PageNumber", request.Page);
			parameters.Add("@PageSize", request.PageSize);
			parameters.Add("@SearchText", request.SearchKeyword);

			var model = new RptInvPeriodPagedResponse();
			using (var result = await conn.QueryMultipleAsync("GetRPT_InvUsedStatus_Paged", parameters, commandType: CommandType.StoredProcedure))
			{
				model.Data = (await result.ReadAsync<RptInvPeriodDTO>()).ToList();
				model.TotalCount = await result.ReadSingleAsync<int>();
			}
			return model;
		}

		public async Task<RptTVANPeriodPagedResponse> GetTVANPeriod(RptPageRequest request)
		{
			using var conn = _dbConnectionFactory.GetConnection(BosEVATDB);
			var parameters = new DynamicParameters();
			parameters.Add("@PageNumber", request.Page);
			parameters.Add("@PageSize", request.PageSize);
			parameters.Add("@SearchText", request.SearchKeyword);

			var model = new RptTVANPeriodPagedResponse();
			using (var result = await conn.QueryMultipleAsync("GetRPT_TVANPeriod_Paged", parameters, commandType: CommandType.StoredProcedure))
			{
				model.Data = (await result.ReadAsync<RptTVANPeriodDTO>()).ToList();
				model.TotalCount = await result.ReadSingleAsync<int>();
			}
			return model;
		}
	}
}
