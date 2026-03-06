using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.ERP_Portal_RC.Controllers
{
    /// <summary>
    /// Controller xử lý ký số hợp đồng điện tử.
    /// Hỗ trợ 3 phương thức: SERVER, APP (SignApp), HSM.
    /// Bao gồm 4 API callback phục vụ SignApp theo đúng document:
    ///   2.a ReceiveSignStatus · 2.b ValidJwt · 2.c GetInvParam · 2.d GetXmlAll · 2.e SetSignedXml
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ContractSignController : ControllerBase
    {
        private readonly IContractSignService _signService;

        public ContractSignController(IContractSignService signService)
        {
            _signService = signService;
        }

        #region Main Sign Contract APIs

        /// <summary>
        /// Ký hợp đồng điện tử. SignMethod: SERVER (default) | APP | HSM.
        /// </summary>
        [HttpPost("Sign")]
        public async Task<IActionResult> SignContract([FromBody] SignContractRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Ok(new { Status = -1, Message = "Dữ liệu không hợp lệ" });

                var userName = User.FindFirstValue(ClaimTypes.Name) ?? "system";

                // Service trả về Domain entity SignContractResult
                SignContractResult result = await _signService.SignContractAsync(request, userName);

                return Ok(new
                {
                    Status  = result.IsSuccess ? 1 : -1,
                    Message = result.Message,
                    Data    = result.Data
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Status = -1, Message = "Lỗi khi ký hợp đồng", ExMessage = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra hợp đồng đã được ký số chưa (tra cứu ECtr_PublicInfo).
        /// </summary>
        [HttpGet("IsSigned")]
        public async Task<IActionResult> IsSigned([FromQuery] string oid)
        {
            try
            {
                if (string.IsNullOrEmpty(oid))
                    return Ok(new { IsSigned = false, Message = "OID không được để trống" });

                var (isSigned, message) = await _signService.IsSignedAsync(oid);
                return Ok(new { IsSigned = isSigned, Message = message });
            }
            catch (Exception ex)
            {
                return Ok(new { IsSigned = false, Message = "Lỗi check trạng thái: " + ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái ký hợp đồng.
        /// </summary>
        [HttpGet("CheckStatus")]
        public async Task<IActionResult> CheckSignStatus(
            [FromQuery] string oid,
            [FromQuery] string signMethod = "SERVER")
        {
            try
            {
                if (string.IsNullOrEmpty(oid))
                    return Ok(new { Status = -1, Message = "Mã hợp đồng không được để trống" });

                // Service trả về Domain entity CheckSignStatusResult
                CheckSignStatusResult result = await _signService.CheckSignStatusAsync(oid, signMethod);

                return Ok(new
                {
                    result.Status,
                    result.Message,
                    result.Data
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Status = -1, Message = "Lỗi khi kiểm tra trạng thái", ExMessage = ex.Message });
            }
        }

        #endregion

        #region App Signing APIs (theo document SignApp)

        /// <summary>
        /// API 2.a: Tiếp nhận trạng thái xử lý từ SignApp (Callback).
        /// [AllowAnonymous] — SignApp gọi trực tiếp không qua JWT Bearer.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("ReceiveSignStatus")]
        [HttpPost("/api/invoice/change_status")]
        public async Task<IActionResult> ReceiveSignStatus([FromBody] SignStatusCallbackRequest request)
        {
            try
            {
                if (request == null)
                    return Ok(new
                    {
                        IsSuccess  = false,
                        Message    = "Request is null",
                        ReturnDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                    });

                var (isSuccess, message) = await _signService.ReceiveSignStatusAsync(request);

                return Ok(new
                {
                    IsSuccess  = isSuccess,
                    Data       = new { ApiInfo = "" },
                    Message    = message,
                    ReturnDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    IsSuccess  = false,
                    Data       = new { ApiInfo = "" },
                    Message    = "Lỗi: " + ex.Message,
                    ReturnDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                });
            }
        }

        /// <summary>API 2.b: Kiểm tra chuỗi JWT hợp lệ từ SignApp.</summary>
        [AllowAnonymous]
        [HttpPost("ValidJwt")]
        [HttpPost("/api/process/valid_jwt")]
        public IActionResult ValidJwt([FromBody] ValidJwtRequest request)
        {
            try
            {
                ValidJwtResponse result = _signService.ValidJwt(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new ValidJwtResponse
                {
                    IsSuccess = false,
                    Message   = "Lỗi: " + ex.Message,
                    ReturnDate = DateTime.Now
                });
            }
        }

        /// <summary>API 2.c: Lấy danh sách OID cần ký.</summary>
        [AllowAnonymous]
        [HttpPost("GetInvParam")]
        [HttpPost("/api/invoice/get_inv_param")]
        public async Task<IActionResult> GetInvParam([FromBody] GetInvParamRequest request)
        {
            try
            {
                if (request == null)
                    return Ok(new GetInvParamResponse
                    {
                        IsSuccess  = false,
                        Message    = "KeyID is required",
                        ReturnDate = DateTime.Now
                    });

                GetInvParamResponse result = await _signService.GetInvParamAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new GetInvParamResponse
                {
                    IsSuccess  = false,
                    Message    = "Lỗi: " + ex.Message,
                    ReturnDate = DateTime.Now
                });
            }
        }

        /// <summary>API 2.d: Lấy XML để ký số (Base64 encoded).</summary>
        [AllowAnonymous]
        [HttpPost("GetXmlAll")]
        [HttpPost("/api/invoice/get_xml_all")]
        public async Task<IActionResult> GetXmlAll([FromBody] GetXmlAllRequest request)
        {
            try
            {
                GetXmlAllResponse result = await _signService.GetXmlAllAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new GetXmlAllResponse
                {
                    IsSuccess  = false,
                    Message    = "Lỗi: " + ex.Message,
                    ReturnDate = DateTime.Now
                });
            }
        }

        /// <summary>API 2.e: Đẩy XML đã ký số lên server.</summary>
        [AllowAnonymous]
        [HttpPost("SetSignedXml")]
        [HttpPost("/api/invoice/set_xml_all")]
        public async Task<IActionResult> SetSignedXml([FromBody] SetSignedXmlRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.XmlContentBase64))
                    return Ok(new { IsSuccess = false, Message = "Dữ liệu yêu cầu bị rỗng" });

                SetSignedXmlResponse result = await _signService.SetSignedXmlAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new SetSignedXmlResponse
                {
                    IsSuccess  = false,
                    Message    = "Lỗi: " + ex.Message,
                    ReturnDate = DateTime.Now
                });
            }
        }

        #endregion
    }
}
