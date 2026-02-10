using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface ITechnicalUserRepository
    {
        Task<TechnicalUser> GetByUserNameAsync(string username);
        Task AddTechnicalUserAsync(TechnicalUser user);
        Task<RegistertrationCodes> GetValidCodeAsync(string code);
        Task AddRegistrationCodeAsync(RegistertrationCodes code);
        Task UpdateRegistrationCodeAsync ( RegistertrationCodes code);
        Task SaveChangesAsync();
    }
}
