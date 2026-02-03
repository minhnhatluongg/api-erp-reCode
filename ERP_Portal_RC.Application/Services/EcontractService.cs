using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ERP_Portal_RC.Domain.Enum.PublicEnum;

namespace ERP_Portal_RC.Application.Services
{
    public class EcontractService : IEcontractService
    {
        private readonly IEContractRepository _eContractRepository;
        public EcontractService(IEContractRepository econtractRepo)
        {
            _eContractRepository = econtractRepo;
        }
        public async Task<DashboardStatsDto> GetContractDashboardAsync(string userCode, string userName, string grpList, bool isManager, ContractSearchRequest request)
        {
            var menuInfo = await _eContractRepository.GetDSMenuByID(userName, grpList);
            string crtUserQuery = (menuInfo.mode == 1) ? userCode : UserMaster.UserCode;

            // Lấy dữ liệu (Mặc định lấy từ đầu năm để có dữ liệu Year/Month)
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1).ToString("yyyy-MM-dd");
            var endOfToday = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var result = await _eContractRepository.CountList(crtUserQuery, startOfYear, endOfToday);
            var list = result.lstMonitor ?? new List<EContract_Monitor>();
            var now = DateTime.Now;

            var ds = new DashboardStatsDto
            {
                // 1. Thống kê tổng quát (Toàn bộ hệ thống hoặc theo phân quyền Master)
                countAll = list.Count,
                count0 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.TRINH_KY),
                count101 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.CHO_KIEM_TRA),
                count201 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.CHO_GD_DUYEN),
                count301 = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.HD_DA_DUYET),
                countKHSign = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.KH_DA_KY),
                countClose = list.Count(s => s.CurrSignNumb == (int)CurrSignNum.HD_DONG),
                countPH = list.Count(s => s.TT4 == TTStatus.TT4_DACAP), // Phát hành hóa đơn

                // 2. Thống kê theo thời gian (Dòng dữ liệu bị thiếu của bạn)
                countAllDay = list.Count(s => s.Crt_Date.Date == now.Date),
                countAllMonth = list.Count(s => s.Crt_Date.Month == now.Month && s.Crt_Date.Year == now.Year),
                countAllYear = list.Count(s => s.Crt_Date.Year == now.Year),

                // 3. Thống kê theo User cụ thể (Sale xem dashboard cá nhân)
                countAlluser = list.Count(s => s.Crt_User == userCode),
                count0user = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.TRINH_KY),
                count101user = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.CHO_KIEM_TRA),
                count301user = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.HD_DA_DUYET),
                countKHSignuser = list.Count(s => s.Crt_User == userCode && s.CurrSignNumb == (int)CurrSignNum.KH_DA_KY),
                countPHuser = list.Count(s => s.Crt_User == userCode && s.TT4 == TTStatus.TT4_DACAP),
            };

            // Đừng quên gán các trường Sum nếu UI cần hiển thị
            ds.countAllSum = ds.countAll;
            ds.sumDT = list.Where(s => s.CurrSignNumb == (int)CurrSignNum.HD_DA_DUYET).Sum(s => s.ItemPrice);

            return ds;
        }

        public async Task<ListEcontractViewModel> GetContractListAsync(string userCode, string userName, string grpList, bool isManager, ContractSearchRequest request)
        {
            var menuInfo = await _eContractRepository.GetDSMenuByID(userName, grpList);
            string crtUserQuery = (menuInfo.mode == 1) ? userCode : UserMaster.UserCode;

            ListEcontractViewModel result;
            string dateFrom = request.FrmDate ?? "2019-01-01";
            string dateTo = request.ToDate ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            string searchKeyword = request.CusTName ?? request.CusTTax ?? request.NCC ?? request.Kinhdoanh;

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                result = await _eContractRepository.Search(searchKeyword, crtUserQuery, dateFrom, dateTo);
            }
            else
            {
                result = await _eContractRepository.GetAllList(crtUserQuery, dateFrom, dateTo);
            }

            if (!string.IsNullOrEmpty(request.Status) && result.lstMonitor != null)
            {
                int statusInt = int.Parse(request.Status);
                result.lstMonitor = result.lstMonitor.Where(x => x.CurrSignNumb == statusInt).ToList();
            }

            return result;
        }
    }
}
