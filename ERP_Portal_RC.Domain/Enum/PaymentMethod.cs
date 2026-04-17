using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Enum
{
    public enum PaymentMethod
    {
        /// <summary>Tiền mặt</summary>
        CASH = 1,

        /// <summary>Chuyển khoản ngân hàng</summary>
        BANK = 2,

        /// <summary>Ví điện tử (Momo, ZaloPay, VNPay...)</summary>
        EWALLET = 3
    }
}
