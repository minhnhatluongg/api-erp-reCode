using System.Text.Json.Serialization;

namespace ERP_Portal_RC.Domain.Entities
{
	public class RptCKSPeriodDTO: CompanyContactDTO
	{
		[JsonPropertyName("certSubjectName")]
		public string? CKS_SubjectName	 { get; set; }

		[JsonPropertyName("certSerialNumber")]
		public string? CKS_SerialNumber { get; set; }

		[JsonPropertyName("certNotAfterDate")]
		public DateTime? CKS_NotAfterDate { get; set; }
	}
}
 
