using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using ERP_Portal_RC.Application.DTOs;

namespace ERP_Portal_RC.Application.Services
{
    /// <summary>
    /// Đọc (<see cref="Detect"/>) và ghi (<see cref="InjectVisibilityHooks"/>) cấu hình ẩn/hiện thông tin
    /// người bán + viền TRỰC TIẾP trên thân XSLT — KHÔNG hard-code theo một mẫu cụ thể.
    ///
    /// Mục tiêu: file mẫu "self-contained" → preview (/view), xác nhận (/confirm-sample) và phát hành
    /// (/quick-publish) dùng đúng một XSLT, nên hệ thống hóa đơn bên kia hiển thị GIỐNG HỆT bản xem trước.
    ///
    /// Cách nhận diện 1 trường (độ tin cậy giảm dần, áp dụng cho MỌI mẫu):
    ///   1) Đã có sẵn id chuẩn "_NBxxx" trên thẻ (chuẩn WinInvoice hiện hành).
    ///   2) Có XPath dữ liệu TT78 của NGƯỜI BÁN (NBan/SDThoai, NBan/STKNHang, ...) — độc lập ngôn ngữ/layout.
    ///   3) (fallback) Nhãn tiếng Việt trong vùng người bán (tblSeller) — chỉ dùng khi không có XPath.
    ///
    /// Trường nào "đang hiển thị" ⇔ nhận diện được (1|2|3) VÀ không bị "#_NBxxx{display:none}".
    /// </summary>
    public static class InvoiceXsltConfigurator
    {
        private sealed class SellerField
        {
            public string Id = "";
            /// <summary>Mảnh XPath/khóa dữ liệu CHỈ thuộc người bán (NBan/... hoặc 'sellerXxx').</summary>
            public string[] Xpaths = Array.Empty<string>();
            /// <summary>Nhãn tiếng Việt (fallback, chỉ dùng trong vùng người bán).</summary>
            public string[] Labels = Array.Empty<string>();
        }

        // Định nghĩa field theo dữ liệu chuẩn, không gắn với 1 mẫu nào.
        private static readonly SellerField[] Fields =
        {
            new() { Id = "_NBSDT",     Xpaths = new[] { "NBan/SDThoai", "sellerPhone", "sellerTel", "sellerSDThoai" }, Labels = new[] { "Điện thoại" } },
            new() { Id = "_NBFax",     Xpaths = new[] { "NBan/Fax", "sellerFax" },                                     Labels = new[] { "Fax" } },
            new() { Id = "_NBEmail",   Xpaths = new[] { "NBan/DCTDTu", "NBan/Email", "sellerEmail" },                   Labels = new[] { "Email", "Thư điện tử" } },
            new() { Id = "_NBWebsite", Xpaths = new[] { "NBan/Website", "sellerWebsite" },                              Labels = new[] { "Website", "Trang web" } },
            new() { Id = "_NBSTK",     Xpaths = new[] { "NBan/STKNHang", "sellerBankAccount" },                         Labels = new[] { "Số tài khoản" } },
        };

        // Các ô không bao giờ ẩn (tên/MST/địa chỉ người bán) → không gắn id nhầm khi tên DN chứa "Fax/Email"...
        private static readonly string[] NeverToggleXpaths = { "NBan/Ten", "NBan/MST", "NBan/DChi" };

        private static readonly Regex SellerTableRx = new(
            @"(<table\b[^>]*\bid\s*=\s*[""']tblSeller[""'][^>]*>)(.*?)(</table>)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex TdRx = new(@"<td\b[^>]*>.*?</td>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex CommentRx = new(@"<!--.*?-->", RegexOptions.Singleline);

        private static readonly Regex BorderImageRx = new(
            @"\.(?:vienhd|page)\b[^{}]*\{[^}]*border-image\s*:\s*url\(\s*['""]?([^'""\)]+?)['""]?\s*\)\s*([\d.]+)?",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        #region INJECT (ghi cấu hình vào thân XSLT)

        /// <summary>
        /// Gắn id chuẩn (_NBxxx) cho các ô thông tin người bán (nếu chưa có) và bảo đảm viền hiển thị
        /// kể cả khi trình kết xuất bên kia không chạy JavaScript. Idempotent.
        /// </summary>
        public static string InjectVisibilityHooks(string xslt)
        {
            if (string.IsNullOrEmpty(xslt)) return xslt;
            try
            {
                string result = EnsureBorderClass(InjectSellerIds(xslt));
                // An toàn tuyệt đối: nếu kết quả không còn well-formed thì giữ nguyên bản gốc.
                return IsWellFormed(result) ? result : xslt;
            }
            catch
            {
                return xslt;
            }
        }

        private static bool IsWellFormed(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
                using var sr = new StringReader(xml);
                using var reader = XmlReader.Create(sr, settings);
                while (reader.Read()) { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string InjectSellerIds(string xslt)
        {
            var table = SellerTableRx.Match(xslt);
            if (table.Success)
            {
                // Có bảng người bán → trong phạm vi này được phép dùng cả nhãn.
                string newInner = TdRx.Replace(table.Groups[2].Value, td => TagCell(td.Value, allowLabel: true));
                return xslt.Substring(0, table.Groups[2].Index)
                     + newInner
                     + xslt.Substring(table.Groups[2].Index + table.Groups[2].Length);
            }

            // Không có tblSeller → chỉ gắn theo XPath người bán (an toàn, tránh nhầm người mua).
            return TdRx.Replace(xslt, td => TagCell(td.Value, allowLabel: false));
        }

        private static string TagCell(string cell, bool allowLabel)
        {
            if (Regex.IsMatch(cell, @"<td\b[^>]*\bid\s*=", RegexOptions.IgnoreCase))
                return cell; // tôn trọng id sẵn có

            string text = CommentRx.Replace(cell, string.Empty); // bỏ comment khi dò
            if (NeverToggleXpaths.Any(xp => Contains(text, xp)))
                return cell; // ô tên/MST/địa chỉ

            var field = MatchField(text, allowLabel);
            if (field == null) return cell;

            return Regex.Replace(cell, @"^<td\b", $"<td id=\"{field.Id}\"", RegexOptions.IgnoreCase);
        }

        private static SellerField? MatchField(string text, bool allowLabel)
        {
            // Ưu tiên XPath (đáng tin, độc lập layout/ngôn ngữ).
            foreach (var f in Fields)
                if (f.Xpaths.Any(xp => Contains(text, xp)))
                    return f;

            if (!allowLabel) return null;

            foreach (var f in Fields)
                if (f.Labels.Any(lb => Contains(text, lb)))
                    return f;

            return null;
        }

        /// <summary>
        /// Khi mẫu có viền (border-image trên .vienhd/.page), gán sẵn class "vienhd" cho container một-trang
        /// để viền không phụ thuộc hoàn toàn vào JS pagination của mẫu.
        /// </summary>
        private static string EnsureBorderClass(string xslt)
        {
            if (!BorderImageRx.IsMatch(xslt)) return xslt;

            return Regex.Replace(
                xslt,
                @"(<xsl:otherwise>\s*)container(\s*</xsl:otherwise>)",
                "$1container vienhd$2",
                RegexOptions.IgnoreCase);
        }

        #endregion

        #region DETECT (đọc cấu hình từ thân XSLT để FE tick checkbox)

        /// <summary>
        /// Dò trạng thái hiển thị hiện tại của file mẫu để FE tick/untick chính xác.
        /// </summary>
        public static AdjustConfigDto Detect(string? xslt)
        {
            var cfg = new AdjustConfigDto
            {
                VienConfig = new VienConfig(),
                LogoPos = new PosConfig(),
                BackgroundPos = new PosConfig(),
            };
            if (string.IsNullOrEmpty(xslt)) return cfg;

            string doc = CommentRx.Replace(xslt!, string.Empty); // toàn văn (đã bỏ comment) cho XPath/id

            // Vùng người bán cho nhãn fallback (tránh nhầm người mua).
            string? sellerScope = null;
            var ms = SellerTableRx.Match(xslt!);
            if (ms.Success) sellerScope = CommentRx.Replace(ms.Groups[2].Value, string.Empty);

            bool Present(SellerField f)
            {
                if (Regex.IsMatch(xslt!, $@"\bid\s*=\s*[""']{Regex.Escape(f.Id)}[""']", RegexOptions.IgnoreCase))
                    return true;                                   // (1) id chuẩn sẵn có
                if (f.Xpaths.Any(xp => Contains(doc, xp)))
                    return true;                                   // (2) XPath người bán
                if (sellerScope != null && f.Labels.Any(lb => Contains(sellerScope, lb)))
                    return true;                                   // (3) nhãn trong vùng người bán
                return false;
            }

            bool HiddenByCss(string id) => Regex.IsMatch(
                xslt!, $@"#\s*{Regex.Escape(id)}\s*\{{[^}}]*display\s*:\s*none", RegexOptions.IgnoreCase);

            bool Shown(SellerField f) => Present(f) && !HiddenByCss(f.Id);

            cfg.IsSoDT = Shown(Fields[0]);
            cfg.IsFax = Shown(Fields[1]);
            cfg.IsEmail = Shown(Fields[2]);
            cfg.IsWebsite = Shown(Fields[3]);
            cfg.IsTaiKhoanNganHang = Shown(Fields[4]);

            // Song ngữ: .en bị "display:none" ⇒ đang tắt.
            cfg.IsSongNgu = !Regex.IsMatch(
                xslt!, @"\.en\b[^{}]*\{[^}]*display\s*:\s*none", RegexOptions.IgnoreCase);

            // Viền: tên file + độ mạnh (% round) nếu có.
            var vm = BorderImageRx.Match(xslt!);
            if (vm.Success)
            {
                string url = vm.Groups[1].Value.Trim();
                int slash = url.LastIndexOf('/');
                cfg.IsThayDoiVien = true;
                cfg.VienConfig.SelectedVien = (slash >= 0 && slash < url.Length - 1) ? url.Substring(slash + 1) : url;
                if (vm.Groups[2].Success &&
                    decimal.TryParse(vm.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doManh))
                {
                    cfg.VienConfig.DoManh = doManh;
                }
            }

            return cfg;
        }

        #endregion

        private static bool Contains(string haystack, string needle) =>
            haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
