using System.Collections.Generic;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Request cho POST /api/integration/ghost-contract — "Hợp đồng Ma".
    ///
    /// Mục đích: tạo hợp đồng bình thường (có SaleEmID thật) nhưng phần lên gói server
    /// TỰ chèn 2 gói miễn phí (giá 0): gói hóa đơn (Cấp bù) + gói truyền nhận (Tvan),
    /// để hợp thức hóa dải hóa đơn qua hệ thống hóa đơn.
    ///
    /// Luồng gọi (n8n / hệ thống hóa đơn) CHỈ cần gửi: mẫu số, ký hiệu, từ số → đến số.
    /// Mọi thứ còn lại (Bên B, Sale LOT, 2 gói free, Bên A) lấy từ appsettings → GhostContract.
    /// </summary>
    public class GhostContractRequest
    {
        // ── Thông tin hóa đơn — BẮT BUỘC (cái caller cần gửi) ──────────────

        /// <summary>Mẫu số hóa đơn. OPTIONAL — trống → server default "1".</summary>
        public string? InvSample { get; set; }

        /// <summary>Ký hiệu hóa đơn (ví dụ "C26LMN") — BẮT BUỘC.</summary>
        public string? InvSign { get; set; }

        /// <summary>Số bắt đầu — BẮT BUỘC.</summary>
        public int? InvFrom { get; set; }

        /// <summary>Số kết thúc — BẮT BUỘC. Phải ≥ InvFrom.</summary>
        public int? InvTo { get; set; }

        // ── Override (đều OPTIONAL — trống thì lấy từ config GhostContract) ─

        /// <summary>Mã NV Sale LOT. Trống → GhostContract:SaleEmID → UserCode token.</summary>
        public string? SaleEmID { get; set; }

        /// <summary>ItemID gói hóa đơn free. Trống → GhostContract:InvoiceItemID.</summary>
        public string? InvoiceItemID { get; set; }

        /// <summary>ItemID gói truyền nhận free. Trống → GhostContract:TransmissionItemID.</summary>
        public string? TransmissionItemID { get; set; }

        /// <summary>Bên A (khách hàng). Trống → GhostContract:DefaultCustomer.</summary>
        public GhostContractCustomer? Customer { get; set; }

        // ── Tracking ───────────────────────────────────────────────────────

        public string? Source { get; set; }
        public string? Campaign { get; set; }
        public string? Descrip { get; set; }
    }

    /// <summary>
    /// Thông tin Bên A (khách hàng) cho hợp đồng ma. Dùng cho cả request override
    /// lẫn cấu hình GhostContract:DefaultCustomer trong appsettings.
    /// </summary>
    public class GhostContractCustomer
    {
        public string? CusTax { get; set; }
        public string? CusName { get; set; }
        public string? CusAddress { get; set; }
        public string? CusTel { get; set; }
        public string? CusEmail { get; set; }
        public string? CusBankNumber { get; set; }
        public string? CusBankAddress { get; set; }
        public string? CusWebsite { get; set; }
        public string? CusPeople_Sign { get; set; }
        public string? CusPosition_BySign { get; set; }
    }

    /// <summary>
    /// Cấu hình hợp đồng ma — bind từ appsettings section "GhostContract".
    /// </summary>
    public class GhostContractOptions
    {
        public const string SectionName = "GhostContract";

        /// <summary>Mã NV Sale LOT cố định dùng chung.</summary>
        public string? SaleEmID { get; set; }

        /// <summary>CmpnID Bên B (mặc định "26" — WinTech).</summary>
        public string CmpnID { get; set; } = "26";

        /// <summary>ItemID gói hóa đơn free (mặc định "0036473" — Cấp bù).</summary>
        public string InvoiceItemID { get; set; } = "0036473";

        /// <summary>ItemID gói truyền nhận free (mặc định "2100398" — Tvan truyền nhận).</summary>
        public string TransmissionItemID { get; set; } = "2100398";

        /// <summary>Bên A mặc định khi request không truyền customer.</summary>
        public GhostContractCustomer? DefaultCustomer { get; set; }
    }

    /// <summary>
    /// Response cho ghost-contract — OID hợp đồng + 2 gói đã resolve.
    /// </summary>
    public class GhostContractResponse
    {
        public string OID { get; set; } = string.Empty;
        public string SaleEmID { get; set; } = string.Empty;
        public List<ResolvedItem> Items { get; set; } = new();
        public bool AccountCreated { get; set; }
        public bool AlreadyHadAccount { get; set; }
        public string? JobOid { get; set; }
    }
}
