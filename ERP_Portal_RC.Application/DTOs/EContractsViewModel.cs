using ERP_Portal_RC.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractsViewModel
    {
        public bool IsExtensionNoSample { get; set; }
        public string? picked { get; set; }
        public string? OID { get; set; }
        public bool ischeck { get; set; }
        public string? invcSample { get; set; }
        public string? DescriptChange { get; set; }
        public string? InvcSign { get; set; }
        public string? InvcFrm { get; set; }
        public string? InvcEnd { get; set; }
        public string? Sum_Amnt { get; set; }
        public bool Isshow { get; set; } = false;
        public bool IsshowJob { get; set; } = true;
        public bool IsshowJobKT { get; set; } = true;
        public bool IsshowYCTM { get; set; } = true;
        public bool IsshowEntry { get; set; } = true;
        public bool IsshowEntryCS { get; set; } = true;
        public bool IsshowCSKT { get; set; } = false;
        public bool IsshowCS { get; set; } = true;
        public bool IsshowYC { get; set; } = true;
        public bool IsshowReturnSign { get; set; }
        public bool ycTK { get; set; } = false;
        public bool ycDaTK { get; set; } = false;
        public bool ycTM { get; set; } = false;
        public bool ycDaTM { get; set; } = false;
        public bool ycPH { get; set; } = false;
        public string? ChiCuc { get; set; }
        public Int32 Mode { get; set; }
        public string? cmpnIDUser { get; set; }
        public List<EContractDTO>? lstEContracts { get; set; }
        public List<HistoryList>? HistoryList { get; set; }
        public EContractDTO? EContracts { get; set; }
        public Right_EContracts? Right_EContracts { get; set; }
        public List<JobDetailDTO>? JobDetail { get; set; }
        public List<JobDetailDTO>? JobDetailTM { get; set; }
        public List<JobDetailDTO>? JobDetailTT78 { get; set; }
        public List<JobDetailDTO>? JobDetailKT { get; set; }
        public List<Signature>? Signature { get; set; }
        public List<JobPost>? JobDetailFac { get; set; }
        public List<JobPost>? JobDetailFacKT { get; set; }
        public List<JobPackDto>? JobPack { get; set; }
        public JobDetailDTO? JobDetailDec { get; set; }
        public JobDetailDTO? JobDetailDecYC { get; set; }
        public List<JobPost>? JobPost { get; set; }
        public List<JobPost>? JobPostJob { get; set; }
        public List<JobPost>? JobPostJobKT { get; set; }
        public List<ListFile>? ListFileSelect { get; set; }
        public IFormCollection? UploadedFiles { get; set; }
        public JobDTO? Job { get; set; }
        public VendorDTO? Vendor { get; set; }
        public templateEcontract? TemplateEcontract { get; set; }
        public CustomerTaxCodeDTO? CustomerTaxCode { get; set; }
        public FileUploadViewModel? FileUpload { get; set; }
        public List<EContractDetails>? EContractDetails { get; set; }
        public EContractDetails? EContractDetailsByUnitName { get; set; }
        public List<ListFile>? ListFiles { get; set; }
        public List<EContractDetails>? EContractDetailsXML { get; set; }
        public Template? Template { get; set; }
        public ECtr_PublicInfo? ECtr_PublicInfo { get; set; }
        public string? SaleEmail { get; set; }
        public EmailUser? EmailUser { get; set; }
        public List<EmailUser>? lstEmailUser { get; set; }
        public string? UrlRequest { get; set; }
        public Nofication Nofication { get; set; }
        public EApprove? approve { get; set; }
        public string? isauto { get; set; }
        public List<JobDTO>? lstJob { get; set; }
        public bool isCheckedShow { get; set; } = false;
    }
}
