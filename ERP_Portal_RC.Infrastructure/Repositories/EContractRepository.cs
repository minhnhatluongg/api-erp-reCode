using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static ERP_Portal_RC.Domain.Enum.PublicEnum;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class EContractRepository : IEContractRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDSignaturesRepository _dSign;
        private const string BosOnline = "BosOnline";
        public EContractRepository(IDbConnectionFactory dbConnectionFactory, IDSignaturesRepository dSign)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dSign = dSign;
        }

        public async Task<ListEcontractViewModel> CountList(string crtUser, string dateStart, string dateEnd)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new { CrtUser = crtUser, Frm_date = dateStart, End_date = dateEnd };

            var model = new ListEcontractViewModel();
            var result = await conn.QueryMultipleAsync("wspCount_List_EContracts", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 3600);

            model.lstMonitor = (await result.ReadAsync<EContract_Monitor>()).ToList();

            MapEContractStatus(model.lstMonitor, crtUser);
            return model;
        }

        public async Task CreateLog(string message, string userCode)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new { Message = message, UserCode = userCode, CrtDate = DateTime.Now };
            await conn.ExecuteAsync("INSERT INTO SystemLogs (Message, UserCode, CrtDate) VALUES (@Message, @UserCode, @CrtDate)", parameters);
        }

        public async Task<ListEcontractViewModel> GetAllList(string crtUser, string dateStart, string dateEnd)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new { CrtUser = crtUser, Frm_date = dateStart, End_date = dateEnd };

            var model = new ListEcontractViewModel();
            var result = await conn.QueryMultipleAsync("wspList_EContracts_All_V22", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 3600);

            model.lstMonitor = (await result.ReadAsync<EContract_Monitor>()).ToList();
            model.subEmpl = (await result.ReadAsync<SubEmpl>()).ToList();

            MapEContractStatus(model.lstMonitor, crtUser);
            return model;
        }

        public async Task<DSMenuViewModel> GetDSMenuByID(string loginName, string grpCode)
        {
            return await _dSign.GetDSMenuByID(loginName, grpCode);
        }

        public async 
            Task<ListEcontractViewModel> Search(string search, string crtUser, string dateStart, string dateEnd)
        {
            if (search == "CÔNG TY TNHH WIN TECH SOLUTION") search = "WIN TECH";
            if (search == "CÔNG TY TNHH WIN ONLINE MEDIA") search = "WIN ONLINE";

            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new DynamicParameters();
            parameters.Add("@strSearch", search);
            parameters.Add("@CrtUser", crtUser);
            parameters.Add("@Frm_date", dateStart);
            parameters.Add("@End_date", dateEnd);
            var model = new ListEcontractViewModel();
            var result = await conn.QueryMultipleAsync("wspList_EContracts_Search_test", parameters, commandType: CommandType.StoredProcedure);

            model.lstMonitor = (await result.ReadAsync<EContract_Monitor>()).ToList();
            model.subEmpl = (await result.ReadAsync<SubEmpl>()).ToList();

            MapEContractStatus(model.lstMonitor, crtUser);
            return model;

        }

        #region Helper
        private void MapEContractStatus(List<EContract_Monitor> list, string currentCrtUser)
        {
            var specialDate = new DateTime(2020, 07, 13);
            foreach (var item in list)
            {
                if (item.ODATE < specialDate && item.CmpnID == "26") item.SiteName = "MONET";
                item.IsDisiable = item.Crt_User != currentCrtUser;

                if (item.isContractPaper || item.isPLHD) item.TT3 = TTStatus.TT3_DACAP;
                if (item.isDesignInvoice) item.TT2 = TTStatus.TT2_THIETKE;

                if (!string.IsNullOrEmpty(item.XHD)) item.isCheckedShow = true;
                if (item.currSignNumbJobKT != 0) item.TT2 = item.TT6;

                bool isDefault = item.TT2 == "Chưa có yêu cầu tạo mẫu" &&
                                item.TT3 == TTStatus.TT3_CHUACAP &&
                                item.TT4 == TTStatus.TT4_CHUACAP;

                if (item.isTool && item.isTT78 && isDefault)
                {
                    item.TT2 = "Tạo mẫu thiết kế: Thực hiện";
                    item.TT3 = TTStatus.TT3_DACAP;
                    if (!item.isGiaHan) item.TT4 = TTStatus.TT4_DACAP;
                }

                item.ischeckTK = item.TT3 == TTStatus.TT3_DACAP || item.isGiaHan;
                item.ischeckPH = item.TT4 == TTStatus.TT4_DACAP;
                item.ischeckKNV = item.TT4 == TTStatus.TT4_KHOANV;

                if (item.ODATE != null) item.ODATE = (DateTime)item.ODATE;
            }
        }
        #endregion
    }
}
