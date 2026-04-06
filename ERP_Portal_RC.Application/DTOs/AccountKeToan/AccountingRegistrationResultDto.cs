using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.AccountKeToan
{
    public class AccountingRegistrationResultDto
    {
        public string UserCode { get; set; } = string.Empty;
        public string LoginName { get; set; } = string.Empty;
        public string GrpCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
