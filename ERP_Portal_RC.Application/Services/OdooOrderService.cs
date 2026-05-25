using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ERP_Portal_RC.Application.Services
{
    /// <summary>
    /// Port từ TVAN_WEB_API/ERP/Controllers/OdooOrdersController.cs.
    /// Service ủy quyền cho IEContractRepository — giữ NGUYÊN logic gọi SP:
    ///   - get-products → BosOnline..wspProducts_Tool_v25  (GetProductsCatalogAsync)
    ///   - owner-info   → BosOnline..Check_OwnerContract   (GetOwnerContractAsync)
    /// </summary>
    public class OdooOrderService : IOdooOrderService
    {
        private readonly IEContractRepository _econtractRepository;
        private readonly ILogger<OdooOrderService> _logger;

        public OdooOrderService(
            IEContractRepository econtractRepository,
            ILogger<OdooOrderService> logger)
        {
            _econtractRepository = econtractRepository;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm/gói dịch vụ cho dropdown trên màn hình tạo / gia hạn đơn.
        /// Override VAT mặc định "8%" và ItemUnitName fallback "Gói" — giữ nguyên TVAN.
        /// </summary>
        public async Task<List<ProductResponse>> GetProductsAsync(GetProductsRequest request)
        {
            request ??= new GetProductsRequest();

            var query = new ProductCatalogQuery
            {
                ClnID = request.ClnID,
                ZoneID = request.ZoneID,
                RegionID = request.RegionID,
                ASM = request.ASM,
                SUB = request.SUB,
                TEAM = request.TEAM,
                CustomerID = request.CustomerID,
                MembType = request.MembType,
                onlyTVAN = request.onlyTVAN
            };

            var rows = await _econtractRepository.GetProductsCatalogAsync(query);

            var result = rows.Select(x => new ProductResponse
            {
                ItemID = x.ItemID,
                ItemName = x.ItemName,
                ItemUnit = x.ItemUnit,
                ItemUnitName = string.IsNullOrWhiteSpace(x.ItemUnitName) ? "Gói" : x.ItemUnitName,
                ItemPerBox = x.ItemPerBox,
                ItemPrice = x.ItemPrice,
                VAT_Rate = "8",
                VAT_Name = "VAT 8%",
                ItemType = "Gói",
                IsRepaire = x.IsRepaire
            }).ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin công ty chủ quản (Bên B). Mặc định CmpnID = "26" — WinTech Solution.
        /// </summary>
        public async Task<OwnerContract?> GetOwnerInfoAsync(string companyId = "26")
        {
            if (string.IsNullOrWhiteSpace(companyId)) companyId = "26";

            try
            {
                return await _econtractRepository.GetOwnerContractAsync(companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OdooOrderService.GetOwnerInfoAsync FAIL CmpnID={CmpnID}", companyId);
                throw;
            }
        }
    }
}
