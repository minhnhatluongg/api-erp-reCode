using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public record MergeChunksRequest(
        string SessionId,
        string FileName,
        string Oid,
        int TotalChunks);
}
