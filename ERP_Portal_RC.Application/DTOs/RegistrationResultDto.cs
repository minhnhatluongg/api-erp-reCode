using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class RegistrationResultDto
    {
        public string NewEmployeeID { get; set; }
        public string? NewUserCode { get; set; }
        /// <summary>
        /// LoginName thực tế đã dùng — có thể khác LoginName FE truyền nếu trùng.
        /// VD: FE truyền "demoacc" đã tồn tại → API tự sinh "demoacc_1".
        /// </summary>
        public string? LoginNameUsed { get; set; }
        public string? ExternalApiWarning { get; set; }
    }
}
