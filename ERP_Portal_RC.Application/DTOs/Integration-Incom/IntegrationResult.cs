using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs.Integration_Incom
{
    public class IntegrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string MerchantId { get; set; } // ID khách hàng (cũ hoặc mới)
        public string OrderOID { get; set; }    // Mã đơn hàng vừa lưu
        public string Status { get; set; }      // "EXISTING_CUSTOMER" hoặc "NEW_CUSTOMER_CREATED"
        public string TaxCode { get; set; }      
        public string CustomerStatus { get; set; }      
    }
}
