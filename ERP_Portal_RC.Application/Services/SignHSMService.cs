using ERP_Portal_RC.Application.DTOs.SignHSM;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class SignHSMService : ISignHSMService
    {
        private readonly ISignHSMRepository _signHSMRepository;
        private readonly ILogger<SignHSMService> _logger;
        public SignHSMService(ISignHSMRepository signHSMRepository, ILogger<SignHSMService> logger)
        {
            _signHSMRepository = signHSMRepository;
            _logger = logger;
        }
        public async Task<ApiResponse<SaveSignedXmlResponseDto>> SaveSignedXmlAsync(SaveSignedXmlRequestDto request)
        {
            string oid = request.OID.Trim();
            _logger.LogInformation("[SignHSM] Bắt đầu lưu signed XML — OID={OID}", oid);

            try
            {
                // ── 1. Validate Base64 ────────────────────────────────────────
                if (!TryDecodeBase64Xml(request.SignedXmlBase64, out string decodedXml, out string base64Error))
                {
                    _logger.LogWarning("[SignHSM] Base64 không hợp lệ — OID={OID} | {Err}", oid, base64Error);
                    return ApiResponse<SaveSignedXmlResponseDto>.ErrorResponse(
                        $"SignedXmlBase64 không hợp lệ: {base64Error}", 400);
                }

                _logger.LogInformation(
                    "[SignHSM] Base64 OK — XML length={Len} chars — OID={OID}",
                    decodedXml.Length, oid);

                // ── 2. Lấy PayloadJson ────────────────────────────────────────
                string payloadJson = await _signHSMRepository.GetPayloadJsonAsync(oid);
                if (string.IsNullOrEmpty(payloadJson))
                    return ApiResponse<SaveSignedXmlResponseDto>.ErrorResponse(
                        $"Không tìm thấy PayloadData cho OID={oid}. Hãy chạy Sign trước.", 400);

                // ── 3. Map DTO + PayloadJson → SignHSMEntity (factory validate) ─
                SignHSMEntity entity = MapToEntity(oid, payloadJson, request.SignedXmlBase64);

                // ── 4. UpdateProcessStatus → đang lưu ────────────────────────
                await _signHSMRepository.UpdateProcessStatusAsync(oid, 1, "Đang lưu XML đã ký...");

                // ── 5. Gọi Repository (chỉ biết Entity) ──────────────────────
                SignHSMResult result = await _signHSMRepository.SaveSignedXmlAsync(entity);

                // ── 6. UpdateProcessStatus theo kết quả SP ───────────────────
                await _signHSMRepository.UpdateProcessStatusAsync(
                    oid,
                    result.IsSuccess ? 2 : -1,
                    result.IsSuccess ? "Đã ký thành công" : $"Lỗi lưu DB: {result.Message}");

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("[SignHSM] SP thất bại — OID={OID} | {Msg}", oid, result.Message);
                    return ApiResponse<SaveSignedXmlResponseDto>.ErrorResponse(
                        $"Lỗi lưu DB: {result.Message}", 400);
                }

                _logger.LogInformation("[SignHSM] Lưu thành công — OID={OID}", oid);

                return ApiResponse<SaveSignedXmlResponseDto>.SuccessResponse(
                    new SaveSignedXmlResponseDto { OID = oid, Message = result.Message },
                    "Lưu hợp đồng đã ký HSM thành công.");
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<SaveSignedXmlResponseDto>.ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignHSM] Exception — OID={OID}", oid);
                await _signHSMRepository.UpdateProcessStatusAsync(oid, -1, $"Lỗi: {ex.Message}");
                return ApiResponse<SaveSignedXmlResponseDto>.ErrorResponse(ex.Message, 500);
            }
        }
        #region Helpers
        private static SignHSMEntity MapToEntity(
            string oid, string payloadJson, string signedXmlBase64)
        {
            var data = JObject.Parse(payloadJson);
            var comp = data["CompanyInfo"];

            DateTime oDate = DateTime.Now;
            if (data["OrderDate"] != null)
                DateTime.TryParseExact(
                    data["OrderDate"]!.ToString(), "dd/MM/yyyy",
                    null, System.Globalization.DateTimeStyles.None, out oDate);

            return SignHSMEntity.Create(
                oid: oid,
                oDate: oDate,
                partyASoCCCD: data["PartnerVAT"]?.ToString() ?? "",
                partyATaxcode: data["PartnerVAT"]?.ToString() ?? "",
                partyAName: data["PartnerName"]?.ToString() ?? "",
                partyBTaxcode: comp?["TaxCode"]?.ToString() ?? "",
                partyBName: comp?["CompanyName"]?.ToString() ?? "",
                signedXmlBase64: signedXmlBase64);
        }

        private static bool TryDecodeBase64Xml(string base64, out string decoded, out string error)
        {
            decoded = "";
            error = "";

            if (string.IsNullOrEmpty(base64))
            {
                error = "Chuỗi base64 rỗng";
                return false;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                decoded = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                error = "Không phải chuỗi Base64 hợp lệ.";
                return false;
            }

            string trimmed = decoded.TrimStart();
            if (!trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
             && !trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                error = $"Nội dung sau decode không phải XML. Bắt đầu bằng: '{trimmed[..Math.Min(50, trimmed.Length)]}'";
                return false;
            }

            return true;
        }

        #endregion
    }
}
