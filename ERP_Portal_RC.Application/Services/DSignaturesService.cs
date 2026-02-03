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
    public class DSignaturesService : IDSignaturesService
    {
        private readonly IDSignaturesRepository _dSignaturesRepository;

        public DSignaturesService(IDSignaturesRepository dSignaturesRepository)
        {
            _dSignaturesRepository = dSignaturesRepository;
        }
        public async Task<DigitalSignaturesDashboardDto> GetCountDigitalSignaturesAsync(string loginName, string userCode, string groupList, bool isManager)
        {
            try
            {
                var cleanGroupList = groupList?.Replace("'", string.Empty) ?? string.Empty;
                // 2. Xác định Mode và CrtUser
                var resultModel = await _dSignaturesRepository.GetDSMenuByID(loginName, cleanGroupList);
                string crtUserQuery = (resultModel.mode == 1) ? userCode : "UserMasterCode";

                // 3. Thiết lập thời gian (Đầu tháng trước đến ngày mai)
                var now = DateTime.Now;
                string dateFrom = now.Month == 1
                        ? $"{now.Year}-01-01"
                        : $"{now.Year}-{now.AddMonths(-1).Month:D2}-01";
                string dateTo = now.AddDays(1).ToString("yyyy-MM-dd");

                // 4. Lấy dữ liệu từ Repo
                var resultCKS = await _dSignaturesRepository.CountCKS("%", crtUserQuery, dateFrom, dateTo);
                var modelList = resultCKS?.digital_Moniter?.ToList();

                // 5. Tính toán Dashboard CKS (Thống kê trạng thái)
                var dsCks = new DashboardStatsDto
                {
                    countAll = modelList.Count(s => s.crt_User == userCode),
                    count0 = modelList.Count(s => s.crt_User == userCode && (s.currSignNumbJob == 0 || s.currSignNumbJob == 100 || s.currSignNumbJob == 200)),
                    count101 = modelList.Count(s => s.crt_User == userCode && s.currSignNumbJob == 101),
                    count301 = modelList.Count(s => s.crt_User == userCode && s.currSignNumbJob == 301),
                    count201 = modelList.Count(s => s.crt_User == userCode && s.currSignNumbJob == 201),
                    countBack = modelList.Count(s => s.crt_User == userCode && (s.currSignNumbJob == 100 || s.currSignNumbJob == 200 || s.currSignNumbJob == 300 || s.currSignNumbJob == 500)),
                    countCancelNotice = modelList.Count(s => !string.IsNullOrEmpty(s.TT2) && s.crt_User == userCode),

                    // Thống kê toàn bộ (All CKS)
                    countAllCKS = modelList.Count,
                    count0CKS = modelList.Count(s => s.currSignNumbJob == 0 || s.currSignNumbJob == 100 || s.currSignNumbJob == 200),
                    count101CKS = modelList.Count(s => s.currSignNumbJob == 101),
                    count301CKS = modelList.Count(s => s.currSignNumbJob == 301),
                    count201CKS = modelList.Count(s => s.currSignNumbJob == 201),
                    countCancelNoticeCKS = modelList.Count(s => !string.IsNullOrEmpty(s.TT2) || s.currSignNumbJobCancel != 0)
                };

                // 6. Tính toán Dashboard (Thống kê thời gian Ngày/Tháng/Năm)
                var dsdashboard = new DashboardStatsDto();
                var scopeList = isManager ? modelList : modelList.Where(s => s.crt_User == userCode).ToList();

                dsdashboard.countAllDay = scopeList.Count(s => s.Crt_Date.Date == now.Date);
                dsdashboard.count0Day = scopeList.Count(s => s.Crt_Date.Date == now.Date && (s.currSignNumbJob == 0 || s.currSignNumbJob == 100 || s.currSignNumbJob == 200));

                dsdashboard.countAllMonth = scopeList.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year);
                dsdashboard.count0Month = scopeList.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year && (s.currSignNumbJob == 0 || s.currSignNumbJob == 100 || s.currSignNumbJob == 200));
                dsdashboard.count101Month = scopeList.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year && s.currSignNumbJob == 101);
                dsdashboard.count201Month = scopeList.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year && s.currSignNumbJob == 201);
                dsdashboard.count301Month = scopeList.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year && s.currSignNumbJob == 301);

                dsdashboard.countAllYear = scopeList.Count(s => s.Crt_Date.Year == now.Year && s.ODate >= now.AddDays(-29));

                return new DigitalSignaturesDashboardDto
                {
                    cks = dsCks,
                    dashboard = dsdashboard,
                    mode = resultModel.mode
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi Service: {ex.Message}");
            }
        }
    }
}
