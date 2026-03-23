using ERP_Portal_RC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface ISignHSMService 
    {
        /// <summary>
        /// Flow:
        ///   1. Lấy PayloadJson từ EVAT_AppSign_Process
        ///   2. Parse payload → map sang SignHSMEntity (qua factory)
        ///   3. UpdateProcessStatus → 1 (đang lưu)
        ///   4. SaveSignedXmlAsync (gọi SP)
        ///   5. UpdateProcessStatus → 2 / -1
        /// </summary>
        Task<ApiResponse<SaveSignedXmlResponseDto>> SaveSignedXmlAsync(
            SaveSignedXmlRequestDto request);
    }
}
