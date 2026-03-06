using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractDetailsViewModel
    {
        public EContractDTO? EContracts { get; set; }
        public List<JobDetailDTO> JobDetail { get; set; } = new List<JobDetailDTO>();
        public List<EContractDetailDTO> EContractDetails { get; set; } = new List<EContractDetailDTO>();
        public VendorDTO? Vendor { get; set; }
        public CustomerTaxCodeDTO? CustomerTaxCode { get; set; }

        // Các Flag điều khiển 
        public bool IsshowJob { get; set; } = true;
        public bool IsshowJobKT { get; set; } = true;
        public bool IsshowEntry { get; set; } = true;
        public bool IsshowEntryCS { get; set; } = true;
        public bool IsshowCSKT { get; set; } = false;
        public bool IsshowCS { get; set; } = true;
        public bool IsshowYC { get; set; } = true;
        public int Mode { get; set; } = 1;

        public JobDTO? Job { get; set; }
    }
}
