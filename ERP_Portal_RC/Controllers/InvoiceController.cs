using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Tạo hóa đơn nháp từ OID hợp đồng.
        /// </summary>
        /// <param name="contractOid">OID hợp đồng. Ví dụ: 000642/260326:133237113</param>
        /// <param name="invoiceType">
        /// Loại mẫu hóa đơn (default: multi):
        /// - **multi**  → Đa thuế suất  → mẫu 1C26TAT, invVatRate = -1
        /// - **single** → Đơn thuế suất → mẫu 1C26TAA, invVatRate = thuế suất thực
        /// </param>
        [HttpPost("{contractOid}/create-draft-invoice")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceDraftDataDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<InvoiceDraftDataDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDraftInvoice(
            [FromRoute] string contractOid,
            [FromQuery] string invoiceType = "multi",
            CancellationToken cancellationToken = default)
        {
            var request = new CreateInvoiceFromContractDto
            {
                ContractOid = Uri.UnescapeDataString(contractOid),
                InvoiceType = invoiceType.ToLower() == "single"
                    ? InvoiceType.Single
                    : InvoiceType.Multi
            };

            var result = await _invoiceService.CreateDraftInvoiceAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<InvoiceDraftDataDto>.SuccessResponse(
                    data: result.Data!,
                    message: "Tạo hóa đơn nháp thành công."));
            }

            var errorMessage = result.ErrorSource == "WinInvoice"
                ? $"[WinInvoice] {result.ErrorMessage}"
                : $"[Hệ thống] {result.ErrorMessage}";

            var errors = result.ErrorCode is not null
                ? new List<string> { $"ErrorCode: {result.ErrorCode}" }
                : null;

            return BadRequest(ApiResponse<InvoiceDraftDataDto>.ErrorResponse(
                message: errorMessage,
                statusCode: 400,
                errors: errors));
        }
    }
}
