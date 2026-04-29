using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class FileUploadConfig
    {
        public const string Section = "FileUpload";

        public string BaseUrl { get; set; } = "";
        public string PhysicalRootPath { get; set; } = "";
        public long MaxFileSizeBytes { get; set; } = 52_428_800; // 50MB default
        public long ChunkSizeBytes { get; set; } = 5_242_880;  // 5MB per chunk
        public List<string> AllowedExtensions { get; set; } = new();
        public List<string> AllowedMimeTypes { get; set; } = new();
        public VirusScanConfig VirusScan { get; set; } = new();
    }
    public class VirusScanConfig
    {
        public bool Enabled { get; set; } = false;
        public string ClamAvHost { get; set; } = "localhost";
        public int ClamAvPort { get; set; } = 3310;
    }
}
