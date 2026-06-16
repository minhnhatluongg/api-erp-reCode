using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class SaleRegistrationModel
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? ManagerEmplID { get; set; }

        [Required]
        public string SoCMND { get; set; }

        // PsID đã bỏ khỏi input — store wsp_RegisterSaleHierarchy tự mặc định '00084' (TDV/CTV).
        public string? Phone { get; set; }
        public bool IsCreateAccount { get; set; } 
        public string? LoginName { get; set; }
        public string? Password { get; set; }
        //public string? RegistrationCode { get; set; }    
    }
}
