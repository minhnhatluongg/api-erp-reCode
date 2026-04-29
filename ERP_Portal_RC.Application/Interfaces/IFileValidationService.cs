using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Interfaces
{
    public interface IFileValidationService
    {
        Task<(bool IsValid, string Error)> ValidateAsync(IFormFile file, CancellationToken ct = default);
    }
}
