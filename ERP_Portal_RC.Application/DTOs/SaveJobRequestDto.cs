using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class SaveJobRequestDto
    {
        public string? EContractOid { get; set; } 
        public string? JobOid { get; set; }
        public string? EntryName { get; set; }
        // Thông tin nghiệp vụ từ Form
        [JsonIgnore]
        public string? EmplName { get; set; }     
        public string? Description { get; set; }  

        // Các Object chứa dữ liệu chi tiết
        public JobInputDto Job { get; set; }       
        public List<JobPackInputDto> Packs { get; set; } 
        public List<EContractDetailItemDto>? Details { get; set; } 
    }
}
