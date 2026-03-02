using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Transactions;

namespace API.ERP_Portal_RC.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EcontractController : Controller
    {
        private readonly IEcontractService _econtractService;
        public EcontractController(IEcontractService econtractService)
        {
            _econtractService = econtractService;
        }

        [HttpGet("countContract")]
        public async Task<ActionResult<ApiResponse<object>>> CountContract(
            [FromQuery] ContractSearchRequest request,
            [FromQuery] bool ismanager)
        {
            try
            {
                var userName = User.Identity?.Name;
                var userCode = User.FindFirst("UserCode")?.Value;
                var grpList = User.FindFirst("Grp_List")?.Value;

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userCode))
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Không tìm thấy thông tin định danh người dùng."));
                }

                var dashboardData = await _econtractService.GetContractDashboardAsync(
                    userCode,
                    userName,
                    grpList ?? "",
                    ismanager,
                    request);

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    econtract = dashboardData
                }, "Lấy dữ liệu Dashboard thành công."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// API lấy danh sách hợp đồng chi tiết 
        [HttpGet("listContract")]
        public async Task<ActionResult<ApiResponse<ListEcontractViewModel>>> GetListContract(
            [FromQuery] ContractSearchRequest request,
            [FromQuery] bool ismanager)
        {
            try
            {
                var userName = User.Identity?.Name;
                var userCode = User.FindFirst("UserCode")?.Value;
                var grpList = User.FindFirst("Grp_List")?.Value;

                var result = await _econtractService.GetContractListAsync(
                    userCode!,
                    userName!,
                    grpList ?? "",
                    ismanager,
                    request);

                return Ok(ApiResponse<ListEcontractViewModel>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ListEcontractViewModel>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll([FromQuery] EContractFilterRequest request)
        {
            try
            {
                var loginName = User.FindFirst(ClaimTypes.Name)?.Value;
                var userCode = User.FindFirst("UserCode")?.Value;
                var groupList = User.FindFirst("Grp_List")?.Value ?? "";

                if (string.IsNullOrEmpty(userCode))
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Không tìm thấy mã người dùng trong Token.", 401));
                }

                var result = await _econtractService.GetAllEContractsAsync(loginName, request, groupList, userCode);

                var meta = new Dictionary<string, object>
                {
                    { "moneyToBePaid", result.MoneyToBePaid },
                    { "moneyPaid", result.MoneyPaid },
                    { "disable", result.Disable }
                };

                // 3. Tạo PagedResponse lồng trong ApiResponse
                var response = PagedResponse<EContract_Monitor>.Create(
                    result.Data,
                    request.Page,
                    request.PageSize,
                    result.Total
                );

                // Gán Meta bổ sung vào response
                response.Meta = meta;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.ErrorResponse($"Lỗi hệ thống: {ex.Message}", 400));
            }
        }

        /// Lấy mẫu hợp đồng dựa trên loại sử dụng ApiResponse wrapper
        /// <param name="type"> original | compensation | extension</param>
        [HttpGet("preview/{type}")]
        public async Task<ActionResult<ApiResponse<Template>>> GetTemplateByType(string type)
        {
            try
            {
                Template? template = null;

                switch (type.ToLower())
                {
                    case "original":
                        template = await _econtractService.GetOriginalContractAsync();
                        break;
                    case "compensation":
                        template = await _econtractService.GetCompensationContractAsync();
                        break;
                    case "extension":
                        template = await _econtractService.GetExtensionContractAsync();
                        break;
                    default:
                        return BadRequest(ApiResponse<Template>.ErrorResponse(
                            "Loại hợp đồng không hợp lệ. Vui lòng chọn: original, compensation hoặc extension.",
                            400));
                }

                if (template == null)
                {
                    return NotFound(ApiResponse<Template>.ErrorResponse(
                        "Không tìm thấy mẫu hợp đồng tương ứng trong hệ thống!",
                        404));
                }

                return Ok(ApiResponse<Template>.SuccessResponse(
                    template,
                    "Lấy thông tin mẫu hợp đồng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Template>.ErrorResponse(
                    $"Lỗi hệ thống: {ex.Message}",
                    500));
            }
        }

        /// API xem trước hợp đồng (Preview) trước khi lưu
        /// <param name="request">Dữ liệu nháp từ các bước nhập liệu trên FE</param>
        [HttpPost("generate-preview")]
        public async Task<ActionResult<ApiResponse<string>>> GeneratePreview([FromBody] ContractPreviewRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Dữ liệu yêu cầu không được để trống.", 400));
            }

            if (string.IsNullOrEmpty(request.FactorID))
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Vui lòng chọn loại mẫu hợp đồng (FactorID).", 400));
            }

            var result = await _econtractService.GenerateContractPreviewAsync(request);

            // 3. Trả về kết quả dựa trên trạng thái xử lý của Service
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        #region Trình kí / Yêu cầu Phát hành / Phát Hành Mẫu

        /// <summary>
        /// Trình ký hợp đồng điện tử: Đẩy trạng thái 0 → 101.
        /// Chỉ cần truyền OID. Các thông tin khác (MST, SampleID) được tự enrich từ DB.
        /// </summary>
        [HttpPost("propose-sign")]
        public async Task<IActionResult> ProposeSign([FromBody] ApprovalWorkflowRequest model)
        {
            var userId = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status401Unauthorized, new { status = 0, message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại." });

            if (string.IsNullOrWhiteSpace(model.OID))
                return BadRequest(new { status = 0, message = "OID không được để trống." });

            var saleFullName = User.FindFirst("FullName")?.Value ?? string.Empty;

            try
            {
                var (success, emailSent) = await _econtractService.ProposeSignContractAsync(model, userId, saleFullName);

                if (!success)
                    return BadRequest(new { status = 0, message = "Trình ký thất bại. Hợp đồng có thể đã được trình ký trước đó.", emailSent = false });

                return Ok(new
                {
                    status = 1,
                    message = "Trình ký hợp đồng thành công.",
                    emailSent,
                    emailMessage = emailSent
                        ? $"Email thông báo đã gửi tới ketoanhoadondientu@win-tech.vn"
                        : "Trình ký thành công nhưng gửi email thất bại (kiểm tra log server)."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = 0, message = ex.Message });
            }
        }

        /// <summary>
        /// Đề xuất tạo / phát hành mẫu hóa đơn (Job): Đẩy trạng thái 0 → 101.
        /// Truyền đầy đủ ReferenceID (contract OID), FactorID, EntryID và các field tùy chọn.
        /// </summary>
        [HttpPost("propose-template")]
        public async Task<IActionResult> ProposeTemplate([FromBody] EContractJobRequest model)
        {
            var userId = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status401Unauthorized, new { status = 0, message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại." });

            if (model == null)
                return BadRequest(new { status = 0, message = "Body request không được để trống." });

            // Validation các field bắt buộc (giống Odoo API cũ)
            if (string.IsNullOrWhiteSpace(model.ReferenceID) ||
                string.IsNullOrWhiteSpace(model.FactorID)    ||
                string.IsNullOrWhiteSpace(model.EntryID))
                return BadRequest(new { status = 0, message = "Thiếu thông tin bắt buộc: ReferenceID, FactorID, EntryID." });

            try
            {
                var (success, message) = await _econtractService.ProposeTemplateAsync(model, userId);
                return success
                    ? Ok(new { status = 1, message })
                    : BadRequest(new { status = 0, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = 0, message = ex.Message });
            }
        }

        /// <summary>
        /// Phát hành mẫu hóa đơn (Job): Đẩy trạng thái 101 → 201.
        /// Chỉ cần truyền OID.
        /// </summary>
        [HttpPost("issue-invoice")]
        [ApiExplorerSettings(IgnoreApi = true)] 
        public async Task<IActionResult> IssueInvoice([FromBody] ApprovalWorkflowRequest model)
        {
            var userId = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status401Unauthorized, new { status = 0, message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại." });

            if (string.IsNullOrWhiteSpace(model.OID))
                return BadRequest(new { status = 0, message = "OID không được để trống." });

            try
            {
                var (success, message) = await _econtractService.IssueInvoiceAsync(model, userId);
                return success
                    ? Ok(new { status = 1, message })
                    : BadRequest(new { status = 0, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = 0, message = ex.Message });
            }
        }

        #endregion

        [HttpPost("save-and-approve")]
        public async Task<IActionResult> SaveAndApprove([FromBody] ContractPreviewRequest request)
        {
            if (request == null || request.Details == null || !request.Details.Any())
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Dữ liệu hợp đồng không đầy đủ."));
            }

            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userCode))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Không tìm thấy mã người dùng trong Token.", 401));
            }
            var result = await _econtractService.ProcessSaveContractAsync(request, userCode);

            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        [HttpGet("get-status-summary")]
        public async Task<IActionResult> GetStatusSummary(string oid)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("OID không được để trống."));

            var result = await _econtractService.GetContractReviewDataAsync(oid);

            if (result == null)
                return Ok(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin hợp đồng", 404));

            return Ok(ApiResponse<ContractStatusResponse>.SuccessResponse(result, "Lấy thông tin thành công."));
        }

        [HttpDelete("draft")]
        public async Task<IActionResult> DeleteDraft([FromBody] DeleteEcontractRequest request)
        {
            var username = User.FindFirst("UserCode")?.Value;
            var result = await _econtractService.DeleteDraftAsync(request, username);
            return Ok(result);
        }

        [HttpPost("unsign")]
        public async Task<IActionResult> UnSign([FromBody] UnSignRequest request)
        {
            if (string.IsNullOrEmpty(request.RequestedBy))
                request.RequestedBy = User.Identity?.Name ?? "system";

            var result = await _econtractService.UnSignAsync(request);
            return Ok(result);
        }

        [HttpGet("job-history")]
        public async Task<IActionResult> GetJobHistory([FromQuery] string oid)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("OID không hợp lệ."));

            var result = await _econtractService.GetJobHistoryAsync(oid);
            return Ok(result);
        }
    }
}

