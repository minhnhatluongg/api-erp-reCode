using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class TvanRenewalQueryDto
    {
        [Range(1, 365)]
        public int DaysBeforeExpiry { get; set; } = 30;

        public bool IncludeExpired { get; set; } = false;

        public string? Mst { get; set; }
        public string? SaleCode { get; set; }
        public string? Keyword { get; set; }

        public string? RangeKey { get; set; }


        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 200)]
        public int Size { get; set; } = 20;
    }
}
