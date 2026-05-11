using System;
using System.Collections.Generic;

namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// Tổng hợp toàn bộ file của user đang đăng nhập — dùng cho dashboard "My Files".
    /// </summary>
    public class UserFilesSummaryResponse
    {
        public string UserCode { get; set; } = "";

        /// <summary>Tổng số file đã upload.</summary>
        public int TotalFiles { get; set; }

        /// <summary>Tổng dung lượng (bytes).</summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>Tổng dung lượng đã format (KB/MB/GB).</summary>
        public string TotalSizeFormatted { get; set; } = "";

        /// <summary>Số hợp đồng có ít nhất 1 file.</summary>
        public int TotalContracts { get; set; }

        /// <summary>Group file theo từng hợp đồng (OID).</summary>
        public List<ContractGroup> ByContract { get; set; } = new();

        /// <summary>Group file theo extension — vd .pdf: 12, .docx: 5.</summary>
        public List<ExtensionGroup> ByExtension { get; set; } = new();

        /// <summary>Group file theo tháng — vd 2026-05: 8.</summary>
        public List<MonthGroup> ByMonth { get; set; } = new();

        /// <summary>10 file mới upload gần nhất.</summary>
        public List<ContractFileMetadata> RecentFiles { get; set; } = new();
    }

    public class ContractGroup
    {
        public string Oid { get; set; } = "";
        public int FileCount { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastUploadedAt { get; set; }
    }

    public class ExtensionGroup
    {
        public string Extension { get; set; } = "";
        public int FileCount { get; set; }
        public long SizeBytes { get; set; }
    }

    public class MonthGroup
    {
        /// <summary>Format yyyy-MM, vd 2026-05.</summary>
        public string Month { get; set; } = "";
        public int FileCount { get; set; }
        public long SizeBytes { get; set; }
    }
}
