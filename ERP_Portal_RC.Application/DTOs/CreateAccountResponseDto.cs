using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CreateAccountResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";

        public bool WindowsAppOK { get; set; }

        public bool WebAppOK { get; set; }
        public bool CheckOK { get; set; }
        public bool DatabaseOK { get; set; }

        public string? WebAppError { get; set; }
        public string? ErrorDetail { get; set; }
    }
}
