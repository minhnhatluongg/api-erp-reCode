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
        private const string BosApproval = "BosApproval";
        private const string BosControlEVAT = "BosControlEVAT";
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

            using var result = await conn.QueryMultipleAsync(
                "wspList_EContracts_All_V22",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 3600);

            {
                var monitorData = await result.ReadAsync<EContract_Monitor>();
                model.lstMonitor = monitorData.ToList();
            }

            if (!result.IsConsumed)
            {
                var subEmplData = await result.ReadAsync<SubEmpl>();
                model.subEmpl = subEmplData.ToList();
            }

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

        public async Task<limitGHCNKD> CheckBCTT(string cmpnID, string saleID, string group)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new DynamicParameters();
            parameters.Add("@CmpnID", cmpnID);
            parameters.Add("@SaleID", saleID);
            parameters.Add("@GroupItem", group);
            
            return await connection.QueryFirstOrDefaultAsync<limitGHCNKD>(
                "GetListBillDebtReceipt", parameters, commandType: CommandType.StoredProcedure);

        }



        public async Task<ListEcontractViewModel> GetEContractsByHierarchyAsync(
            string search, 
            string emplChild, 
            string dateStart, 
            string dateEnd, 
            string currentUserCode)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);

            var finalResult = new ListEcontractViewModel { lstMonitor = new List<EContract_Monitor>() };

            var parameters1 = new DynamicParameters();
            parameters1.Add("@CrtUser", search);
            parameters1.Add("@LeaderCode", currentUserCode);
            parameters1.Add("@Frm_date", dateStart);
            parameters1.Add("@End_date", dateEnd);

            var multi = await connection.QueryMultipleAsync("wspList_EContracts_All_byLeader", parameters1, commandType: CommandType.StoredProcedure);

            var rootMonitor = (await multi.ReadAsync<EContract_Monitor>()).ToList();
            var subEmployees = (await multi.ReadAsync<SubEmpl>()).Where(s => s.EmployeeID != currentUserCode).ToList();


            // CASE 1: Xem tất cả cấp dưới trực tiếp
            if (emplChild == currentUserCode && search == currentUserCode)
            {
                foreach (var sub in subEmployees)
                {
                    var matched = rootMonitor.Where(s => s.Crt_User == sub.EmployeeID);
                    finalResult.lstMonitor.AddRange(matched);
                }
            }
            // CASE 2: Xem cấp dưới của một nhân viên cụ thể (Cấp cháu)
            else if (emplChild == currentUserCode && search != currentUserCode)
            {
                var targetSubs = subEmployees.Where(s => s.EmployeeID == search).ToList();
                foreach (var sub in targetSubs)
                {
                    var p = new DynamicParameters();
                    p.Add("@CrtUser", sub.EmployeeID);
                    p.Add("@Frm_date", dateStart);
                    p.Add("@End_date", dateEnd);

                    var resEmp = await connection.QueryMultipleAsync("wspList_SubEmpl", p, commandType: CommandType.StoredProcedure);
                    var subOfSub = await resEmp.ReadAsync<SubEmpl>();

                    foreach (var item in subOfSub)
                    {
                        var matched = rootMonitor.Where(s => s.Crt_User == item.EmployeeID);
                        finalResult.lstMonitor.AddRange(matched);
                    }
                }
            }
            // CASE 3: Lọc đích danh hoặc lọc sâu (Deep Nesting)
            else if (search == currentUserCode && emplChild != currentUserCode)
            {
                var filteredSubs = subEmployees.Where(s => s.EmployeeID != search && s.EmployeeID != emplChild).ToList();
                foreach (var sub in filteredSubs)
                {
                    var p = new DynamicParameters();
                    p.Add("@strSearch", sub.EmployeeID);
                    p.Add("@CrtUser", sub.EmployeeID);
                    p.Add("@Frm_date", dateStart);
                    p.Add("@End_date", dateEnd);

                    var resDeep = await connection.QueryMultipleAsync("wspList_EContracts_Select_CB", p, commandType: CommandType.StoredProcedure);
                    finalResult.lstMonitor.AddRange(await resDeep.ReadAsync<EContract_Monitor>());
                }
            }
            // CASE 4: Mặc định lấy theo EmplChild cụ thể
            else
            {
                var p = new DynamicParameters();
                p.Add("@strSearch", emplChild);
                p.Add("@CrtUser", emplChild);
                p.Add("@Frm_date", dateStart);
                p.Add("@End_date", dateEnd);

                var resLast = await connection.QueryMultipleAsync("wspList_EContracts_Select_CB", p, commandType: CommandType.StoredProcedure);
                finalResult.lstMonitor = (await resLast.ReadAsync<EContract_Monitor>()).ToList();
            }

            return finalResult;
        }
        public async Task<List<string>> GetListOIDHasDetails(List<string> oids)
        {
            if (oids == null || !oids.Any())
                return new List<string>();

            const int batchSize = 2000;
            var allResults = new List<string>();

            using var connection = _dbConnectionFactory.GetConnection(BosApproval);

            string sQuery = @"
                    SELECT DISTINCT OID 
                    FROM dbo.zsgn_webContracts 
                    WHERE OID IN @oids";

            var chunks = oids.Chunk(batchSize);

            foreach (var batch in chunks)
            {
                var result = await connection.QueryAsync<string>(
                    sQuery,
                    new { oids = batch }
                );

                if (result != null)
                {
                    allResults.AddRange(result);
                }
            }
            return allResults.Distinct().ToList();
        }

        public async Task<int> ExecuteApprovalWorkflow(ApprovalWorkflowRequest model, (string Factor, string Entry, int NextStep, string Sp) config, string userId)
        {
            // Sử dụng dapper và connection factory
            using var conn = _dbConnectionFactory.GetConnection("BosApproval");
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Enrich Data: Lấy thông tin MST và SampleID từ DB nếu Model truyền lên bị trống
                // Điều này giúp Frontend chỉ cần gửi OID là đủ chạy nghiệp vụ
                var queryInfo = @"SELECT CmpnID, CusTax, CmpnTax, SampleID 
                          FROM BosOnline.dbo.EContracts WHERE OID = @OID";
                var contractInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(queryInfo, new { OID = model.OID }, trans);

                var p = new DynamicParameters();
                p.Add("@FactorID", config.Factor);
                p.Add("@EntryID", config.Entry);
                p.Add("@OID", model.OID);
                p.Add("@ODate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                p.Add("@CmpnID", (string?)contractInfo?.CmpnID ?? "26");
                p.Add("@Crt_User", userId);
                p.Add("@nextSignNumb", config.NextStep);
                p.Add("@holdSignNumb", config.NextStep == 201 ? 101 : 0);
                p.Add("@Variant30", "1"); // '1' = SP dùng GETDATE() làm ngày ký

                // Phân nhánh Variant dựa trên nghiệp vụ Job hay EContract
                if (config.Factor.StartsWith("JOB"))
                {
                    p.Add("@DataTbl", "EContractJobs");
                    p.Add("@SignTble", "zsgn_EContractJobs");
                    // SampleID: ưu tiên FE gửi lên, fallback lấy từ DB
                    p.Add("@Variant29", !string.IsNullOrEmpty(model.SampleID) ? model.SampleID : (string?)contractInfo?.SampleID);
                }
                else
                {
                    p.Add("@SignTble", "zsgn_EContracts");
                    // CusTax, CmpnTax, SampleID đều lấy từ DB (enrich)
                    p.Add("@Variant27", (string?)contractInfo?.CusTax);
                    p.Add("@Variant28", (string?)contractInfo?.CmpnTax ?? "0312303803");
                    p.Add("@Variant29", (string?)contractInfo?.SampleID);
                }

                p.Add("@AppvMess", !string.IsNullOrEmpty(model.AppvMess) ? model.AppvMess : "Trình ký từ hệ thống Portal");

                // 2. Thực thi Store Procedure Core
                var result = await conn.QuerySingleAsync<dynamic>(config.Sp, p, transaction: trans, commandType: CommandType.StoredProcedure);

                trans.Commit();
                return (int)result.ExecValue;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi thực thi Workflow: {ex.Message}");
            }
        }

        public async Task<(string CusTax, string CusName)> GetContractInfoForEmailAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var sql = @"SELECT TOP 1 
                            ISNULL(CusTax,'') AS CusTax, 
                            ISNULL(CusName,'') AS CusName
                        FROM dbo.EContracts WITH(NOLOCK)
                        WHERE OID = @OID";
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { OID = oid });
            if (row == null) return (string.Empty, string.Empty);
            return ((string)row.CusTax, (string)row.CusName);
        }


        public async Task<(bool success, string message)> CreateEContractJobAsync(
            ERP_Portal_RC.Domain.Entities.EContractJobRequest request, string userId)
        {
            const string spName = "Ins_EContractJobs_RequestByPortal";

            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            if (conn.State == ConnectionState.Closed) conn.Open();

            // userId từ JWT ưu tiên hơn model.Crt_User
            var crtUser = !string.IsNullOrWhiteSpace(userId) ? userId
                        : request.Crt_User ?? string.Empty;

            var p = new DynamicParameters();
            p.Add("@OID",           request.OID ?? "");          // thường để trống, SP tự sinh
            p.Add("@ReferenceID",   request.ReferenceID);        // Contract OID — BẮT BUỘC
            p.Add("@FactorID",      request.FactorID);
            p.Add("@EntryID",       request.EntryID);
            p.Add("@Crt_User",      crtUser);
            p.Add("@Descrip",       request.Descrip ?? "Yêu cầu từ hệ thống Portal");
            p.Add("@FileLogo",      request.FileLogo    ?? "");
            p.Add("@FileInvoice",   request.FileInvoice ?? "");
            p.Add("@FileOther",     request.FileOther   ?? "");
            p.Add("@MailAcc",       request.MailAcc     ?? "");
            p.Add("@ReferenceInfo", request.ReferenceInfo ?? "");
            p.Add("@InvcSign",      request.InvcSign    ?? "");
            p.Add("@InvcFrm",       request.InvcFrm,    dbType: DbType.Int32);
            p.Add("@InvcEnd",       request.InvcEnd,    dbType: DbType.Int32);
            p.Add("@invcSample",    request.invcSample  ?? "");

            try
            {
                dynamic? result = null;
                try
                {
                    using var multi = await conn.QueryMultipleAsync(spName, p, commandType: CommandType.StoredProcedure);
                    while (!multi.IsConsumed)
                    {
                        var rows = (await multi.ReadAsync<dynamic>()).ToList();
                        var first = rows.FirstOrDefault();
                        if (first != null) result = first;
                    }
                }
                catch
                {
                    using var reader = await conn.ExecuteReaderAsync(spName, p, commandType: CommandType.StoredProcedure);
                    do
                    {
                        if (reader.Read())
                        {
                            var row = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] = reader.GetValue(i);
                            result = row;
                        }
                    } while (reader.NextResult());
                }

                if (result == null)
                    return (false, "SP không trả về kết quả.");

                // Parse — hỗ trợ cả IDictionary (ExpandoObject) lẫn dynamic (Dapper row)
                string oid = "", excStatus = "";
                if (result is IDictionary<string, object> dict)
                {
                    oid       = dict.TryGetValue("OID",       out var v1) ? v1?.ToString() ?? "" : "";
                    excStatus = dict.TryGetValue("excStatus", out var v2) ? v2?.ToString() ?? "" : "";
                }
                else
                {
                    oid       = result.OID?.ToString()       ?? "";
                    excStatus = result.excStatus?.ToString() ?? "";
                }

                var parts   = excStatus.Split('|', 2, StringSplitOptions.None);
                var flag    = parts.Length > 0 ? parts[0].Trim() : "0";
                var message = parts.Length > 1 ? parts[1].Trim() : excStatus;

                if (flag == "1")
                    return (true, string.IsNullOrEmpty(message) ? $"Yêu cầu tiếp nhận thành công (Job: {oid})." : message);

                if (message.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase) &&
                    message.Contains("đã duyệt",   StringComparison.OrdinalIgnoreCase))
                    return (false, $"Yêu cầu đã tồn tại và đã được duyệt (Job: {oid}).");

                return (false, string.IsNullOrEmpty(message) ? "Thất bại (SP trả về flag=0)." : message);
            }
            catch (Exception ex) when (GetSqlErrorNumber(ex) == 2627 || GetSqlErrorNumber(ex) == 2601)
            {
                throw new Exception($"Job đã tồn tại cho hợp đồng {request.ReferenceID} (SQL Duplicate: {ex.Message}).");
            }
        }



        public async Task<(bool success, string message)> AdvanceEContractJobSigningAsync(
            string contractOid, string factorId, string entryId,
            string userId, int fromSignNumb, int toSignNumb, string? appvMess = null)
        {
            // Bước 1: Tìm Job OID từ contract ReferenceID trong BosOnline
            using var connOnline = _dbConnectionFactory.GetConnection(BosOnline);
            var jobOid = await connOnline.QueryFirstOrDefaultAsync<string?>(
                @"SELECT TOP 1 OID 
                  FROM dbo.EContractJobs WITH(NOLOCK) 
                  WHERE ReferenceID = @RefID AND FactorID = @FactorID 
                  ORDER BY ODate DESC",
                new { RefID = contractOid, FactorID = factorId });

            if (string.IsNullOrEmpty(jobOid))
                return (false, $"Không tìm thấy Job ({factorId}) cho hợp đồng {contractOid}. Hãy chạy Propose Template trước.");

            // Bước 2: Gọi zsgn_EContractJobs_NOR với Job OID đã tìm được
            using var connApproval = _dbConnectionFactory.GetConnection("BosApproval");
            if (connApproval.State == ConnectionState.Closed) connApproval.Open();
            using var trans = connApproval.BeginTransaction();
            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID",      factorId);
                p.Add("@OID",           jobOid);
                p.Add("@ODate",         DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                p.Add("@CmpnID",        "26");
                p.Add("@Crt_User",      userId);
                p.Add("@DataTbl",       "EContractJobs");
                p.Add("@SignTble",       "zsgn_EContractJobs");
                p.Add("@SignChck",       0);
                p.Add("@holdSignNumb",   fromSignNumb);
                p.Add("@nextSignNumb",   toSignNumb);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess",       !string.IsNullOrEmpty(appvMess) ? appvMess : "Phát hành từ hệ thống Portal");
                // Variant01-25: để rỗng
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant26",      contractOid); // OIDJob (ReferenceID)
                p.Add("@Variant27",      "");
                p.Add("@Variant28",      "");
                p.Add("@Variant29",      "");
                p.Add("@Variant30",      "1");
                p.Add("@EntryID",        entryId);

                var result = await connApproval.QuerySingleAsync<dynamic>(
                    "zsgn_EContractJobs_NOR", p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();
                bool success = (int)result.ExecValue == 1;
                return (success, success
                    ? $"Phát hành thành công (Job: {jobOid})."
                    : $"Phát hành thất bại. Job có thể chưa ở trạng thái 101 (Job: {jobOid}).");
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi nâng trạng thái Job: {ex.Message}");
            }
        }
        public async  Task<Template> GetTemplateByCodeAsync(string factorId)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosControlEVAT);
            var parameters = new { cmpnID = "26", FactorID = factorId };
            return await connection.QueryFirstOrDefaultAsync<Template>(
            "dbo.ECtr_Get_Sample_ContentAll",
            parameters,
            commandType: CommandType.StoredProcedure);
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

        private static int GetSqlErrorNumber(Exception ex)
        {
            var prop = ex.GetType().GetProperty("Number");
            if (prop == null) return 0;
            try { return (int)(prop.GetValue(ex) ?? 0); }
            catch { return 0; }
        }

        #endregion

    }
}
