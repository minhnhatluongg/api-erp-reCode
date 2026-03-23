using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class DeXuatCapTaiKhoanRequestDto
    {
        [Required(ErrorMessage = "OIDContract không được để trống.")]
        public string OIDContract { get; set; } = "";

        [JsonIgnore]
        public string CmpnID { get; set; } = "26";
        [JsonIgnore]
        public string CrtUser { get; set; } = "";
        [JsonIgnore]
        public string MailAcc { get; set; } = "ketoan.hoadonso@gmail.com";
    }
}
