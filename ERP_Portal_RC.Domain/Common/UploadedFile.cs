using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common
{
    public class UploadedFile
    {
        /// <summary>Đường dẫn vật lý / relative path để lưu vào DB, VD "transfer/2026/04/tvan_abc123.jpg".</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>URL public để hiển thị/preview, VD "/api/files/preview?path=...".</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Tên file gốc user upload, VD "tvan.jpg".</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>MIME type, VD "image/jpeg", "application/pdf".</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Size byte.</summary>
        public long Size { get; set; }

        /// <summary>Phân loại (transfer / invoice / avatar…).</summary>
        public string? Type { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
