using Dapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Data;
using System.Security.Claims;
using System.Transactions;
using System.Web;

namespace API.ERP_Portal_RC.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EcontractController : Controller
    {
        //[ApiExplorerSettings(IgnoreApi = true)] -> Check các API bị ẩn
        private readonly IEcontractService _econtractService;
        private readonly IConfiguration _configuration;
        public EcontractController(IEcontractService econtractService, IConfiguration configuration)
        {
            _econtractService = econtractService;
            _configuration = configuration;
        }
        /// <summary>
        /// Lấy thống kê dashboard hợp đồng (đếm theo trạng thái).
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ismanager"></param>
        /// <returns></returns>
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

        /// <summary>
        /// API lấy danh sách hợp đồng điện tử có phân trang và lọc theo điều kiện.
        /// </summary>
        /// <param name="request">Các tham số lọc: Từ ngày, Đến ngày, Từ khóa, Trạng thái, Trang hiện tại, Số dòng/trang.</param>
        /// <param name="ismanager">Flag xác định quyền quản lý (true: xem toàn bộ, false: xem cá nhân).</param>
        /// <returns>Trả về đối tượng <see cref="ApiResponse{ListEcontractViewModel}"/> chứa danh sách hợp đồng và thông tin phân trang.</returns>
        /// <response code="200">Lấy dữ liệu thành công.</response>
        /// <response code="401">Người dùng chưa đăng nhập hoặc Token hết hạn.</response>
        /// <response code="500">Lỗi hệ thống phát sinh tại server.</response>
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
        /// <summary>
        /// Lấy toàn bộ hợp đồng theo bộ lọc mở rộng, trả kèm meta tiền.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Lấy mẫu hợp đồng theo loại (original / compensation / extension).
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Tạo file xem trước hợp đồng từ dữ liệu nháp.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Lưu và phê duyệt hợp đồng trong một lần gọi.
        /// </summary>
        /// <param name="request">Dữ liệu hợp đồng từ các bước nhập liệu trên FE</param>
        /// <returns>Kết quả thực hiện</returns>
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
        /// <summary>
        /// Lấy trạng thái tổng hợp + lịch sử duyệt theo OID.
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Xóa bản nháp hợp đồng (chỉ được xóa khi chưa trình ký).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("draft")]
        public async Task<IActionResult> DeleteDraft([FromBody] DeleteEcontractRequest request)
        {
            var username = User.FindFirst("UserCode")?.Value;
            var result = await _econtractService.DeleteDraftAsync(request, username);
            return Ok(result);
        }
        /// <summary>
        /// Hủy ký hợp đồng.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("unsign")]
        public async Task<IActionResult> UnSign([FromBody] UnSignRequest request)
        {
            if (string.IsNullOrEmpty(request.RequestedBy))
                request.RequestedBy = User.Identity?.Name ?? "system";

            var result = await _econtractService.UnSignAsync(request);
            return Ok(result);
        }
        /// <summary>
        /// Lấy lịch sử các job xử lý theo OID hợp đồng.
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpGet("job-history")]
        public async Task<IActionResult> GetJobHistory([FromQuery] string oid)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("OID không hợp lệ."));

            var result = await _econtractService.GetJobHistoryAsync(oid);
            return Ok(result);
        }
        /// <summary>
        /// Lấy danh sách job kỹ thuật (JobKT) theo OID.
        /// </summary>
        /// <param name="OID"></param>
        /// <returns></returns>
        [HttpGet("getjobKT")]
        public async Task<IActionResult> GetJobKT(string OID)
        {
            var response = new ApiResponse<List<JobEntity>>();

            try
            {
                var result = await _econtractService.GetJobKTbyOID(OID);

                if (result != null && result.Any())
                {
                    response.Success = true;
                    response.Data = result;
                    response.Message = "Lấy dữ liệu thành công.";
                    response.StatusCode = 200;
                    return Ok(response);
                }

                response.Success = false;
                response.Message = "Không tìm thấy dữ liệu yêu cầu.";
                response.StatusCode = 404;
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Đã xảy ra lỗi hệ thống: " + ex.Message;
                response.StatusCode = 500;
                return StatusCode(500, response);
            }
        }
        /// <summary>
        /// Lấy chi tiết nội dung hợp đồng theo OID.
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpGet("get-details/{oid}")] 
        public async Task<IActionResult> GetEContractDetails(string oid)
        {
            var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userName)) return Unauthorized();

            var result = await _econtractService.GetEContractDetailsActionAsync(oid);

            return Ok(result);
        }
        /// <summary>
        /// Lấy chi tiết thông tin Job và Hợp đồng cho giao diện "Yêu cầu tạo mẫu"
        /// </summary>
        /// <param name="oid">Mã OID của hợp đồng (Ví dụ: 000642/260302:155826516)</param>
        /// <param name="kt">Flag kiểm tra kỹ thuật (0: Bình thường, 1: Xóa job cũ tạo job mới)</param>
        [HttpGet("get-job-details")]
        public async Task<IActionResult> GetJobDetails([FromQuery] string oid, [FromQuery] string kt = "0")
        {
            if (string.IsNullOrEmpty(oid))
            {
                return BadRequest(ApiResponse<EContractDetailsViewModel>.ErrorResponse("OID không được để trống."));
            }
            var result = await _econtractService.GetJobDetailsAsync(oid, kt);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        /// <summary>
        /// Lấy danh sách phòng ban có quyền thao tác với hợp đồng điện tử (Dựa trên claim "OperDeptList" trong token). Có phân trang.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [HttpGet("get-departments")]
        public async Task<IActionResult> GetDepartments(int pageSize = 10, int pageNumber = 1)
        {
            var operDeptList = User.FindFirst("OperDeptList")?.Value;
            if (string.IsNullOrEmpty(operDeptList))
            {
                return Ok(PagedResponse<DepartmentDTO>.Create(new List<DepartmentDTO>(), pageNumber, pageSize, 0));
            }
            var response = await _econtractService.GetDepartmentsPagedAsync(operDeptList, pageNumber, pageSize);
            return Ok(response);
        }
        /// <summary>
        /// Xác minh chi tiết job theo mã số thuế và OID.
        /// </summary>
        /// <param name="cusTax"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpGet("verify-job")]
        public async Task<IActionResult> VerifyJob([FromQuery] string cusTax, [FromQuery] string oid)
        {
            if (string.IsNullOrEmpty(cusTax) || string.IsNullOrEmpty(oid))
            {
                return BadRequest(ApiResponse<List<EContractDetailDTO>>.ErrorResponse("Mã số thuế và OID không được để trống."));
            }
            string cleanedOid = System.Net.WebUtility.UrlDecode(oid).Trim();
            string cleanedTax = cusTax.Trim();
            var response = await _econtractService.VerifyJobDetailsAsync(cleanedTax, cleanedOid);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Upload file đính kèm hợp đồng (không giới hạn kích thước).
        /// </summary>
        /// <param name="files"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpPost("upload-files")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFiles(IFormFileCollection files, [FromQuery] string oid)
        {
            if (string.IsNullOrEmpty(oid))
                return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu OID của hợp đồng."));

            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Không có file nào được chọn."));
            }
            var response = await _econtractService.UploadContractFilesAsync(files, oid);
            return Ok(response);
        }
        /// <summary>
        /// Lấy danh sách file đã upload theo OID hợp đồng.
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpGet("list/{oid}")]
        public async Task<IActionResult> GetListFiles(string oid)
        {
            var response = await _econtractService.GetListFilesByOidAsync(oid);
            return Ok(response);
        }
        [HttpGet("download/{oid}/{fileName}")]
        public IActionResult DownloadFile(string oid, string fileName)
        {
            try
            {
                string uploadRoot = _configuration["ApiSettings:UploadPath"] ?? "D:\\IIS WEB\\api-erprc.win-tech.vn\\Uploads";

                string folderName = oid.Replace("/", "").Replace(":", "");
                string filePath = Path.Combine(uploadRoot, folderName, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File không tồn tại trên hệ thống.");

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("save-job")]
        public async Task<IActionResult> SaveJob([FromBody] SaveJobRequestDto request)
        {
            var fullNameFromToken = User.FindFirst("FullName")?.Value;
            var userCode = User.FindFirst("UserCode")?.Value;

            if (string.IsNullOrEmpty(fullNameFromToken) || string.IsNullOrEmpty(userCode))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Phiên làm việc hết hạn hoặc không hợp lệ."));
            }

            request.EmplName = fullNameFromToken;

            var response = await _econtractService.SaveJobAsync(request, userCode);
            return Ok(response);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("approve-job-now")]
        public async Task<IActionResult> ApproveJobNow([FromBody] ApproveJobRequestDto request)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            var fullName = User.FindFirst("FullName")?.Value;

            if (string.IsNullOrEmpty(userCode)) return Unauthorized();
            var result = await _econtractService.ApproveJobNowAsync(request, userCode, fullName);
            return Ok(result);
        }
        /// <summary>
        /// Lấy thông tin hiển thị chi tiết hợp đồng theo OID.
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpGet("get")]
        public async Task<IActionResult> Get([FromQuery] string oid)
        {
            if (string.IsNullOrEmpty(oid))
            {
                return Ok(ApiResponse<object>.ErrorResponse("Vui lòng cung cấp mã OID hợp đồng."));
            }

            try
            {
                var userCode = User.FindFirst("UserCode")?.Value ?? "";
                var grpList = User.FindFirst("GrpList")?.Value ?? "";
                var firstClaim = User.Claims.FirstOrDefault()?.Value ?? "";
                var result = await _econtractService.GetContractDetailForDisplayAsync(oid, userCode, grpList, firstClaim);

                if (result == null)
                {
                    return Ok(ApiResponse<object>.ErrorResponse("Không tìm thấy dữ liệu hợp đồng hoặc lỗi truy vấn."));
                }

                return Ok(ApiResponse<EContractsViewModel>.SuccessResponse(result, "Lấy chi tiết hợp đồng thành công."));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// <summary>
        /// Kiểm tra hợp đồng đã được trình ký hay chưa.
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        [HttpGet("check-submitted/{oid}")]
        public async Task<IActionResult> CheckStatus(string oid)
        {
            string cleanedOid = System.Net.WebUtility.UrlDecode(oid).Trim();
            var isSubmitted = await _econtractService.CheckIfSubmitted(cleanedOid);
            return Ok(new { OID = cleanedOid, IsSubmitted = isSubmitted });
        }

        /// <summary>
        /// Lấy OID của job kế tiếp dựa trên OID hợp đồng chính. ( tiền tố OID - 00x )
        /// </summary>
        /// <param name="mainOid"></param>
        /// <returns></returns>
        [HttpGet("next-job-oid/{mainOid}")]
        public async Task<IActionResult> GetNextOid(string mainOid)
        {
            string decodedOid = System.Net.WebUtility.UrlDecode(mainOid);
            var response = await _econtractService.GetNextJobOIDAsync(decodedOid);
            return Ok(response);
        }

        /// <summary>
        /// Khởi tạo một yêu cầu xử lý công việc (Job) mới.
        /// </summary>
        /// <param name="request">
        /// Thông tin yêu cầu. Danh sách mã **FactorID,EntryID** xử lý:
        /// 
        /// 
        /// - ** EntryID:(JB:001) ** → Tạo mẫu có sẵn (FactorID :JOB_00001)
        /// - ** EntryID:(JB:002) ** → Tạo mẫu thiết kế (FactorID :JOB_00001)
        /// - ** EntryID:(JB:005) ** → Điều chỉnh mẫu (FactorID :JOB_00001)
        /// - ** EntryID:(JB:004) ** → Phát hành hóa đơn (FactorID :JOB_00002)
        /// - ** EntryID:(JB:003) ** → Kích hoạt tài khoản (FactorID :JOB_00003) 
        /// - ** EntryID:(JB:006) ** → Đề xuất chỉnh sửa (FactorID :JOB_00003)
        /// - ** EntryID:(JB:012) ** → Kiểm tra mẫu (FactorID :JOB_00006)
        /// - ** EntryID:(JB:010) ** → Xuất hóa đơn Hóa Đơn Điện Tử (FactorID :JOB_00005)
        /// 
        /// - ** Gửi lần đầu sẽ luôn ở trạng thái trình kí ( SignNumb : 101) cho mọi yêu cầu.
        /// </param>
        /// <response code="200">Khởi tạo Job thành công</response>
        /// <response code="401">Không tìm thấy thông tin định danh người dùng</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ</response>
        [HttpPost("create-job")]
        public async Task<IActionResult> CreateJob([FromBody] InsertJobRequest request)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userCode))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin định danh người dùng", 401));
            }
            request.Crt_User = userCode;
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu đầu vào không hợp lệ", 400));

            var result = await _econtractService.CreateJobAsync(request);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Kiểm tra trạng thái hiện tại của một job theo referenceId + factorId + entryId.
        /// </summary>
        /// <param name="referenceId"></param>
        /// <param name="factorId"></param>
        /// <param name="entryId"></param>
        /// <returns></returns>
        [HttpGet("check-status-job")]
        public async Task<IActionResult> CheckStatus(
            [FromQuery] string referenceId,
            [FromQuery] string factorId,
            [FromQuery] string entryId)
        {
            var result = await _econtractService.GetJobStatusAsync(referenceId, factorId, entryId);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Đề xuất cấp tài khoản cho khách hàng từ hợp đồng điện tử.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("de-xuat-captk")]
        [ProducesResponseType(typeof(ApiResponse<DeXuatCapTaiKhoanResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DeXuatCapTaiKhoanResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<DeXuatCapTaiKhoanResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeXuat([FromBody] DeXuatCapTaiKhoanRequestDto request)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userCode))
            {
                return Unauthorized(ApiResponse<DeXuatCapTaiKhoanResponseDto>.ErrorResponse(
                    "Phiên làm việc hết hạn hoặc không tìm thấy thông tin người dùng.", statusCode: 401));
            }
            request.CrtUser = userCode;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<DeXuatCapTaiKhoanResponseDto>.ErrorResponse(
                    "Dữ liệu không hợp lệ.", statusCode: 400, errors: errors));
            }
            try
            {
                var response = await _econtractService.DeXuatCapTaiKhoanAsync(request);

                return response.Success
                    ? Ok(response)
                    : BadRequest(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse<DeXuatCapTaiKhoanResponseDto>.ErrorResponse(
                        "Lỗi hệ thống.", statusCode: 500,
                        errors: new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy danh sách hợp đồng đang ở trạng thái 101 (chờ kiểm tra).
        /// </summary>
        /// <param name="frmDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("waiting-verify-101")]
        public async Task<ActionResult<ApiResponse<IEnumerable<EContract101Response>>>> GetList101(
        [FromQuery] string frmDate = "",
        [FromQuery] string endDate = "")
        {
            try
            {
                var data = await _econtractService.GetWaitingContracts(frmDate, endDate);

                if (data == null || !data.Any())
                {
                    return Ok(ApiResponse<IEnumerable<EContract101Response>>.SuccessResponse(
                        data, "Không tìm thấy hợp đồng nào đang chờ kiểm tra."));
                }
                return Ok(ApiResponse<IEnumerable<EContract101Response>>.SuccessResponse(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã có lỗi hệ thống xảy ra",
                    500,
                    new List<string> { ex.Message }));
            }
        }

        [HttpGet("list-paged")]
        public async Task<IActionResult> GetPaged([FromQuery] EContractPagedRequest request)
        {
            var userCode = User.FindFirstValue("UserCode") ?? "";
            var userName = User.FindFirstValue("UserName") ?? "";
            var grpList = User.FindFirstValue("GrpList") ?? "";

            var result = await _econtractService.GetPagedAsync(userCode, userName, grpList, request);
            return Ok(result);
        }
    }
}

