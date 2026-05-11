using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
	public class RptPageRequest
	{
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 20;
		public string? SearchKeyword { get; set; }
	}
}
