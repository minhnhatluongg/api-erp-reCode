using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.EntitiesIntergration;
using ERP_Portal_RC.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static ERP_Portal_RC.Domain.Enum.PublicEnum;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class EContractRepository : IEContractRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDSignaturesRepository _dSign;
        private readonly IConfiguration _configuration;
        private const string BosOnline = "BosOnline";
        private const string BosApproval = "BosApproval";
        private const string BosControlEVAT = "BosControlEVAT";
        private const string BosDocument = "BosDocument";
        private const string BosCataloge = "BosCataloge";
        public EContractRepository(IDbConnectionFactory dbConnectionFactory, IDSignaturesRepository dSign, IConfiguration configuration)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dSign = dSign;
            _configuration = configuration;
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
            using var conn = _dbConnectionFactory.GetConnection("BosApproval");
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Enrich Data: Lấy thông tin MST và SampleID từ DB nếu Model truyền lên bị trống
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
                p.Add("@Variant30", "1");
                // Phân nhánh Variant dựa trên nghiệp vụ Job hay EContract
                if (config.Factor.StartsWith("JOB"))
                {
                    p.Add("@DataTbl", "EContractJobs");
                    p.Add("@SignTble", "zsgn_EContractJobs");
                    p.Add("@Variant29", !string.IsNullOrEmpty(model.SampleID) ? model.SampleID : (string?)contractInfo?.SampleID);
                }
                else
                {
                    p.Add("@SignTble", "zsgn_EContracts");
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
            //const string spName = "Ins_EContractJobs_RequestByOdoo";

            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            if (conn.State == ConnectionState.Closed) conn.Open();

            var crtUser = !string.IsNullOrWhiteSpace(userId) ? userId
                        : request.Crt_User ?? string.Empty;

            var p = new DynamicParameters();
            p.Add("@OID", request.OID ?? "");          // thường để trống, SP tự sinh
            p.Add("@ReferenceID", request.ReferenceID);        // Contract OID — BẮT BUỘC
            p.Add("@FactorID", request.FactorID);
            p.Add("@EntryID", request.EntryID);
            p.Add("@Crt_User", crtUser);
            p.Add("@Descrip", request.Descrip ?? "Yêu cầu từ hệ thống Portal");
            p.Add("@FileLogo", request.FileLogo ?? "");
            p.Add("@FileInvoice", request.FileInvoice ?? "");
            p.Add("@FileOther", request.FileOther ?? "");
            p.Add("@MailAcc", request.MailAcc ?? "");
            p.Add("@ReferenceInfo", request.ReferenceInfo ?? "");
            p.Add("@InvcSign", request.InvcSign ?? "");
            p.Add("@InvcFrm", request.InvcFrm, dbType: DbType.Int32);
            p.Add("@InvcEnd", request.InvcEnd, dbType: DbType.Int32);
            p.Add("@invcSample", request.invcSample ?? "");

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

                string oid = "", excStatus = "";
                if (result is IDictionary<string, object> dict)
                {
                    oid = dict.TryGetValue("OID", out var v1) ? v1?.ToString() ?? "" : "";
                    excStatus = dict.TryGetValue("excStatus", out var v2) ? v2?.ToString() ?? "" : "";
                }
                else
                {
                    oid = result.OID?.ToString() ?? "";
                    excStatus = result.excStatus?.ToString() ?? "";
                }

                var parts = excStatus.Split('|', 2, StringSplitOptions.None);
                var flag = parts.Length > 0 ? parts[0].Trim() : "0";
                var message = parts.Length > 1 ? parts[1].Trim() : excStatus;

                if (flag == "1")
                    return (true, string.IsNullOrEmpty(message) ? $"Yêu cầu tiếp nhận thành công (Job: {oid})." : message);

                if (message.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase) &&
                    message.Contains("đã duyệt", StringComparison.OrdinalIgnoreCase))
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
                p.Add("@FactorID", factorId);
                p.Add("@OID", jobOid);
                p.Add("@ODate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                p.Add("@CmpnID", "26");
                p.Add("@Crt_User", userId);
                p.Add("@DataTbl", "EContractJobs");
                p.Add("@SignTble", "zsgn_EContractJobs");
                p.Add("@SignChck", 0);
                p.Add("@holdSignNumb", fromSignNumb);
                p.Add("@nextSignNumb", toSignNumb);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess", !string.IsNullOrEmpty(appvMess) ? appvMess : "Phát hành từ hệ thống Portal");
                // Variant01-25: để rỗng
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant26", contractOid);
                p.Add("@Variant27", "");
                p.Add("@Variant28", "");
                p.Add("@Variant29", "");
                p.Add("@Variant30", "1");
                p.Add("@EntryID", entryId);

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
        public async Task<Template> GetTemplateByCodeAsync(string factorId)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosControlEVAT);
            var parameters = new { cmpnID = "26", FactorID = factorId };
            return await connection.QueryFirstOrDefaultAsync<Template>(
            "dbo.ECtr_Get_Sample_ContentAll",
            parameters,
            commandType: CommandType.StoredProcedure);
        }

        public async Task<string> SaveFullContractAsync(EContractMaster master, List<EContractDetails> details)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var detailsTable = new DataTable();
            // KHAI BÁO CỘT: Thứ tự phải trùng khớp 100% với SQL Type ở Bước 1
            detailsTable.Columns.Add("ItemNo", typeof(int));         // 1
            detailsTable.Columns.Add("OID", typeof(string));         // 2
            detailsTable.Columns.Add("ItemID", typeof(string));      // 3
            detailsTable.Columns.Add("ItemName", typeof(string));    // 4
            detailsTable.Columns.Add("ItemUnit", typeof(string));    // 5
            detailsTable.Columns.Add("ItemPrice", typeof(decimal));  // 6
            detailsTable.Columns.Add("ItemQtty", typeof(decimal));   // 7
            detailsTable.Columns.Add("ItemAmnt", typeof(decimal));   // 8
            detailsTable.Columns.Add("VAT_Rate", typeof(decimal));   // 9
            detailsTable.Columns.Add("VAT_Amnt", typeof(decimal));   // 10
            detailsTable.Columns.Add("Sum_Amnt", typeof(decimal));   // 11
            detailsTable.Columns.Add("Descrip", typeof(string));     // 12 
            detailsTable.Columns.Add("InvcSign", typeof(string));    // 13
            detailsTable.Columns.Add("InvcFrm", typeof(int));        // 14
            detailsTable.Columns.Add("InvcEnd", typeof(int));        // 15
            detailsTable.Columns.Add("invcSample", typeof(string));  // 16
            detailsTable.Columns.Add("itemUnitName", typeof(string));// 17
            detailsTable.Columns.Add("ItemPerBox", typeof(decimal)); // 18

            int count = 1;
            foreach (var d in details)
            {
                decimal amnt = d.ItemQtty * d.ItemPrice;
                decimal vatAmnt = amnt * (d.VAT_Rate / 100);
                string safeName = d.ItemName ?? ""; // Tránh NULL

                detailsTable.Rows.Add(
                    count++,                          // 1. ItemNo
                    master.OID,                       // 2. OID
                    d.ItemID ?? "",                   // 3. ItemID
                    safeName,                         // 4. ItemName
                    d.ItemUnit ?? "",                 // 5. ItemUnit
                    d.ItemPrice,                      // 6. ItemPrice
                    d.ItemQtty,                       // 7. ItemQtty
                    amnt,                             // 8. ItemAmnt
                    d.VAT_Rate,                       // 9. VAT_Rate
                    vatAmnt,                          // 10. VAT_Amnt
                    amnt + vatAmnt,                   // 11. Sum_Amnt
                    safeName,                         // 12. Descrip 
                    d.InvcSign ?? "",                 // 13. InvcSign
                    d.InvcFrm,                        // 14. InvcFrm
                    d.InvcEnd,                        // 15. InvcEnd
                    d.InvcSample ?? "",               // 16. invcSample
                    d.itemUnitName ?? d.ItemUnit,     // 17. itemUnitName
                    d.ItemPerBox                      // 18. ItemPerBox
                );
            }

            var parameters = new DynamicParameters();

            parameters.Add("@CmpnID", master.CmpnID);
            parameters.Add("@OID", master.OID);
            parameters.Add("@ODate", master.ODate);
            parameters.Add("@FactorID", master.FactorID);
            parameters.Add("@EntryID", master.EntryID);
            parameters.Add("@SaleEmID", master.SaleEmID);
            parameters.Add("@CmpnName", master.CmpnName);
            parameters.Add("@CmpnAddress", master.CmpnAddress);
            parameters.Add("@CmpnContactAddress", master.CmpnContactAddress);
            parameters.Add("@CmpnTax", master.CmpnTax);
            parameters.Add("@CmpnTel", master.CmpnTel);
            parameters.Add("@CmpnMail", master.CmpnMail);
            parameters.Add("@CmpnPeople_Sign", master.CmpnPeople_Sign);
            parameters.Add("@CmpnPosition_BySign", master.CmpnPosition_BySign); 
            parameters.Add("@CmpnBankNumber", master.CmpnBankNumber);
            parameters.Add("@CmpnBankAddress", master.CmpnBankAddress);
            parameters.Add("@SignDate", DateTime.Now);
            parameters.Add("@TaxDepartment", master.TaxDepartment ?? "");
            //parameters.Add("@TinhThanhTitle", "");
            parameters.Add("@Descript_Cus", master.Descript_Cus ?? "");

            // Thông tin Khách hàng
            parameters.Add("@CustomerID", master.CustomerID);
            parameters.Add("@CusName", master.CusName);
            parameters.Add("@RegionID", master.RegionID);
            parameters.Add("@CusAddress", master.CusAddress);
            parameters.Add("@CusContactAddress", master.CusContactAddress);
            parameters.Add("@CusTax", master.CusTax);
            parameters.Add("@CusTel", master.CusTel);
            parameters.Add("@CusFax", master.CusFax);
            parameters.Add("@CusEmail", master.CusEmail);
            parameters.Add("@CusPeople_Sign", master.CusPeople_Sign);
            parameters.Add("@CusPosition_BySign", master.CusPosition_BySign);
            parameters.Add("@CusBankNumber", master.CusBankNumber);
            parameters.Add("@CusBankAddress", master.CusBankAddress);
            parameters.Add("@CmpID_Sign", master.CmpID_Sign ?? "");
            parameters.Add("@CmpName_Sign", master.CmpName_Sign ?? "");
            parameters.Add("@isUsingAcc", 0);
            parameters.Add("@SignNumb", -1);
            parameters.Add("@tokenOID", master.tokenOID);

            // Thông tin Tiền tệ & Hệ thống
            parameters.Add("@PrdcAmnt", master.PrdcAmnt);
            parameters.Add("@VAT_Rate", master.VAT_Rate);
            parameters.Add("@VAT_Amnt", master.VAT_Amnt);
            parameters.Add("@IsVAT", 1);
            parameters.Add("@DscnAmnt", master.DscnAmnt);
            parameters.Add("@Sum_Amnt", master.Sum_Amnt);
            parameters.Add("@SampleID", master.SampleID);
            parameters.Add("@HTMLContent", master.HTMLContent);
            parameters.Add("@Descrip", master.Descrip);
            parameters.Add("@Crt_User", master.Crt_User);
            parameters.Add("@ChgeUser", master.ChgeUser);

            // Các trường bổ sung mới
            parameters.Add("@CusWebsite", master.CusWebsite);
            parameters.Add("@Date_BusLicence", master.Date_BusLicence);
            parameters.Add("@OIDContract", master.OIDContract);
            parameters.Add("@RefeContractDate", master.RefeContractDate);

            // Các flags
            parameters.Add("@IsCapBu", master.IsCapBu);
            parameters.Add("@IsGiaHan", master.IsGiaHan);
            parameters.Add("@IsOnline", master.IsOnline);
            parameters.Add("@isTT78", master.IsTT78);

            // 3. Add tham số Table Details
            parameters.Add("@Details", detailsTable.AsTableValuedParameter("dbo.EContractDetailType"));

            // Thực thi
            await conn.ExecuteAsync("dbo.sp_EContract_InsertAll_new", parameters, commandType: CommandType.StoredProcedure);
            return master.OID;
        }

        public async Task CreateApprovalFlowAsync(EContractMaster master)
        {
            using var conn = _dbConnectionFactory.GetConnection("BosApproval");

            var parameters = new DynamicParameters();
            parameters.Add("@FactorID", master.FactorID);
            parameters.Add("@OID", master.OID);
            parameters.Add("@ODate", master.ODate.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd"));
            parameters.Add("@CmpnID", master.CmpnID);
            parameters.Add("@Crt_User", master.Crt_User);
            parameters.Add("@DataTbl", string.Empty);
            parameters.Add("@SignTble", "zsgn_EContracts");
            parameters.Add("@SignChck", 0);
            parameters.Add("@holdSignNumb", 0);
            parameters.Add("@nextSignNumb", 0);
            parameters.Add("@Variant22", string.Empty);
            parameters.Add("@Variant26", string.Empty);
            parameters.Add("@Variant27", master.CusTax ?? string.Empty);
            parameters.Add("@Variant28", master.CmpnTax ?? string.Empty);
            parameters.Add("@Variant29", master.SampleID ?? "0010");
            parameters.Add("@EntryID", master.EntryID ?? "EC:001");
            parameters.Add("@AppvMess", ".");

            try
            {
                await conn.ExecuteAsync(
                    "dbo.zsgn_webContracts_NOR",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo luồng phê duyệt cho OID {master.OID}: {ex.Message}");
            }
        }

        #region Helper
        private void MapEContractStatus(List<EContract_Monitor> list, string currentCrtUser)
        {
            if (list == null || !list.Any()) return;

            var specialDate = new DateTime(2020, 07, 13);

            foreach (var item in list)
            {
                // 1. Xử lý SiteName
                if (item.ODATE < specialDate && item.CmpnID == "26")
                    item.SiteName = "MONET";

                // 2. Quyền chỉnh sửa
                item.IsDisiable = item.Crt_User != currentCrtUser;

                // 3. Logic Hợp đồng giấy/Phụ lục
                if (item.isContractPaper || item.isPLHD)
                    item.TT3 = TTStatus.TT3_DACAP;

                // 4. Thiết kế mẫu
                if (item.isDesignInvoice)
                    item.TT2 = TTStatus.TT2_THIETKE;

                // 5. Kiểm tra kỹ thuật (Job KT)
                if (item.currSignNumbJobKT != 0)
                    item.TT2 = item.TT6;

                // 6. Hiển thị Checkbox Xuất hóa đơn
                item.isCheckedShow = !string.IsNullOrEmpty(item.XHD);

                // 7. Logic mặc định cho Tool & TT78
                bool isDefault = item.TT2 == "Chưa có yêu cầu tạo mẫu" &&
                                 item.TT3 == TTStatus.TT3_CHUACAP &&
                                 item.TT4 == TTStatus.TT4_CHUACAP;

                if (item.isTool && item.isTT78 && isDefault)
                {
                    item.TT2 = "Tạo mẫu thiết kế: Thực hiện";
                    item.TT3 = TTStatus.TT3_DACAP;
                    if (!item.isGiaHan) item.TT4 = TTStatus.TT4_DACAP;
                }

                // --- LOGIC CHO TT8 (XUẤT HÓA ĐƠN HĐĐT) ---
                item.isCheckXHD = item.TT8 != "Chưa có yêu cầu Xuất hóa đơn HĐĐT";
                item.isDisableCheckXHD = (item.TT8 == "Đã gửi yêu cầu xuất hóa đơn" || item.TT8 == "Đã xuất hóa đơn điện tử");
                // 9. Các flag trạng thái Checkbox cho các cột khác
                item.ischeckTK = (item.TT3 == TTStatus.TT3_DACAP || item.isGiaHan);
                item.ischeckPH = (item.TT4 == TTStatus.TT4_DACAP);
                item.ischeckKNV = (item.TT4 == TTStatus.TT4_KHOANV);
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

        public async Task<EContractStatusRaw> GetContractStatusRawAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var result = new EContractStatusRaw();

            using (var multi = await conn.QueryMultipleAsync("dbo.sp_EContract_GetStatusSummary",
                   new { OID = oid },
                   commandType: CommandType.StoredProcedure))
            {
                result.Master = await multi.ReadFirstOrDefaultAsync<EContractMasterSummary>();
                if (result.Master == null) return null;

                result.Details = (await multi.ReadAsync<EContractDetailSummary>()).ToList();

                result.SignedData = await multi.ReadFirstOrDefaultAsync<ContractPublicInfoSummary>();
            }
            return result;
        }

        public async Task<(int Ok, string Message)> DeleteDraftAsync(string oid, string username)
        {
            using var con = _dbConnectionFactory.GetConnection(BosOnline);
            var p = new DynamicParameters();
            p.Add("@OID", oid.Trim());
            p.Add("@DeletedBy", username);
            p.Add("@OK", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@Message", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);

            await con.ExecuteAsync("dbo.Del_EContract_Draft", p, commandType: CommandType.StoredProcedure);

            return (p.Get<int>("@OK"), p.Get<string>("@Message"));
        }

        public async Task<(bool Success, string Message, object Data)> UnSignAsync(UnSignRequest model, string correlationId)
        {
            using var con = _dbConnectionFactory.GetConnection(BosOnline);
            if (con.State == ConnectionState.Closed) await ((System.Data.Common.DbConnection)con).OpenAsync();

            using var trans = con.BeginTransaction();
            try
            {
                // 1. Xóa thông tin trình ký
                int delZsgn = await con.ExecuteAsync(
                    "DELETE FROM BosApproval.dbo.zsgn_webContracts WHERE OID = @OID",
                    new { model.OID }, trans);

                // 2. Xóa thông tin Public (Hợp đồng đã ký)
                int delPublic = await con.ExecuteAsync(
                    "DELETE FROM BosControlEVAT.dbo.ECtr_PublicInfo WHERE InvcCode = @OID",
                    new { model.OID }, trans);

                string status = (delZsgn > 0 || delPublic > 0) ? "DELETED" : "NO_ACTION";
                string msg = $"Kết quả: {delZsgn} dòng zsgn, {delPublic} dòng PublicInfo đã được xử lý.";

                // 3. Ghi Log Unsign
                await con.ExecuteAsync(@"
                INSERT INTO BosControlEVAT.dbo.ECtr_UnsignLogs 
                (OID, CorrelationId, Reason, RequestedBy, FullName, [Role], ActionStatus, ActionMessage)
                VALUES (@OID, @CorrelationId, @Reason, @RequestedBy, @FullName, @Role, @status, @msg)",
                    new
                    {
                        model.OID,
                        CorrelationId = correlationId,
                        model.Reason,
                        model.RequestedBy,
                        model.FullName,
                        model.Role,
                        status,
                        msg
                    }, trans);

                trans.Commit();
                return (true, msg, new { delZsgn, delPublic, status });
            }
            catch (Exception)
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task<EContractHistoryRaw> GetFullHistoryDataAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection("BosOnline");
            var raw = new EContractHistoryRaw();
            string cleanOid = oid.Replace("%2F", "/").Replace("%2f", "/");
            cleanOid = System.Net.WebUtility.UrlDecode(cleanOid);
            using (var multi = await conn.QueryMultipleAsync("wspGet_EContracts_ByID_History", new { OID = cleanOid }, commandType: CommandType.StoredProcedure))
            {
                raw.History = (await multi.ReadAsync<HistoryListEntity>()).ToList();
            }

            using (var multiJob = await conn.QueryMultipleAsync("wspGet_EContracts_ByID_DS", new { OID = cleanOid }, commandType: CommandType.StoredProcedure))
            {
                multiJob.Read<dynamic>();
                raw.Jobs = (await multiJob.ReadAsync<JobEntity>()).ToList();
            }

            return raw;
        }

        public async Task<List<JobEntity>> GetJobKTbyOID(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection("BosOnline");
            try
            {
                var result = await conn.QueryAsync<JobEntity>(
                    "GetJobKTbyOID",
                    new { oid },
                    commandType: CommandType.StoredProcedure
                );
                return result.ToList();
            }
            catch (Exception ex)
            {
                return new List<JobEntity>();
            }
        }

        public async Task<List<EContractDetails>> GetEContractDetailsNewAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            try
            {
                string query = "getEContractDetailsNew";
                DynamicParameters para = new DynamicParameters();
                para.Add("@oid", oid);

                var result = await conn.QueryAsync<EContractDetails>(
                    query,
                    param: para,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                return new List<EContractDetails>();
            }
        }

        public async Task<EContractHistoryRaw2> GetEContractRawDataAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var raw = new EContractHistoryRaw2();
            try
            {
                using (var multi = await conn.QueryMultipleAsync(
                    "wspGet_EContracts_ByID",
                    new { OID = oid },
                    commandType: CommandType.StoredProcedure))
                {
                    // 1. Table 1: Thông tin Master của hợp đồng (EContracts_Rslt)
                    raw.EContract = await multi.ReadFirstOrDefaultAsync<EContractMaster>();

                    // 2. Table 2: Danh sách Jobs liên quan (EContractJobs)
                    raw.Jobs = (await multi.ReadAsync<JobEntity>()).ToList();

                    // 3. Table 3: Danh sách các bước ký/duyệt (zsgn_EContractJobs)
                    // Trong code cũ bạn gọi đây là JobPost
                    raw.JobPosts = (await multi.ReadAsync<JobPost>()).ToList();

                    // 4. Table 4: Danh sách File đính kèm (DocAttachfile)
                    raw.ListFiles = (await multi.ReadAsync<ListFile>()).ToList();

                    // 5. Table 5: Chi tiết sản phẩm/dịch vụ (EContractDetails)
                    raw.EContractDetails = (await multi.ReadAsync<EContractDetails>()).ToList();

                    // 6. Table 6: Thông tin đơn vị chủ quản (bosCompanyInfo)
                    raw.Vendor = await multi.ReadFirstOrDefaultAsync<VendorEntity>();

                    // 7. Table 7: Thông tin mẫu hợp đồng (EVat_Samples)
                    raw.TemplateEcontract = await multi.ReadFirstOrDefaultAsync<templateEcontract>();

                    // 8. Table 8: Thông tin công khai hóa đơn (ECtr_PublicInfo)
                    raw.ECtr_PublicInfo = await multi.ReadFirstOrDefaultAsync<ECtr_PublicInfo>();

                    // 9. Table 9: Thông tin Email người dùng (HmrEmplProfile)
                    raw.EmailUser = await multi.ReadFirstOrDefaultAsync<EmailUser>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy dữ liệu Raw cho OID {oid}: {ex.Message}");
            }
            return raw;
        }

        public async Task DeleteJob01Async(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                const string sql = @"DELETE FROM EContractJobs 
                                 WHERE ReferenceID = @OID 
                                   AND FactorID = 'JOB_00001' 
                                   AND EntryID = 'JB:001'";
                await conn.ExecuteAsync(sql, new { OID = oid }, trans);
                trans.Commit();

            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task<JobEntity> InsertJobAsync(JobEntity job)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                if (!string.IsNullOrEmpty(job.OID))
                {
                    await UploadFileAsync(job);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@ReferenceID", job.ReferenceID);
                    p.Add("@FactorID", job.FactorID);
                    p.Add("@EntryID", job.EntryID);
                    p.Add("@Descrip", job.Descrip ?? string.Empty);
                    p.Add("@FileLogo", job.FileLogo ?? string.Empty);
                    p.Add("@FileInvoice", job.FileInvoice ?? string.Empty);
                    p.Add("@FileOther", job.FileOther ?? string.Empty);
                    p.Add("@Crt_User", job.Crt_User ?? string.Empty);
                    p.Add("@InvcSign", job.InvcSign ?? string.Empty);
                    p.Add("@InvcFrm", job.InvcFrm ?? 0);
                    p.Add("@InvcEnd", job.InvcEnd ?? 0);
                    p.Add("@invcSample", job.invcSample ?? string.Empty);
                    p.Add("@CmpnID", job.cmpnID ?? string.Empty);
                    p.Add("@OID", "", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                    p.Add("@MailAcc", "ketoanhoadondientu@win-tech.vn");
                    p.Add("@ReferenceInfo", job.ReferenceInfo ?? string.Empty);
                    p.Add("@isAuto", job.isAuto_InvcNumb);

                    await conn.ExecuteAsync("wspInsert_EContractJobs_IsAuto", p, trans, commandType: CommandType.StoredProcedure);
                }
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            var result = await conn.QueryMultipleAsync("wspList_Job", new { OID = job.ReferenceID }, commandType: CommandType.StoredProcedure);
            return result.Read<JobEntity>().Last();
        }

        public async Task UploadFileAsync(JobEntity job)
        {
            var files = new List<string> {
            job.FileName0, job.FileName1, job.FileName2, job.FileName3, job.FileName4}.Where(f => !string.IsNullOrEmpty(f)).ToList();

            if (!files.Any()) return;

            using var conn = _dbConnectionFactory.GetConnection(BosDocument);
            conn.Open();

            const string sQuery = @" INSERT INTO [BosDocument].[dbo].[DocAttachfile]
                                        (AttachType, 
                                        AttachNote, 
                                        AttachDate, 
                                        AttachFile, 
                                        ConvertFile, 
                                        OID, 
                                        FactorID, 
                                        EntryID, 
                                        Crt_User, DocSource, DocSourceDateField, DocSourceDateField_Value, LinkFonder)
                                            VALUES (
                                            @AttachType, 
                                            @AttachNote, 
                                            GETDATE(), 
                                            @AttachFile, 
                                            @ConvertFile, 
                                            @OID, 
                                            @FactorID, 
                                            @EntryID, 
                                            @Crt_User, 
                                            @DocSource, 
                                            @DocSourceDateField, 
                                            GETDATE(), 
                                            @LinkFonder)";
            var oidLink = job.ReferenceID?.Replace("/", "").Replace(":", "") ?? "";

            foreach (var fileName in files)
            {
                using var trans = conn.BeginTransaction();
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@AttachType", job.AttachType ?? ".");
                    parameters.Add("@AttachNote", job.AttachNote ?? ".");
                    parameters.Add("@AttachDate", DateTime.Now.ToString());
                    parameters.Add("@AttachFile", fileName);
                    parameters.Add("@ConvertFile", fileName ?? string.Empty);
                    parameters.Add("@OID", job.OID);
                    parameters.Add("@FactorID", job.FactorIDAtt);
                    parameters.Add("@EntryID", job.EntryID);
                    parameters.Add("@Crt_User", job.Crt_User);
                    parameters.Add("@DocSource", "");
                    parameters.Add("@DocSourceDateField", job.FileUrl);
                    parameters.Add("@LinkFonder", oidLink);

                    await conn.ExecuteAsync(sQuery, parameters, trans);
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public async Task<List<DepartmentsEntity>> GetDepartmentsByOidAsync(string did)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosCataloge);
            conn.Open();
            var result = await conn.QueryAsync<DepartmentsEntity>(
                "GetDeparment",
                new { DID = did },
                commandType: CommandType.StoredProcedure);
            return result.ToList();

        }

        public async Task<List<EContractDetails>> VerifyJobAsync(string cusTax, string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            conn.Open();

            var result = await conn.QueryAsync<EContractDetails>(
                "wps_VerifyJob",
                new { taxNumber = cusTax, OID = oid },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }
        public async Task UpdateJobSaveAsync(JobEntity job, List<JobPackEntity> jobPacks, int sumInvc, int? countChange, string info, string descript)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var mainParams = new DynamicParameters();
                mainParams.Add("@ReferenceID", job.ReferenceID);
                mainParams.Add("@OID", job.OID);
                mainParams.Add("@FactorID", job.FactorID);
                mainParams.Add("@EntryID", job.EntryID);
                mainParams.Add("@Descrip", descript);
                mainParams.Add("@FileLogo", job.FileLogo ?? string.Empty);
                mainParams.Add("@FileInvoice", job.FileInvoice ?? string.Empty);

                mainParams.Add("@FileOther", job.FileOther ?? string.Empty);
                mainParams.Add("@FileName0", job.FileName0 ?? string.Empty);
                mainParams.Add("@FileName1", job.FileName1 ?? string.Empty);
                mainParams.Add("@FileName2", job.FileName2 ?? string.Empty);
                mainParams.Add("@FileName3", job.FileName3 ?? string.Empty);
                mainParams.Add("@FileName4", job.FileName4 ?? string.Empty);
                mainParams.Add("@FileName5", job.FileName5 ?? string.Empty);
                mainParams.Add("@FileName6", job.FileName6 ?? string.Empty);
                mainParams.Add("@FileName7", job.FileName7 ?? string.Empty);
                mainParams.Add("@FileName8", job.FileName8 ?? string.Empty);
                mainParams.Add("@FileName9", job.FileName9 ?? string.Empty);

                mainParams.Add("@Crt_User", job.Crt_User);
                mainParams.Add("@InvcSign", job.InvcSign ?? string.Empty);
                mainParams.Add("@InvcFrm", job.InvcFrm ?? 0);
                mainParams.Add("@InvcEnd", sumInvc);
                mainParams.Add("@invcSample", job.invcSample ?? string.Empty);
                mainParams.Add("@PackID", job.PackID ?? string.Empty);
                mainParams.Add("@ReferenceInfo", info);
                mainParams.Add("@CountChange", countChange);
                mainParams.Add("@Reason", job.ChangeOption ?? string.Empty);
                mainParams.Add("@DescriptChange", job.DescriptChange ?? string.Empty);
                mainParams.Add("@TemplateID", job.TemplateID ?? string.Empty);
                mainParams.Add("@Issave", true);
                mainParams.Add("@OperDept", job.OperDept ?? string.Empty);
                mainParams.Add("@isDesignInvoices", job.isDesignInvoices);

                await conn.ExecuteAsync("wspUpdate_EContractJobs_Save", mainParams, transaction, commandType: CommandType.StoredProcedure);

                // 2. Cập nhật chi tiết từng gói sản phẩm trong Job (wpsIns_EContractJobDetail)
                foreach (var pack in jobPacks)
                {
                    var packParams = new DynamicParameters();
                    packParams.Add("@OID", job.OID);
                    packParams.Add("@ItemID", pack.ItemID);
                    packParams.Add("@Descrip", pack.Descrip ?? string.Empty);
                    packParams.Add("@InvcSign", pack.InvcSign);
                    packParams.Add("@InvcFrm", pack.InvcFrm);
                    packParams.Add("@InvcEnd", pack.InvcEnd);
                    packParams.Add("@invcSample", pack.invcSample);
                    packParams.Add("@ItemNo", pack.ItemNo);
                    packParams.Add("@PublDate", pack.PublDate);
                    packParams.Add("@Use_Date", pack.Use_Date);

                    await conn.ExecuteAsync("wpsIns_EContractJobDetail", packParams, transaction, commandType: CommandType.StoredProcedure);
                }
                await conn.ExecuteAsync("wspUpdate_EContractJobs_ByPackID", new { OID = job.OID }, transaction, commandType: CommandType.StoredProcedure);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<Limit> limitcn(string cmpnId, string saleId, string GroupItem)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new DynamicParameters();
            parameters.Add("@CmpnID", cmpnId);
            parameters.Add("@SaleID", saleId);
            parameters.Add("@GroupItem", GroupItem);

            return await connection.QueryFirstOrDefaultAsync<Limit>(
                "GetDebtLtdSales",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<List<ListFile>> GetListFilesAsync(string oid)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var result = await connection.QueryAsync<ListFile>(
                "Get_ListFile",
                new { OID = oid },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<Right_EContracts?> GetRightEContractsAsync(int currSign, string grpList)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);

            var parameters = new DynamicParameters();
            parameters.Add("@CurrSign", currSign);
            parameters.Add("@Grp_Code", grpList);

            return await connection.QueryFirstOrDefaultAsync<Right_EContracts>(
                "wspRight_EContracts",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<byte[]?> GetInvcContentXmlAsync(string oid)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);
            string sql = "SELECT InvcContent FROM BosControlEVAT.dbo.ECtr_PublicInfo WHERE InvcCode = @OID";
            return await connection.ExecuteScalarAsync<byte[]>(sql, new { OID = oid });
        }

        public async Task<EmailUserRawData?> GetEmailUserDeptAsync(string oid)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);

            var parameters = new DynamicParameters();
            parameters.Add("@OID", oid);

            var data = new EmailUserRawData();

            try
            {
                using (var multi = await connection.QueryMultipleAsync(
                    "Get_EmailByJob",
                    parameters,
                    commandType: CommandType.StoredProcedure))
                {
                    data.Jobs = (await multi.ReadAsync<JobEntity>()).ToList();
                    data.EmailUserDept = await multi.ReadFirstOrDefaultAsync<EmailUserDept>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin Email theo Job OID {oid}: {ex.Message}");
            }
            return data;
        }

        public async Task<bool> ApproveContractJobAsync(ZsgnEContractJob entity, int holdSignNumb, int nextSignNumb)
        {
            using var connApproval = _dbConnectionFactory.GetConnection(BosApproval);

            if (connApproval.State == ConnectionState.Closed) connApproval.Open();
            using var trans = connApproval.BeginTransaction();

            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID", entity.FactorID);
                p.Add("@OID", entity.OID);
                p.Add("@ODate", entity.ODate.ToString("yyyy-MM-dd HH:mm:ss"));
                p.Add("@CmpnID", entity.CmpnID ?? "26");
                p.Add("@Crt_User", entity.Crt_User);
                p.Add("@DataTbl", entity.DataTbl ?? "EContractJobs");
                p.Add("@SignTble", "zsgn_EContractJobs");
                p.Add("@SignChck", 0);
                p.Add("@holdSignNumb", holdSignNumb);
                p.Add("@nextSignNumb", nextSignNumb);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess", entity.AppvMess ?? "Duyệt từ hệ thống Portal");

                for (int i = 1; i <= 25; i++)
                {
                    p.Add($"@Variant{i:D2}", "");
                }

                p.Add("@Variant26", entity.ReferenceID ?? "");
                p.Add("@Variant27", "");
                p.Add("@Variant28", "");
                p.Add("@Variant29", "");
                p.Add("@Variant30", entity.Variant30 ?? "1");
                p.Add("@EntryID", entity.EntryID);

                var result = await connApproval.QuerySingleAsync<dynamic>(
                    "zsgn_EContractJobs_NOR",
                    p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();
                return (int)result.ExecValue == 1;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi thực thi duyệt Job OID {entity.OID}: {ex.Message}");
            }
        }
        public async Task UpdateJobChangeAsync(JobEntity job, int? countChange, string info)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            if (conn.State == ConnectionState.Closed) conn.Open();

            using var trans = conn.BeginTransaction();
            try
            {
                // 1. Cập nhật thông tin Job Master kèm thông tin người thực hiện
                var detailParams = new DynamicParameters();
                detailParams.Add("@ReferenceID", job.ReferenceID);
                detailParams.Add("@FactorID", job.FactorID);
                detailParams.Add("@EntryID", "JB:005");
                detailParams.Add("@Descrip", job.Descrip ?? string.Empty);
                detailParams.Add("@FileLogo", job.FileLogo ?? string.Empty);
                detailParams.Add("@FileInvoice", job.FileInvoice ?? string.Empty);
                detailParams.Add("@FileOther", job.FileOther ?? string.Empty);
                detailParams.Add("@Crt_User", job.Crt_User ?? string.Empty);
                detailParams.Add("@InvcSign", job.InvcSign ?? string.Empty);
                detailParams.Add("@InvcFrm", job.InvcFrm ?? 0);
                detailParams.Add("@InvcEnd", job.InvcEnd ?? 0);
                detailParams.Add("@invcSample", job.invcSample ?? string.Empty);
                detailParams.Add("@PackID", job.PackID ?? string.Empty);
                detailParams.Add("@ReferenceInfo", info ?? string.Empty);
                detailParams.Add("@OID", job.OID);
                detailParams.Add("@CountChange", countChange);
                detailParams.Add("@Reason", job.ChangeOption ?? string.Empty);
                detailParams.Add("@DescriptChange", job.DescriptChange ?? string.Empty);
                detailParams.Add("@exeEmplName", job.EmplName ?? string.Empty);
                detailParams.Add("@exeEmplID", job.EmplID ?? string.Empty);
                detailParams.Add("@ReferenceDate", DateTime.Now);
                detailParams.Add("@OperDept", job.OperDept);

                await conn.ExecuteAsync("wspUpdate_EContractJobs_exeEmplName_v1",
                    detailParams, transaction: trans, commandType: CommandType.StoredProcedure);

                // 2. Chèn/Cập nhật chi tiết Job (JobDetail)
                var detailParams2 = new DynamicParameters();
                detailParams2.Add("@OID", job.OID);
                detailParams2.Add("@ItemID", job.PackID);
                detailParams2.Add("@Descrip", job.Descrip ?? string.Empty);
                detailParams2.Add("@InvcSign", job.InvcSign);
                detailParams2.Add("@InvcFrm", job.InvcFrm);
                detailParams2.Add("@InvcEnd", job.InvcEnd);
                detailParams2.Add("@invcSample", job.invcSample);
                detailParams2.Add("@ItemNo", job.ItemNo);
                detailParams2.Add("@PublDate", job.PublDate);
                detailParams2.Add("@Use_Date", job.Use_Date);

                await conn.ExecuteAsync("wpsIns_EContractJobDetail",
                    detailParams2, transaction: trans, commandType: CommandType.StoredProcedure);

                // 3. Cập nhật thông tin chi tiết hợp đồng chính (EContractsDetails)
                var detailParamsDetail = new DynamicParameters();
                detailParamsDetail.Add("@OID", job.ReferenceID);
                detailParamsDetail.Add("@ItemID", job.PackID);
                detailParamsDetail.Add("@InvcSign", job.InvcSign);
                detailParamsDetail.Add("@InvcFrm", job.InvcFrm);
                detailParamsDetail.Add("@InvcEnd", job.InvcEnd);
                detailParamsDetail.Add("@invcSample", job.invcSample);

                await conn.ExecuteAsync("wspUpdate_EContractsDetails_V2",
                    detailParamsDetail, transaction: trans, commandType: CommandType.StoredProcedure);

                // 4. Cập nhật lại PackID cho Job
                await conn.ExecuteAsync("wspUpdate_EContractJobs_ByPackID",
                    new { OID = job.OID }, transaction: trans, commandType: CommandType.StoredProcedure);

                // 5. Thực hiện trình ký tự động (zsgn_EContractJobs_NOR)
                var approveParams = new DynamicParameters();
                approveParams.Add("@FactorID", "JOB_00001");
                approveParams.Add("@OID", job.OID);
                approveParams.Add("@ODate", DateTime.Now.ToString("yyyy/MM/dd"));
                approveParams.Add("@CmpnID", job.cmpnID ?? "26");
                approveParams.Add("@Crt_User", job.Crt_User);
                approveParams.Add("@DataTbl", string.Empty);
                approveParams.Add("@SignTble", "zsgn_EContractJobs");
                approveParams.Add("@SignChck", string.Empty);
                approveParams.Add("@holdSignNumb", 0);
                approveParams.Add("@nextSignNumb", 101);
                approveParams.Add("@Variant22", string.Empty);
                approveParams.Add("@Variant30", string.Empty);
                approveParams.Add("@EntryID", "JB:005");
                approveParams.Add("@AppvMess", "Yêu cầu chỉnh sửa");
                approveParams.Add("@AppvRouteGrpTp", "1");

                await conn.ExecuteAsync("BosApproval.dbo.zsgn_EContractJobs_NOR",
                    approveParams, transaction: trans, commandType: CommandType.StoredProcedure);
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi khi cập nhật Job Change OID {job.OID}: {ex.Message}");
            }
        }
        public async Task<string> GetByOIDJobChangeAsync(string OID)
        {
            var rawData = await GetEContractRawDataAsync(OID);

            if (rawData == null)
            {
                throw new Exception($"Không tìm thấy dữ liệu hợp đồng cho OID {OID}");
            }
            var expandedDetails = new List<EContractDetails>();
            var targetDetails = rawData.EContractDetails
                .Where(s => s.UsIN == "JOB_00001")
                .ToList();
            foreach (var detail in targetDetails)
            {
                detail.InvcEnd = (int)detail.ItemPerBox;
                int repeatCount = (int)(detail.ItemQtty > 1 ? detail.ItemQtty : 1);
                for (int i = 0; i < repeatCount; i++)
                {
                    expandedDetails.Add(detail);
                }
            }
            var jobRequest = new JobEntity
            {
                ReferenceID = OID,
                FactorID = "JOB_00001", // JobFactor.JOB_00001
                EntryID = "JB:005",    // JobEntry.JB:005
                Crt_User = rawData.EContract.Crt_User,
                cmpnID = rawData.EContract.CmpnID
            };
            var resultJob = await InsertJobAsync(jobRequest);
            return resultJob.OID;
        }

        public async Task<JobEntity> InsertJobChangeYCAsync(JobEntity job)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            if (conn.State == ConnectionState.Closed) conn.Open();

            using var trans = conn.BeginTransaction();
            try
            {

                if (!string.IsNullOrEmpty(job.OID))
                {
                    await UploadFileAsync(job);
                }
                else
                {
                    var detailParams = new DynamicParameters();
                    detailParams.Add("@ReferenceID", job.ReferenceID);
                    detailParams.Add("@FactorID", job.FactorID);
                    detailParams.Add("@EntryID", job.EntryID);
                    detailParams.Add("@Descrip", job.Descrip ?? string.Empty);
                    detailParams.Add("@FileLogo", job.FileLogo ?? string.Empty);
                    detailParams.Add("@FileInvoice", job.FileInvoice ?? string.Empty);
                    detailParams.Add("@FileOther", job.FileOther ?? string.Empty);
                    detailParams.Add("@Crt_User", job.Crt_User ?? string.Empty);
                    detailParams.Add("@InvcSign", job.InvcSign ?? string.Empty);
                    detailParams.Add("@InvcFrm", job.InvcFrm ?? 0);
                    detailParams.Add("@InvcEnd", job.InvcEnd ?? 0);
                    detailParams.Add("@invcSample", job.invcSample ?? string.Empty);
                    detailParams.Add("@CmpnID", job.cmpnID ?? string.Empty);
                    detailParams.Add("@OID", "");

                    await conn.ExecuteAsync("wspInsert_EContractJobs",
                        detailParams, transaction: trans, commandType: CommandType.StoredProcedure);

                    trans.Commit();
                }

                var resultList = await conn.QueryAsync<JobEntity>(
                    "wspList_Job",
                    new { OID = job.ReferenceID },
                    commandType: CommandType.StoredProcedure);

                return resultList.LastOrDefault();
            }
            catch (Exception ex)
            {
                if (trans.Connection != null) trans.Rollback();
                throw new Exception($"Lỗi khi khởi tạo Job Change YC: {ex.Message}");
            }
        }

        public async Task<IEnumerable<WebContractDetailsExport>> getWebContractDetailsExport(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new DynamicParameters();
            parameters.Add("@oid", oid);

            return await conn.QueryAsync<WebContractDetailsExport>(
                "getWebContractDetailsExport",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> CheckIfSubmitted(string oid)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new { oid = oid };

            return await connection.ExecuteScalarAsync<bool>(
                "sp_CheckJobStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<object>> GetDocAttachFilesAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosDocument);
            const string sql = @"SELECT AttachFile as FileName, 
                                LinkFile as RelativePath, 
                                AttachNote as Note 
                         FROM [BosDocument].[dbo].[DocAttachfile] 
                         WHERE OID = @OID";

            var files = await conn.QueryAsync<dynamic>(sql, new { OID = oid });

            var baseUrl = _configuration["FileConfig:BaseUrl"]; // https://api-erprc.win-tech.vn/uploads
            return files.Select(f => new
            {
                f.FileName,
                f.Note,
                ViewUrl = $"{baseUrl}/{f.RelativePath}"
            });
        }

        public async Task<string> GetNextJobOIDAsync(string mainOid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            return await conn.QueryFirstOrDefaultAsync<string>(
                "dbo.sp_EContract_GetNextJobOID",
                new { MainOID = mainOid },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<string> InsertJobFullAsync(InsertJobRequest request)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);

            var parameters = new DynamicParameters();
            parameters.Add("@ReferenceID", request.ReferenceID);
            parameters.Add("@EntryID", request.EntryID);
            parameters.Add("@FactorID", request.FactorID);
            parameters.Add("@CmpnID", request.CmpnID ?? "26");
            parameters.Add("@OperDept", request.OperDept);
            parameters.Add("@Crt_User", request.Crt_User);
            parameters.Add("@CusTax", request.CusTax);
            parameters.Add("@CusName", request.CusName);
            parameters.Add("@EntryName", request.EntryName ?? "");
            parameters.Add("@Descrip", request.Descrip);
            parameters.Add("@ItemID", request.ItemID);
            parameters.Add("@InvcSign", request.InvcSign);
            parameters.Add("@InvcFrm", request.InvcFrm);
            parameters.Add("@InvcEnd", request.InvcEnd);
            parameters.Add("@ReferenceDate", request.ReferenceDate);
            parameters.Add("@ReferenceInfo", request.ReferenceInfo ?? "");
            parameters.Add("@InvcSample", request.InvcSample);
            parameters.Add("@FileInvoice", request.FileInvoice ?? ""); // Truyền link full
            parameters.Add("@FileOther", request.FileOther ?? "");     // Truyền link full

            return await conn.QueryFirstOrDefaultAsync<string>(
                "sp_EContract_InsertJob_Full_v2",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<JobStatusResponse> CheckJobStatusAsync(string referenceId, string factorId, string entryId)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            return await conn.QueryFirstOrDefaultAsync<JobStatusResponse>(
                "sp_EContract_CheckJobStatus",
                new { ReferenceID = referenceId, FactorID = factorId, EntryID = entryId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<object>> GetAttachmentsByOidAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosDocument);
            var baseUrl = _configuration["FileConfig:BaseUrl"];

            const string sql = @"
                SELECT AttachID, AttachFile as FileName, AttachNote as Note, 
                       LinkFile as RelativePath, AttachDate
                FROM [BosDocument].[dbo].[DocAttachfile]
                WHERE OID = @OID
                ORDER BY AttachDate DESC";

            var files = await conn.QueryAsync<dynamic>(sql, new { OID = oid });

            return files.Select(f => new
            {
                f.AttachID,
                f.FileName,
                f.Note,
                f.AttachDate,
                ViewUrl = $"{baseUrl}/{f.RelativePath}"
            });
        }

        public async Task<int> AddAttachmentsAsync(string oid, string factorId, string entryId, string user, string jsonAttachments)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new DynamicParameters();
            parameters.Add("@OID", oid);
            parameters.Add("@FactorID", factorId);
            parameters.Add("@EntryID", entryId);
            parameters.Add("@Crt_User", user);
            parameters.Add("@AttachmentJson", jsonAttachments);

            return await conn.ExecuteAsync("sp_EContract_AddAttachments", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<dynamic>> GetRawAttachmentsByOidAsync(string oid)
        {
            using var conn = _dbConnectionFactory.GetConnection(BosDocument);
            const string sql = @"
                SELECT AttachID, AttachFile, AttachNote, LinkFile, AttachDate 
                FROM [BosDocument].[dbo].[DocAttachfile] 
                WHERE OID = @OID 
                ORDER BY AttachDate DESC";

            return await conn.QueryAsync<dynamic>(sql, new { OID = oid });
        }

        public async Task<bool> InsertOrderBasicAsync(
            EContractIntegrationRequest model, string merchantId, string orderOid, string crtUser)
        {
            var connection = _dbConnectionFactory.GetConnection(BosOnline);

            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                var dt = new DataTable();
                dt.Columns.Add("ItemNo", typeof(int));      // 1
                dt.Columns.Add("OID", typeof(string));   // 2
                dt.Columns.Add("ItemID", typeof(string));   // 3
                dt.Columns.Add("ItemName", typeof(string));   // 4
                dt.Columns.Add("ItemUnit", typeof(string));   // 5
                dt.Columns.Add("ItemPrice", typeof(decimal));  // 6
                dt.Columns.Add("ItemQtty", typeof(decimal));  // 7
                dt.Columns.Add("ItemAmnt", typeof(decimal));  // 8
                dt.Columns.Add("VAT_Rate", typeof(decimal));  // 9
                dt.Columns.Add("VAT_Amnt", typeof(decimal));  // 10
                dt.Columns.Add("Sum_Amnt", typeof(decimal));  // 11
                dt.Columns.Add("Descrip", typeof(string));   // 12
                dt.Columns.Add("InvcSign", typeof(string));   // 13
                dt.Columns.Add("InvcFrm", typeof(int));      // 14
                dt.Columns.Add("InvcEnd", typeof(int));      // 15
                dt.Columns.Add("invcSample", typeof(string));   // 16
                dt.Columns.Add("itemUnitName", typeof(string));   // 17
                dt.Columns.Add("ItemPerBox", typeof(decimal));  // 18

                if (model.Details != null)
                {
                    int itemNo = 0;
                    foreach (var item in model.Details)
                    {
                        dt.Rows.Add(
                        itemNo++,       // ItemNo  — tự tăng
                        orderOid,       // OID
                        item.ItemID,
                        item.ItemName,
                        item.ItemUnit,
                        item.ItemPrice,
                        item.ItemQtty,
                        item.ItemAmnt,
                        item.VAT_Rate,
                        item.VAT_Amnt,
                        item.Sum_Amnt,
                        item.Descrip,
                        item.InvcSign,
                        item.InvcFrm,
                        item.InvcEnd,
                        item.InvcSample,
                        item.ItemUnitName,  
                        0               // ItemPerBox
                    );
                    }
                }

                var p = new DynamicParameters();

                // Thông tin công ty (Bên B)
                p.Add("@CmpnID", model.MyCmpnID);
                p.Add("@MerchantID", merchantId);
                p.Add("@OID", orderOid);
                p.Add("@FactorID", model.FactorID ?? "EContract");
                p.Add("@EntryID", model.EntryID ?? "EC:001");
                p.Add("@SaleEmID", model.SaleEmID);
                p.Add("@CmpnName", model.MyCmpnName);
                p.Add("@CmpnTax", model.MyCmpnTax);
                p.Add("@CmpnAddress", model.MyCmpnAddress);
                p.Add("@CmpnContactAddress", model.MyCmpnContactAddress ?? model.MyCmpnAddress);
                p.Add("@CmpnMail", model.MyCmpnMail);
                p.Add("@CmpnTel", model.MyCmpnTel);
                p.Add("@CmpnPeople_Sign", model.MyCmpnPeople_Sign);
                p.Add("@CmpnPosition_BySign", model.MyCmpnPosition_Sign);
                p.Add("@CmpnBankNumber", model.MyCmpnBankNumber);
                p.Add("@CmpnBankAddress", model.MyCmpnBankAddress);


                //Field ràng buộc not null
                p.Add("@RegionID", string.Empty);
                p.Add("@DscnAmnt", 0);
                p.Add("@CmpID_Sign", "");




                // Thông tin khách hàng (Bên A)
                p.Add("@CusName", model.CusName);
                p.Add("@CusTax", model.CusTax);
                p.Add("@CusAddress", model.CusAddress);
                p.Add("@CusEmail", model.CusEmail);
                p.Add("@CusTel", model.CusTel);
                p.Add("@CusPeople_Sign", model.CusPeople_Sign);
                p.Add("@CusPosition_BySign", model.CusPosition_BySign);
                p.Add("@CusBankNumber", model.CusBankNumber);
                p.Add("@CusBankAddress", model.CusBankAddress);

                // Tài chính
                p.Add("@PrdcAmnt", model.PrdcAmnt);
                p.Add("@VAT_Rate", model.VAT_Rate);
                p.Add("@VAT_Amnt", model.VAT_Amnt);
                p.Add("@Sum_Amnt", model.Sum_Amnt);
                p.Add("@SampleID", model.SampleID);
                p.Add("@Descrip", model.Descrip ?? "");
                p.Add("@Crt_User", crtUser);

                // ← Parameters mới
                p.Add("@ODate", model.ODate ?? DateTime.Now);
                p.Add("@SignDate", model.SignDate ?? DateTime.Now);
                p.Add("@HtmlContent", model.HtmlContent ?? "INCOM-MINI-APP");
                p.Add("@OidContract", model.OidContract);
                p.Add("@RefeContractDate", model.RefeContractDate);
                p.Add("@IsCapBu", model.IsCapBu);
                p.Add("@IsGiaHan", model.IsGiaHan);
                p.Add("@IsTT78", model.IsTT78);
                p.Add("@IsOnline", model.IsOnline);

                p.Add("@Details", dt.AsTableValuedParameter("dbo.EContractDetailType"));

                await connection.ExecuteAsync(
                    "[dbo].[sp_EContract_Insert_Basic]", p,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> OrderExistsAsync(string orderOid)
        {
            var connection = _dbConnectionFactory.GetConnection(BosOnline);

            var p = new DynamicParameters();
            p.Add("@OID", orderOid);

            var result = await connection.ExecuteScalarAsync<int>(
                "[dbo].[sp_EContract_CheckOrderExists]",
                p,
                commandType: CommandType.StoredProcedure
            );
            return result == 1;
        }

        public async Task<OwnerContract> GetOwnerContractAsync(string companyId = "26")
        {
            var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var result = await connection.QueryFirstOrDefaultAsync<OwnerContract>(
                "BosOnline..Check_OwnerContract",
                new { CmpnID = companyId },
                commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<bool> CheckOrderBySaleAsync(string cusTax, string saleEmID)
        {
            var connection = _dbConnectionFactory.GetConnection(BosOnline);
            var p = new DynamicParameters();
            p.Add("@CusTax", cusTax);
            p.Add("@SaleEmID", saleEmID);

            var result = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(1) 
                    FROM EContracts 
                    WHERE CusTax    = @CusTax 
                      AND Crt_User  = @SaleEmID",
                p);

            return result > 0;
        }

        public async Task<DeXuatCapTaiKhoanResult> DeXuatAsync(ProposeCreateAccount entity)
        {
            using var con = _dbConnectionFactory.GetConnection(BosOnline);
            var param = new DynamicParameters();
            param.Add("@OIDContract", entity.OIDContract);
            param.Add("@CmpnID", entity.CmpnID);
            param.Add("@Crt_User", entity.CrtUser);
            param.Add("@MailAcc", entity.MailAcc);

            IDictionary<string, object>? row = null;

            using var multi = await con.QueryMultipleAsync(
                "sp_DeXuatCapTaiKhoan",
                param,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 3600);

            while (!multi.IsConsumed)
            {
                var rows = (await multi.ReadAsync()).ToList();
                if (rows.Count == 0) continue;

                var candidate = (IDictionary<string, object>)rows[0];

                if (candidate.ContainsKey("OIDJob"))
                {
                    row = candidate;
                    break;
                }
            }

            if (row == null)
            {
                throw new InvalidOperationException(
                    $"SP không trả về kết quả OIDJob. Kiểm tra OIDContract '{entity.OIDContract}'.");
            }

            var rowCI = new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase);

            string Get(string key) =>
                rowCI.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "";

            // IsSuccess: 1 = thành công | 2 = đã tồn tại | 0 = lỗi
            string isSuccessRaw = Get("IsSuccess");
            int isSuccess = int.TryParse(isSuccessRaw, out var n) ? n : 0;

            if (isSuccess == 0)
            {
                string spMessage = Get("Message");
                if (string.IsNullOrEmpty(spMessage))
                    spMessage = $"SP thất bại. Raw row: [{string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"))}]";
                throw new InvalidOperationException(spMessage);
            }

            return new DeXuatCapTaiKhoanResult
            {
                OIDJob = Get("OIDJob"),
                ReferenceInfo = Get("Message"),
                IsAlreadyExists = isSuccess == 2
            };
        }

        public async Task<string?> GetMerchantIdAsync(string connectionString, string mst)
        {
            const string sql = @"
                SELECT TOP 1 MerchantId
                FROM BosEVAT..EVat_CompanyInfo WITH (NOLOCK)
                WHERE TaxNumber = @TaxNumber";

            using var conn = new SqlConnection(connectionString);
            var result = await conn.QueryFirstOrDefaultAsync<string>(
                sql, new { TaxNumber = mst });
            return result;
        }

        public async Task<InvCounterResult?> GetInvCounterAsync(string connectionString, string merchantId, DateTime frmDate, DateTime toDate)
        {
            using var conn = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@cmpnID", merchantId, DbType.String, ParameterDirection.Input);
            parameters.Add("@frmDate", frmDate.Date, DbType.Date, ParameterDirection.Input);
            parameters.Add("@toDate", toDate.Date, DbType.Date, ParameterDirection.Input);

            var result = await conn.QueryFirstOrDefaultAsync<InvCounterResult>(
                "BosEVAT..W_Status_InvCounter",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120); 
            return result;
        }

        public async Task<IEnumerable<EContractWaiting>> GetRawList101Async(string frmDate, string endDate)
        {
            using var connection = _dbConnectionFactory.GetConnection(BosOnline);

            var parameters = new DynamicParameters();
            parameters.Add("@Frm_date", frmDate);
            parameters.Add("@End_date", endDate);
            return await connection.QueryAsync<EContractWaiting>(
                "wspList_EContracts_ChoKiemTra_101",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<(IEnumerable<EContract_Monitor> Data, IEnumerable<SubEmpl> SubEmpl)> GetPagedAsync(string crtUser, string frm, string end, string? search, int? statusFilter, int page, int pageSize)
        {
            // Normalize tên công ty (giữ logic cũ)
            search = search switch
            {
                "CÔNG TY TNHH WIN TECH SOLUTION" => "WIN TECH",
                "CÔNG TY TNHH WIN ONLINE MEDIA" => "WIN ONLINE",
                _ => search
            };

            using var conn = _dbConnectionFactory.GetConnection(BosOnline);
            var parameters = new DynamicParameters();
            parameters.Add("@CrtUser", crtUser);
            parameters.Add("@Frm_date", frm);
            parameters.Add("@End_date", end);
            parameters.Add("@strSearch", search ?? "");
            parameters.Add("@StatusFilter", statusFilter);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            using var multi = await conn.QueryMultipleAsync(
                "wspList_EContracts_Paged",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            var data = (await multi.ReadAsync<EContract_Monitor>()).ToList();

            var subEmpl = !multi.IsConsumed
                ? (await multi.ReadAsync<SubEmpl>()).ToList()
                : new List<SubEmpl>();

            return (data, subEmpl);
        }
    }
}
