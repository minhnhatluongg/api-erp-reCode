using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces.Accounts_payable
{
    public interface IFileStorageRepository
    {
        // ==========================================================
        //  COMMAND
        // ==========================================================

        /// <summary>
        /// Lưu file mới vào storage. Sinh tên file an toàn (tránh trùng),
        /// trả về metadata (Path / Url / Size) để Application lưu vào DB.
        /// </summary>
        /// <param name="content">Stream nội dung file.</param>
        /// <param name="fileName">Tên gốc từ user, VD "tvan.jpg".</param>
        /// <param name="contentType">MIME type, VD "image/jpeg".</param>
        /// <param name="type">Phân loại: "transfer" / "invoice" / "avatar"... — quyết định folder con.</param>
        Task<UploadedFile> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            string type,
            CancellationToken ct = default);

        /// <summary>
        /// Lưu file từ byte[] (tiện khi upload từ base64).
        /// </summary>
        Task<UploadedFile> SaveAsync(
            byte[] content,
            string fileName,
            string contentType,
            string type,
            CancellationToken ct = default);

        /// <summary>
        /// Xoá file theo path (dùng khi user bỏ chọn ảnh đã upload).
        /// </summary>
        Task<bool> DeleteAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Copy file sang vị trí mới (dùng khi nhân bản phiếu, clone ảnh đính kèm).
        /// </summary>
        Task<UploadedFile> CopyAsync(string sourcePath, string destType, CancellationToken ct = default);


        // ==========================================================
        //  QUERY
        // ==========================================================

        /// <summary>
        /// Đọc file thành stream (dùng cho endpoint <c>GET /api/files/preview</c>).
        /// </summary>
        /// <returns>Stream + ContentType để controller trả <c>File(stream, contentType)</c>.</returns>
        Task<(Stream Stream, string ContentType, string FileName)> ReadAsync(
            string path,
            CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra file có tồn tại không (trước khi delete hoặc preview).
        /// </summary>
        Task<bool> ExistsAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Lấy metadata file (không load content) — size, lastModified, contentType.
        /// </summary>
        Task<UploadedFile?> GetMetadataAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Sinh URL public để hiển thị / xem trước.
        /// Thường là <c>/api/files/preview?path={urlEncoded}</c>.
        /// </summary>
        string BuildPublicUrl(string path);
    }
}
