using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class ClamAvVirusScanService : IVirusScanService
    {
        private readonly VirusScanConfig _config;
        private readonly ILogger<ClamAvVirusScanService> _logger;

        public ClamAvVirusScanService(
            IOptions<FileUploadConfig> config,
            ILogger<ClamAvVirusScanService> logger)
        {
            _config = config.Value.VirusScan;
            _logger = logger;
        }
        public async Task<(bool IsClean, string VirusName)> ScanAsync(Stream fileStream, CancellationToken ct = default)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(_config.ClamAvHost, _config.ClamAvPort, ct);
                await using var net = tcp.GetStream();

                // Gửi lệnh INSTREAM
                var cmd = "zINSTREAM\0"u8.ToArray();
                await net.WriteAsync(cmd, ct);

                // Gửi dữ liệu theo chunk 4KB
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, ct)) > 0)
                {
                    var sizeBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(bytesRead));
                    await net.WriteAsync(sizeBytes, ct);
                    await net.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                }

                // Kết thúc stream
                await net.WriteAsync(new byte[4], ct);
                await net.FlushAsync(ct);

                // Đọc kết quả
                var responseBuffer = new byte[1024];
                int responseLen = await net.ReadAsync(responseBuffer, ct);
                string response = Encoding.UTF8.GetString(responseBuffer, 0, responseLen).Trim('\0').Trim();

                // "stream: OK" = sạch, ngược lại chứa tên virus
                bool isClean = response.EndsWith("OK");
                string virus = isClean ? "" : response.Replace("stream: ", "").Replace(" FOUND", "");

                return (isClean, virus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VirusScan] Không thể kết nối ClamAV, bỏ qua scan.");
                return (true, ""); // Fail-open: không block upload nếu scanner lỗi
            }
        }
    }
}
