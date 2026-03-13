using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class FileUploadViewModel
    {
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; }
        public long Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Extension { get; set; }
        public string? Source { get; set; }
    }
}
