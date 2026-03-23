using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ISignHSMRepository
    {
        /// <summary>
        /// Lấy PayloadDataJson từ BosEVAT.dbo.EVAT_AppSign_Process theo OID.
        /// Service dùng để parse ODate, PartnerVAT, CompanyInfo → build Entity.
        /// </summary>
        Task<string> GetPayloadJsonAsync(string oid);

        /// <summary>
        /// Gọi [BosControlEVAT].[dbo].[Ins_ContractContent_SignedByOdoo_origin].
        /// Nhận Entity — không biết DTO tồn tại.
        /// </summary>
        Task<SignHSMResult> SaveSignedXmlAsync(SignHSMEntity entity);

        /// <summary>
        /// Cập nhật trạng thái EVAT_AppSign_Process.
        /// status: 1=Processing, 2=Success, -1=Error
        /// </summary>
        Task UpdateProcessStatusAsync(string oid, int status, string message);
    }
}
