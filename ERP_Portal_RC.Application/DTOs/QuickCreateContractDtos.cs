using System;
using System.Collections.Generic;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Request lightweight cho POST /api/integration/quick-create.
    /// Dùng cho luồng tích hợp ngoài: Landingpage / Zalo Mini App / Email / Smax.ai Chatbot → n8n.
    ///
    /// SO VỚI /api/Econtract/save-and-approve (ContractPreviewRequest):
    ///   - Bỏ toàn bộ thông tin Bên B (CmpnID/CmpnName/CmpnTax/CmpnBank...) — server tự fill
    ///     từ IEContractRepository.GetOwnerContractAsync (mặc định CmpnID="26").
    ///   - Bỏ List&lt;EContractDetailItem&gt;. Thay bằng 1 object 'Product' giống schema response
    ///     của /api/odoo/orders/get-products → FE chỉ cần copy nguyên item vừa chọn vào đây
    ///     và thêm Qtty.
    ///   - Hiện chỉ hỗ trợ GÓI HÓA ĐƠN ĐIỆN TỬ (HĐĐT). Gói Chữ ký số (CKS) chưa hỗ trợ.
    ///   - Gộp đơn mới / gia hạn vào 1 endpoint qua field IsGiaHan + OIDContract.
    /// </summary>
    public class QuickCreateContractRequest
    {
        // ─────────────────────────────────────────────────────────────────
        // 1. Loại đơn
        // ─────────────────────────────────────────────────────────────────

        /// <summary>false = Tạo mới (default). true = Gia hạn.</summary>
        public bool IsGiaHan { get; set; } = false;

        /// <summary>OID hợp đồng cũ tham chiếu — BẮT BUỘC khi IsGiaHan = true.</summary>
        public string? OIDContract { get; set; }

        /// <summary>Ngày hợp đồng cũ — khuyến nghị khi gia hạn. Trống → DateTime.Now.</summary>
        public DateTime? RefeContractDate { get; set; }

        // ─────────────────────────────────────────────────────────────────
        // 2. Thông tin khách hàng (Bên A)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>MST khách hàng — BẮT BUỘC.</summary>
        public string CusTax { get; set; } = string.Empty;

        /// <summary>Tên công ty khách hàng — BẮT BUỘC.</summary>
        public string CusName { get; set; } = string.Empty;

        public string? CusAddress { get; set; }
        public string? CusTel { get; set; }
        public string? CusEmail { get; set; }
        public string? CusBankNumber { get; set; }
        public string? CusBankAddress { get; set; }
        public string? CusWebsite { get; set; }
        public string? CusPeople_Sign { get; set; }
        public string? CusPosition_BySign { get; set; }

        // ─────────────────────────────────────────────────────────────────
        // 3. Gói hóa đơn điện tử
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gói cước HĐĐT — BẮT BUỘC.
        /// Cấu trúc giống response item của /api/odoo/orders/get-products.
        /// FE copy nguyên 1 item từ get-products vào đây, có thể bổ sung Qtty (default 1).
        /// </summary>
        public QuickCreateProduct Product { get; set; } = new();

        // ─────────────────────────────────────────────────────────────────
        // 4. Thông tin hóa đơn
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mẫu số hóa đơn. OPTIONAL — server default "1".
        /// Ghép với InvSign tạo thành dạng "1C26LMN" trong các báo cáo.
        /// </summary>
        public string? InvSample { get; set; }

        /// <summary>Ký hiệu hóa đơn (ví dụ "C26LMN") — BẮT BUỘC.</summary>
        public string? InvSign { get; set; }

        /// <summary>Số bắt đầu — BẮT BUỘC.</summary>
        public int? InvFrom { get; set; }

        /// <summary>Số kết thúc — BẮT BUỘC. Phải ≥ InvFrom.</summary>
        public int? InvTo { get; set; }

        // ─────────────────────────────────────────────────────────────────
        // 5. Sale & nguồn dữ liệu
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mã NV sale phụ trách — BẮT BUỘC với đơn mới.
        /// Với gia hạn: nếu trống, server lấy UserCode từ token.
        /// </summary>
        public string? SaleEmID { get; set; }

        /// <summary>
        /// Nguồn dữ liệu (tracking n8n): "LANDING_PAGE" | "ZALO_MINI_APP" | "EMAIL_MK" |
        /// "SMAX_CHATBOT" | "MANUAL"...
        /// </summary>
        public string? Source { get; set; }

        public string? Campaign { get; set; }
        public string? CustomerExternalID { get; set; }

        /// <summary>Diễn giải tự do. Trống → server auto-build từ Source + Campaign.</summary>
        public string? Descrip { get; set; }
    }

    /// <summary>
    /// Schema gói cước — KHỚP 100% với response item của /api/odoo/orders/get-products.
    /// Bổ sung field Qtty (số lượng đặt mua, không có trong get-products).
    /// </summary>
    public class QuickCreateProduct
    {
        // ── Các field nhận từ get-products (FE copy nguyên vào) ──
        public string ItemID { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public string? ItemUnit { get; set; }
        public string? ItemUnitName { get; set; }
        public int ItemPerBox { get; set; }
        public decimal ItemPrice { get; set; }
        public string? VAT_Rate { get; set; }
        public string? VAT_Name { get; set; }
        public string? ItemType { get; set; }
        public int IsRepaire { get; set; }

        // ── Field bổ sung ──
        /// <summary>Số lượng đặt mua. Trống → server dùng ItemPerBox (thường 1).</summary>
        public decimal? Qtty { get; set; }
    }

    /// <summary>
    /// Response chi tiết cho quick-create — bọc thêm thông tin nguồn & gói đã resolve.
    /// </summary>
    public class QuickCreateContractResponse
    {
        public string OID { get; set; } = string.Empty;
        public bool IsGiaHan { get; set; }
        public string? OIDContract { get; set; }
        public string? Source { get; set; }
        public ResolvedItem? ResolvedProduct { get; set; }
        public bool AccountCreated { get; set; }
        public bool AlreadyHadAccount { get; set; }
        public string? JobOid { get; set; }
    }

    public class ResolvedItem
    {
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Qtty { get; set; }
        public decimal Price { get; set; }
        public string VAT_Rate { get; set; } = "8";
        public string InvSample { get; set; } = string.Empty;
        public string InvSign { get; set; } = string.Empty;
        public int InvFrom { get; set; }
        public int InvTo { get; set; }
    }
}
