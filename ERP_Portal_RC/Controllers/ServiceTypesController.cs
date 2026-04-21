using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("api/service-types")]
    [ApiExplorerSettings(IgnoreApi = true)] 
    public class ServiceTypesController : ControllerBase
    {
        private readonly IServiceTypeService _service;
        private readonly ILogger<ServiceTypesController> _logger;

        // Lấy username từ JWT claim — fallback "system" nếu chưa có auth
        private string CurrentUser =>
            User.Identity?.Name ?? HttpContext.Request.Headers["X-User"].FirstOrDefault() ?? "system";

        public ServiceTypesController(IServiceTypeService service, ILogger<ServiceTypesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/service-types
        // GET /api/service-types?activeOnly=true
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách loại dịch vụ.
        /// activeOnly=true → chỉ trả về dịch vụ đang hoạt động (dùng cho dropdown tạo HĐ).
        /// activeOnly=false (mặc định) → trả về tất cả (dùng cho màn hình quản trị).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ServiceTypeDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            var data = activeOnly
                ? await _service.GetAllActiveAsync()
                : await _service.GetAllAsync();

            var meta = new Dictionary<string, object>
            {
                { "totalCount", data.Count() },
                { "activeOnly",  activeOnly   }
            };

            return Ok(ApiResponse<IEnumerable<ServiceTypeDto>>.SuccessResponseWithMeta(
                data: data,
                meta: meta,
                message: "Lấy danh sách loại dịch vụ thành công"
            ));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/service-types/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Chi tiết 1 loại dịch vụ theo ID.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ServiceTypeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ServiceTypeDto>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id);

            if (dto is null)
                return NotFound(ApiResponse<ServiceTypeDto>.ErrorResponse(
                    message: $"Không tìm thấy loại dịch vụ ID = {id}.",
                    statusCode: 404
                ));

            return Ok(ApiResponse<ServiceTypeDto>.SuccessResponse(dto));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/service-types
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Thêm loại dịch vụ mới.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> Create([FromBody] CreateServiceTypeDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    message: "Dữ liệu không hợp lệ.",
                    errors: errors
                ));
            }

            try
            {
                var newId = await _service.CreateAsync(dto, CurrentUser);

                return CreatedAtAction(
                    actionName: nameof(GetById),
                    routeValues: new { id = newId },
                    value: ApiResponse<object>.SuccessResponse(
                        data: new { ServiceTypeID = newId },
                        message: "Tạo loại dịch vụ thành công.",
                        statusCode: 201
                    )
                );
            }
            catch (InvalidOperationException ex)
            {
                // Code trùng
                return Conflict(ApiResponse<object>.ErrorResponse(
                    message: ex.Message,
                    statusCode: 409
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo ServiceType. Code={Code}", dto.Code);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    message: "Đã xảy ra lỗi hệ thống.",
                    statusCode: 500
                ));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/service-types/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cập nhật thông tin loại dịch vụ.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 409)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceTypeDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse.ErrorResponse(
                    message: "Dữ liệu không hợp lệ.",
                    errors: errors
                ));
            }

            try
            {
                var updated = await _service.UpdateAsync(id, dto, CurrentUser);

                return updated
                    ? Ok(ApiResponse.SuccessResponse("Cập nhật loại dịch vụ thành công."))
                    : StatusCode(500, ApiResponse.ErrorResponse("Cập nhật không thành công.", 500));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse.ErrorResponse(ex.Message, 409));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi update ServiceType ID={ID}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Đã xảy ra lỗi hệ thống.", 500));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/service-types/{id}  →  Soft-delete
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Vô hiệu hóa loại dịch vụ (soft-delete, set IsActive = false).
        /// Không xóa vật lý vì ServiceType có thể đang được dùng trong PaymentRecord cũ.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                await _service.DeactivateAsync(id);
                return Ok(ApiResponse.SuccessResponse("Vô hiệu hóa loại dịch vụ thành công."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.ErrorResponse(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi deactivate ServiceType ID={ID}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("Đã xảy ra lỗi hệ thống.", 500));
            }
        }
    }
}
