using ERP_Portal_RC.Application.DTOs.Tax;
using ERP_Portal_RC.Domain.Entities.Tax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ITaxService
    {
        /// <summary>
        /// Lấy thông tin hợp đồng + công ty + trạng thái tờ khai theo MST.
        /// </summary>
        Task<TaxFullInfoDto?> GetFullInfoByMstAsync(string mst, int loaiCap = 0);

        /// <summary>
        /// Lấy danh sách OID (hợp đồng) gắn với MST/CCCD.
        /// </summary>
        Task<IEnumerable<ContractSummaryRow>> GetOidListByMstAsync(string mst);

        /// <summary>
        /// Lấy thông tin chi tiết hợp đồng theo OID (kèm sản phẩm + mẫu TT78).
        /// </summary>
        Task<TaxFullInfoByOidDto?> GetFullInfoByOidAsync(string oid);
    }
}
