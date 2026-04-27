using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IChunkUploadService
    {
        Task<string> SaveChunkAsync(string sessionId, int chunkIndex, IFormFile chunk, CancellationToken ct = default);
        Task<string> MergeChunksAsync(string sessionId, string fileName, string oid, int totalChunks, CancellationToken ct = default);
        void CleanupSession(string sessionId);
    }
}
