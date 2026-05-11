using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Entities
{
	public class RptCKSPeriodPagedResponse
	{
		public List<RptCKSPeriodDTO> Data { get; set; } = new();
		public int TotalCount { get; set; }
	}
}
