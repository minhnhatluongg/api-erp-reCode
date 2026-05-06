using ERP_Portal_RC.Domain.EntitiesInvoice;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InvoiceRepository> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public InvoiceRepository(IHttpClientFactory httpClientFactory, ILogger<InvoiceRepository> logger)
        {
            _httpClient = httpClientFactory.CreateClient("WinInvoiceClient");
            _logger = logger;
        }

        public async Task<WinInvoiceCreateResponse> CreateInvoiceAsync(
            WinInvoiceCreateRequest payload,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/api/invoice/add_type_2";

            var httpResponse = await _httpClient.PostAsJsonAsync(
                endpoint, payload, _jsonOptions, cancellationToken);

            var rawJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Raw Response from WinInvoice: {RawJson}", rawJson);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new WinInvoiceCreateResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {rawJson}"
                };
            }

            try
            {
                var result = JsonSerializer.Deserialize<WinInvoiceCreateResponse>(rawJson, _jsonOptions);
                return result ?? new WinInvoiceCreateResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Không deserialize được response từ WinInvoice."
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi Deserialize JSON từ WinInvoice. Raw: {RawJson}", rawJson);

                string? winInvoiceMessage = null;
                bool winInvoiceSuccess = false;
                string? winInvoiceErrorCode = null;
                string? winInvoiceInvRef = null;

                try
                {
                    using var doc = JsonDocument.Parse(rawJson);
                    var root = doc.RootElement;

                    winInvoiceSuccess    = root.TryGetProperty("isSuccess",    out var s) && s.GetBoolean();
                    winInvoiceMessage    = root.TryGetProperty("errorMessage", out var m) ? m.GetString() : null;
                    winInvoiceErrorCode  = root.TryGetProperty("ErrorCode",    out var c) ? c.ToString() : null;
                    winInvoiceInvRef     = root.TryGetProperty("invRef",       out var r) ? r.GetString() : null;
                }
                catch
                {
                    // Raw JSON cũng không parse được — trả nguyên raw
                }

                return new WinInvoiceCreateResponse
                {
                    IsSuccess    = winInvoiceSuccess,
                    ErrorMessage = winInvoiceMessage
                        ?? $"WinInvoice trả về dữ liệu không đọc được. Raw: {rawJson[..Math.Min(200, rawJson.Length)]}",
                    ErrorCode    = winInvoiceErrorCode,
                    InvRef       = winInvoiceInvRef
                };
            }
        }
    }
}
