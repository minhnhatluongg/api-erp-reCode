using ERP_Portal_RC.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IFileStorageService
    {
        //Task<string> UploadFileAsync(IFormFile file, string subFolder);
        // Overload mới với CancellationToken (giữ tương thích cũ)
        Task<string> UploadFileAsync(IFormFile file, string subFolder, CancellationToken ct);

        /// <summary>
        /// Upload file kèm UserCode (lấy từ JWT) để lưu vào metadata.
        /// </summary>
        Task<ContractFileMetadata> UploadFileAsync(
            IFormFile file, string subFolder, string uploadedBy, CancellationToken ct);

        ContractFilesResponse GetFilesByOid(string oid, int year, int month);

        Task<string> RebuildMetadata(string oid, int year, int month, CancellationToken ct);
        List<ContractFileMetadata> GetAllFilesByOid(string oid);
    }
}
