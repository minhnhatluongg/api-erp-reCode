using ERP_Portal_RC.Domain.Entities.Tax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    /// <summary>
    /// Truy vấn nguyên thủy phục vụ TaxService.
    /// Mỗi method bám sát một SP, KHÔNG compose dữ liệu cross-DB — phần đó để TaxService làm.
    /// </summary>
    public interface ITaxRepository
    {
        // ── BosOnline ────────────────────────────────────────────────────────
        Task<EContractTaxInfo?> GetEContractInfoByMstAsync(string mst, int loaiCap);
        Task<EContractTaxInfo?> GetEContractInfoByOidAsync(string oid);
        Task<IEnumerable<ContractSummaryRow>> GetOidListByMstAsync(string mst);
        Task<TaxContractRange?> GetContractRangeAsync(string cusTax, string invSign, string invSample);
        Task<IEnumerable<TaxProductRow>> GetEContractDetailByOidAsync(string oid);

        // ── BosEVAT (server theo MST) ────────────────────────────────────────
        Task<TaxCmpnInfo?> GetEvatCmpnInfoAsync(string evatConnStr, string taxcode, string cccd);
        Task<IEnumerable<SampleTT78>> GetSampleTT78Async(string evatConnStr, string taxcode);

        // ── BosTVAN (server theo MST) ────────────────────────────────────────
        Task<bool> CheckConfirmTokhaiAsync(string tvanConnStr, string mst, string cccd);
    }
}
