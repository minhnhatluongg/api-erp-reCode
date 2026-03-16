using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        public FileStorageService(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            string uploadRoot = _configuration["FileConfig:PhysicalRootPath"];

            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString("00");

            string cleanSubFolder = subFolder.Replace("/", "_").Replace(":", "_").Trim();

            string relativePath = Path.Combine(year, month, cleanSubFolder);

            string physicalPath = Path.Combine(uploadRoot, relativePath);

            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
            }

            string fileName = $"{Guid.NewGuid()}_{RemoveDiacritics(file.FileName)}";
            string fullPhysicalPath = Path.Combine(physicalPath, fileName);

            using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine(relativePath, fileName).Replace("\\", "/");
        }
        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
