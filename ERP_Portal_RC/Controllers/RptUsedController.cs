using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Data;
using System.Security.Claims;

namespace API.ERP_Portal_RC.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class RptUsedController : Controller
	{
		private readonly IRptUsedService _rptUsedService;
		private readonly IConfiguration _configuration;
		public RptUsedController(IRptUsedService rptUsedService, IConfiguration configuration)
		{
			_rptUsedService = rptUsedService;
			_configuration = configuration;
		}
		/// <summary>
		/// Lấy danh sách công ty có chữ ký số sắp/đã hết hạn
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpGet("cert-expire")]
		public async Task<ActionResult<ApiResponse<object>>> CKSPeriod(
			[FromQuery] RptPageRequest request)
		{
			try
			{
				var responseData = await _rptUsedService.GetCKSPeriod(request);

				return Ok(ApiResponse<object>.SuccessResponse(responseData, "OK"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse.ErrorResponse($"Lỗi: {ex.Message}"));
			}
		}

		/// <summary>
		/// Lấy danh sách công ty có mẫu hóa đơn sắp/đã hết số lượng sử dụng
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpGet("low-remaining-inv")]
		public async Task<ActionResult<ApiResponse<object>>> INVPeriod(
			[FromQuery] RptPageRequest request)
		{
			try
			{
				var responseData = await _rptUsedService.GetINVPeriod(request);

				return Ok(ApiResponse<object>.SuccessResponse(responseData, "OK"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse.ErrorResponse($"Lỗi: {ex.Message}"));
			}
		}

		/// <summary>
		/// Lấy danh sách công ty sắp/đã hết hạn sử dụng TVAN
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpGet("tvan-expire")]
		public async Task<ActionResult<ApiResponse<object>>> TVANPeriod(
			[FromQuery] RptPageRequest request)
		{
			try
			{
				var responseData = await _rptUsedService.GetTVANPeriod(request);

				return Ok(ApiResponse<object>.SuccessResponse(responseData, "OK"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse.ErrorResponse($"Lỗi: {ex.Message}"));
			}
		}

	}
}

