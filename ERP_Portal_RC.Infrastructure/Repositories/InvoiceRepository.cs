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

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public InvoiceRepository(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("WinInvoiceClient");
        }

        public async Task<WinInvoiceCreateResponse> CreateInvoiceAsync(
            WinInvoiceCreateRequest payload,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/api/invoice/add_type_2";

            var httpResponse = await _httpClient.PostAsJsonAsync(
                endpoint, payload, _jsonOptions, cancellationToken);

            var rawJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new WinInvoiceCreateResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {rawJson}"
                };
            }

            var result = JsonSerializer.Deserialize<WinInvoiceCreateResponse>(rawJson, _jsonOptions);

            return result ?? new WinInvoiceCreateResponse
            {
                IsSuccess = false,
                ErrorMessage = "Không deserialize được response từ WinInvoice."
            };
        }
    }
}
