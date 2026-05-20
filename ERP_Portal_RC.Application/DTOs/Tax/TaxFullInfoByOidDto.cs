using ERP_Portal_RC.Domain.Entities.Tax;
using System.Collections.Generic;

namespace ERP_Portal_RC.Application.DTOs.Tax
{
    /// <summary>
    /// Response của GET /api/Tax/get-full-info-by-oid.
    /// Bổ sung danh sách mẫu hóa đơn TT78 và danh sách sản phẩm/dịch vụ trên hợp đồng.
    /// </summary>
    public class TaxFullInfoByOidDto
    {
        public string? CusTax { get; set; }
        public string? CusCMND_ID { get; set; }
        public string? OID { get; set; }
        public string? CusEmail { get; set; }
        public string? CusTel { get; set; }
        public string? SName { get; set; }
        public string? Address { get; set; }
        public TaxContractRange? ContractRange { get; set; }
        public IEnumerable<SampleTT78> Samples { get; set; } = new List<SampleTT78>();
        public IEnumerable<TaxProductRow> Products { get; set; } = new List<TaxProductRow>();
        public bool IsToKhai { get; set; }
    }
}
