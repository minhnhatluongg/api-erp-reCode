using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
	public class CompanyContactDTO
	{
		public string? Taxnumber { get; set; }
		public string? MerchantName { get; set; }
		public string? Tel1 { get; set; }
		public string? Tel2 { get; set; }
		public string? Tel3 { get; set; }
		public string? Email1 { get; set; }
		public string? Email2 { get; set; }
		public string? Email3 { get; set; }
		public string? ServerKey { get; set; }
		public string? SaleID { get; set; }
		public string? SaleEmail { get; set; }
		public string? SaleFullName { get; set; }
		public string? SaleDName { get; set; }
		public string? SaleLoginName { get; set; }
	}
}
