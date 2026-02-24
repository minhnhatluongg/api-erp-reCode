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
        private readonly IMailService _mailService;

        public EcontractService(IEContractRepository econtractRepo, IMailService mailService)
        {
            _eContractRepository = econtractRepo;
            _mailService = mailService;
        }

        public async Task<EContractServiceResult> GetAllEContractsAsync(string userName, EContractFilterRequest request, string groupList, string userCode)
        {

            var menuTask = _eContractRepository.GetDSMenuByID(userName, groupList);
            var moneyTask = _eContractRepository.CheckBCTT("26", userCode, "0000003");

            await Task.WhenAll(menuTask, moneyTask);

            var CN = await moneyTask;
            var resultMenu = await menuTask;

            var dateFrom = !string.IsNullOrEmpty(request.FrmDate) ? request.FrmDate : "2010-01-01";
            var dateTo = !string.IsNullOrEmpty(request.ToDate) ? request.ToDate : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            string crtUser = (resultMenu?.mode == 1 || request.IsUser == "1") ? userCode : "%";

            ListEcontractViewModel result;
            if (!string.IsNullOrEmpty(request.EmplChild) && request.EmplChild != "null")
            {
                string strEmplChild = (string.IsNullOrEmpty(request.StrEmplChild) || request.StrEmplChild == "null")
                    ? userCode : request.StrEmplChild;

                result = await _eContractRepository.GetEContractsByHierarchyAsync(request.EmplChild, strEmplChild, dateFrom, dateTo, userCode);
            }
            else if (!string.IsNullOrEmpty(request.CusTName))
            {
                result = await _eContractRepository.Search(request.CusTName, crtUser, dateFrom, dateTo);
            }
            else
            {
                result = await _eContractRepository.GetAllList(crtUser, dateFrom, dateTo);
            }

            if (result?.lstMonitor == null) return new EContractServiceResult { Total = 0, Data = new List<EContract_Monitor>() };

            var filteredQuery = result.lstMonitor.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Status) && int.TryParse(request.Status, out int statusInt))
            {
                filteredQuery = filteredQuery.Where(x => x.CurrSignNumb == statusInt);
            }

            // 5. Phân trang (Pagination)
            int totalRecords = filteredQuery.Count();
            int pageSize = request.PageSize;
            int page = request.Page;
            int offset = (page - 1) * pageSize;

            var pagedList = (pageSize == -1)
                ? filteredQuery.ToList()
                : filteredQuery.Skip(offset).Take(pageSize).ToList();

            if (pagedList.Any())
            {
                var currentPageOids = pagedList.Select(x => x.OID).ToList();
                var oidsWithDetails = await _eContractRepository.GetListOIDHasDetails(currentPageOids);
                var oidsWithDetailsSet = new HashSet<string>(oidsWithDetails ?? new List<string>());

                foreach (var item in pagedList)
                {
                    // Đảm bảo ngày tháng hợp lệ
                    if (item.ODATE == DateTime.MinValue) item.ODATE = DateTime.Now;

                    // Quyền Disable dựa trên người tạo
                    item.IsDisiable = (item.Crt_User != userCode);

                    // Mapping trạng thái TT2 từ kế toán (TT6) nếu có
                    if (item.currSignNumbJobKT != 0) item.TT2 = item.TT6;

                    // Kiểm tra xem có chi tiết đính kèm không
                    item.isCheckedShow = oidsWithDetailsSet.Contains(item.OID);
                }
            }

            return new EContractServiceResult
            {
                MoneyToBePaid = (CN != null && CN.PTHU != 0.000m) ? FormatCurrency(CN.PTHU) : "0",
                MoneyPaid = (CN != null && CN.DABTT != 0.000m) ? FormatCurrency(CN.DABTT) : "0",
                Data = pagedList,
                Total = totalRecords,
                Disable = result.IsDisiable
            };
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

        // 1. Trình ký hợp đồng: 0 -> 101 (Sử dụng store webContracts)
        public async Task<(bool success, bool emailSent)> ProposeSignContractAsync(ApprovalWorkflowRequest model, string userId, string saleFullName)
        {
            var config = ("EContract", "EC:001", 101, "zsgn_webContracts_NOR");
            var result = await _eContractRepository.ExecuteApprovalWorkflow(model, config, userId);
            var success = result == 1;

            if (!success) return (false, false);

            bool emailSent = false;
            try
            {
                var (cusTax, cusName) = await _eContractRepository.GetContractInfoForEmailAsync(model.OID);
                await _mailService.SendProposeSignNotificationAsync(
                    oid: model.OID,
                    cusTax: cusTax,
                    cusName: cusName,
                    saleFullName: saleFullName, 
                    ktName: string.Empty
                );
                emailSent = true;
            }
            catch
            {
                emailSent = false;
            }

            return (success, emailSent);
        }

        // 2. Đề xuất tạo mẫu (JB:005/JOB_00001 hoặc JB:004/JOB_00002):
        //    Tạo Job record + sign 0 -> 101 qua Ins_EContractJobs_RequestByOdoo
        public async Task<(bool success, string message)> ProposeTemplateAsync(
            ERP_Portal_RC.Domain.Entities.EContractJobRequest request, string userId)
        {
            return await _eContractRepository.CreateEContractJobAsync(request, userId);
        }


        // 3. Phát hành mẫu (JB:004/JOB_00002): Tìm Job OID -> nâng 101 -> 201
        public async Task<(bool success, string message)> IssueInvoiceAsync(ApprovalWorkflowRequest model, string userId)
        {
            return await _eContractRepository.AdvanceEContractJobSigningAsync(
                contractOid:  model.OID,
                factorId:     "JOB_00002",
                entryId:      "JB:004",
                userId:       userId,
                fromSignNumb: 101,
                toSignNumb:   201,
                appvMess:     model.AppvMess
            );
        }
        public async Task<Template?> GetTemplateByCodeAsync(string factorId)
        {
            return await _eContractRepository.GetTemplateByCodeAsync(factorId);
        }

        public async Task<Template?> GetOriginalContractAsync()
        {
            return await _eContractRepository.GetTemplateByCodeAsync("TT78_EContract");
        }

        public async Task<Template?> GetCompensationContractAsync()
        {
            return await _eContractRepository.GetTemplateByCodeAsync("TT78_EContractExt");
        }

        public async Task<Template?> GetExtensionContractAsync()
        {
            return await _eContractRepository.GetTemplateByCodeAsync("TT78_EContractExt1");
        }

        #region Helper
        public string FormatCurrency(decimal? number)
        {
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("vi-VN");
            string a = string.Format(culture, "{0:N0}", number);
            return a;
        }
        #endregion

    }
}


