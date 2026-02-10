using ERP_Portal_RC.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IRegistrationCodeService
    {
        //register and login 
        Task<bool> RegisterAsync(TechnicalRegistrationRequest request);
        Task<LoginResponseDto?> LoginAsync(TechnicalLoginRequest request);

        /// <summary>
        /// Tạo và lưu mã đăng ký mới
        /// </summary>
        /// <param name="techUserId"></param>
        /// <returns></returns>
        Task<string> GenerateAndSaveCodeAsync(int techUserId);

        Task<bool> ValidateCodeAsync(string code);

        Task<bool> ValidateAndUseCodeAsync(string code, string email);

    }
}
