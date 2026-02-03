using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class DSignaturesService /*: IDSignaturesService*/
    {
        private readonly IDSignaturesRepository _dSignaturesRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DSignaturesService(IDSignaturesRepository dSignaturesRepository, UserManager<ApplicationUser> userManager)
        {
            _dSignaturesRepository = dSignaturesRepository;
            _userManager = userManager;
        }
        //public async Task<DigitalSignaturesViewModel> GetCountDigitalSignaturesAsync(string userName, bool isManager)
        //{
        //    try
        //    {
        //        var user = await _userManager.FindByNameAsync(userName);
        //        if (user == null)
        //        {
        //            throw new Exception("User not found.");
        //        }
        //        var grouplst = user.Grp_List?.Replace("'", string.Empty) ?? string.Empty;

        //        var resultModel = await _dSignaturesRepository.GetDSMenuByID(user.UserName, grouplst);

        //        string crtuser = (resultModel.mode == 1) ? user.UserCode : "UserMasterCode";

        //        string dateFrom = DateTime.Now.Month == 1
        //                ? $"{DateTime.Now.Year}-01-01"
        //                : $"{DateTime.Now.Year}-{DateTime.Now.AddMonths(-1).Month:D2}-01";
        //        string dateTo = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

        //        var resultCKS = await _dSignaturesRepository.CountCKS("%", crtuser, dateFrom, dateTo);
        //        var modelList = resultCKS.digital_Moniter.ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }
        //}
    }
}
