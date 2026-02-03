using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class DigitalSignaturesDashboardDto
    {
        public ApplicationUser? ApplicationUser { get; set; }
        public List<bosMenuRight>? bosMenuRight { get; set; }
        public int mode { get; set; }
        public DashboardStatsDto cks { get; set; } = new();
        public DashboardStatsDto dashboard { get; set; } = new();
    }
}
