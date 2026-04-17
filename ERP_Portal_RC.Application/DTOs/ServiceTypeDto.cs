using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class ServiceTypeDto
    {
        public int ServiceTypeID { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? Crt_User { get; set; }
        public DateTime Crt_Date { get; set; }
        public string? ChgeUser { get; set; }
        public DateTime? ChgeDate { get; set; }
    }

    public class CreateServiceTypeDto
    {
        [Required(ErrorMessage = "Code không được để trống")]
        [MaxLength(50, ErrorMessage = "Code tối đa 50 ký tự")]
        public string Code { get; set; } = default!;

        [Required(ErrorMessage = "Tên loại dịch vụ không được để trống")]
        [MaxLength(200, ErrorMessage = "Tên tối đa 200 ký tự")]
        public string Name { get; set; } = default!;

        [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string? Description { get; set; }
    }

    public class UpdateServiceTypeDto
    {
        [Required(ErrorMessage = "Code không được để trống")]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required(ErrorMessage = "Tên loại dịch vụ không được để trống")]
        [MaxLength(200)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
