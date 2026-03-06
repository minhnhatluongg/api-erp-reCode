using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            // 1. Chuẩn hóa tên thư mục (Xóa ký tự đặc biệt như logic cũ)
            string folderName = subFolder.Replace("/", "").Replace(":", "").Trim();
            string uploadRoot = Path.Combine(_env.ContentRootPath, "Uploads", "Upload", folderName);

            if (!Directory.Exists(uploadRoot))
            {
                Directory.CreateDirectory(uploadRoot);
            }

            // 2. Xử lý tên file (Tiếng Việt không dấu)
            string fileName = RemoveDiacritics(file.FileName);
            string fullPath = Path.Combine(uploadRoot, fileName);

            // 3. Lưu file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fullPath;
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
