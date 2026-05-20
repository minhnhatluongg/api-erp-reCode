using ERP_Portal_RC.Application.DTOs.Tax;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities.Tax;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    /// <summary>
    /// Orchestrates các call cross-server BosOnline / BosEVAT / BosTVAN.
    /// Repository chỉ làm 1 SP / 1 method, Service compose lại thành DTO trả về Controller.
    /// </summary>
    public class TaxService : ITaxService
    {
        private readonly ITaxRepository _taxRepo;
        private readonly IConnectionRepository _connection;
        private readonly ILogger<TaxService> _log;

        public TaxService(
            ITaxRepository taxRepo,
            IConnectionRepository connection,
            ILogger<TaxService> log)
        {
            _taxRepo = taxRepo;
            _connection = connection;
            _log = log;
        }

        private static string Normalize(string? s)
            => (s ?? string.Empty).Trim().Replace(" ", string.Empty).Replace(".", string.Empty);

        public async Task<TaxFullInfoDto?> GetFullInfoByMstAsync(string mst, int loaiCap = 0)
        {
            mst = Normalize(mst);
            if (string.IsNullOrEmpty(mst)) return null;

            var econ = await _taxRepo.GetEContractInfoByMstAsync(mst, loaiCap);
            if (econ == null) return null;

            var taxcode = econ.CusTax ?? mst;
            var cccd = econ.CusCMND_ID ?? string.Empty;

            var cnEvat = _connection.GetCnServerByMST(taxcode, cccd, "EVAT");
            if (string.IsNullOrWhiteSpace(cnEvat))
                throw new InvalidOperationException($"Không xác định được server EVAT cho {taxcode}");

            var cmpn = await _taxRepo.GetEvatCmpnInfoAsync(cnEvat, taxcode, cccd);

            TaxContractRange? range = null;
            if (!string.IsNullOrEmpty(econ.InvcSign) && !string.IsNullOrEmpty(econ.InvcSample))
            {
                range = await _taxRepo.GetContractRangeAsync(taxcode, econ.InvcSign!, econ.InvcSample!);
            }

            var cnTvan = _connection.GetCnServerByMST(taxcode, cccd, "TVAN");
            bool isToKhai = await _taxRepo.CheckConfirmTokhaiAsync(cnTvan, taxcode, cccd);

            return new TaxFullInfoDto
            {
                CusTax = econ.CusTax,
                CusCMND_ID = econ.CusCMND_ID,
                OID = econ.OID,
                CusPeople_Sign = econ.CusPeople_Sign,
                CusEmail = econ.CusEmail ?? cmpn?.Email,
                CusTel = econ.CusTel ?? cmpn?.Tel,
                SName = cmpn?.SName,
                Address = cmpn?.Address,
                IsToKhai = isToKhai,
                CusWebsite = econ.CusWebsite,
                CusBankNumber = econ.CusBankNumber,
                CusBankAddress = econ.CusBankAddress,
                ContractRange = range,
            };
        }

        public async Task<IEnumerable<ContractSummaryRow>> GetOidListByMstAsync(string mst)
        {
            mst = Normalize(mst);
            if (string.IsNullOrEmpty(mst)) return new List<ContractSummaryRow>();

            return await _taxRepo.GetOidListByMstAsync(mst);
        }

        public async Task<TaxFullInfoByOidDto?> GetFullInfoByOidAsync(string oid)
        {
            if (string.IsNullOrWhiteSpace(oid)) return null;

            var econ = await _taxRepo.GetEContractInfoByOidAsync(oid);
            if (econ == null) return null;

            var taxcode = econ.CusTax ?? string.Empty;
            var cccd = econ.CusCMND_ID ?? string.Empty;

            var cnEvat = _connection.GetCnServerByMST(taxcode, cccd, "EVAT");
            if (string.IsNullOrWhiteSpace(cnEvat))
                throw new InvalidOperationException($"Không xác định được server EVAT cho {taxcode}");

            var cmpn = await _taxRepo.GetEvatCmpnInfoAsync(cnEvat, taxcode, cccd);
            var samples = await _taxRepo.GetSampleTT78Async(cnEvat, taxcode);
            var products = await _taxRepo.GetEContractDetailByOidAsync(econ.OID ?? oid);

            TaxContractRange? range = null;
            if (!string.IsNullOrEmpty(econ.InvcSign) && !string.IsNullOrEmpty(econ.InvcSample))
            {
                range = await _taxRepo.GetContractRangeAsync(taxcode, econ.InvcSign!, econ.InvcSample!);
            }

            var cnTvan = _connection.GetCnServerByMST(taxcode, cccd, "TVAN");
            bool isToKhai = await _taxRepo.CheckConfirmTokhaiAsync(cnTvan, taxcode, cccd);

            return new TaxFullInfoByOidDto
            {
                CusTax = econ.CusTax,
                CusCMND_ID = econ.CusCMND_ID,
                OID = econ.OID,
                CusEmail = econ.CusEmail ?? cmpn?.Email,
                CusTel = econ.CusTel ?? cmpn?.Tel,
                SName = cmpn?.SName,
                Address = cmpn?.Address,
                ContractRange = range,
                Samples = samples,
                Products = products,
                IsToKhai = isToKhai
            };
        }
    }
}
