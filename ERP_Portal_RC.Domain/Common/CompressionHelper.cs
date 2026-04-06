using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common
{
    public static class CompressionHelper
    {
        public static string DecompressGZip(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0) return string.Empty;

            using (var memoryStream = new MemoryStream(compressedData))
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(gZipStream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
