using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class WebhookRepository : IWebhookRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline      = "BosOnline";
        private const string BosApproval    = "BosApproval";
        private const string BosControlEVAT = "BosControlEVAT";

        public WebhookRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        // ── Ghi log ──────────────────────────────────────────────────────────
        public async Task WriteLogAsync(WebhookLog log)
        {
            try
            {
                using var conn = _dbConnectionFactory.GetConnection(BosControlEVAT);
                await conn.ExecuteAsync(@"
                    INSERT INTO [dbo].[ECtr_WebhookLogs]
                        (EventType, ContractOid, InvoiceNo, InvoiceSign, InvoiceDate,
                         GovCode, SourceAction, RawPayload, ClientIp, Status, ErrorMessage, CreatedAt)
                    VALUES
                        (@EventType, @ContractOid, @InvoiceNo, @InvoiceSign, @InvoiceDate,
                         @GovCode, @SourceAction, @RawPayload, @ClientIp, @Status, @ErrorMessage, @CreatedAt)",
                    log);
            }
            catch
            {
                // Log thất bại không nên làm hỏng flow chính — nuốt lỗi tại đây
            }
        }

        // ── Nâng trạng thái 101 → 201 ────────────────────────────────────────
        public async Task<(bool Success, string Message)> AdvanceInvoiceExportedAsync(
            string contractOid, string userId = "WEBHOOK")
        {
            // 1. Tìm Job OID từ EContractJobs (ReferenceID = contractOid, JOB_00005)
            using var connOnline = _dbConnectionFactory.GetConnection(BosOnline);
            var jobOid = await connOnline.QueryFirstOrDefaultAsync<string?>(
                @"SELECT TOP 1 OID
                  FROM dbo.EContractJobs WITH (NOLOCK)
                  WHERE ReferenceID = @RefID AND FactorID = 'JOB_00005'
                  ORDER BY ODate DESC",
                new { RefID = contractOid });

            if (string.IsNullOrEmpty(jobOid))
                return (false, $"Không tìm thấy Job JOB_00005 cho hợp đồng '{contractOid}'. Cần tạo job trước.");

            // 2. Kiểm tra SignNumb hiện tại trong zsgn_EContractJobs
            using var connApproval = _dbConnectionFactory.GetConnection(BosApproval);

            var currentSign = await connApproval.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 SignNumb
                  FROM dbo.zsgn_EContractJobs WITH (NOLOCK)
                  WHERE Variant19 = @ContractOid
                    AND FactorID  = 'JOB_00005'
                    AND EntryID   = 'JB:010'
                  ORDER BY Crt_Date DESC",
                new { ContractOid = contractOid });

            if (currentSign == 201)
                return (false, $"Hợp đồng '{contractOid}' đã ở trạng thái 201 (đã xuất HĐĐT). Bỏ qua.");

            if (currentSign == null)
                return (false, $"Không tìm thấy bản ghi zsgn cho Job JOB_00005/JB:010 của '{contractOid}'.");

            if (currentSign != 101)
                return (false, $"Hợp đồng '{contractOid}' đang ở SignNumb={currentSign}, không phải 101. Không thể nâng lên 201.");

            // 3. Gọi zsgn_EContractJobs_NOR: 101 → 201
            if (connApproval.State == ConnectionState.Closed) connApproval.Open();
            using var trans = connApproval.BeginTransaction();
            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID",      "JOB_00005");
                p.Add("@OID",           jobOid);
                p.Add("@ODate",         DateTime.Now.ToString("yyyy-MM-dd"));
                p.Add("@CmpnID",        "26");
                p.Add("@Crt_User",      userId);
                p.Add("@DataTbl",       "EContractJobs");
                p.Add("@SignTble",      "zsgn_EContractJobs");
                p.Add("@SignChck",      0);
                p.Add("@holdSignNumb",  101);
                p.Add("@nextSignNumb",  201);
                p.Add("@AppvRouteGroup","");
                p.Add("@AppvRouteGrpTp",1);
                p.Add("@AppvMess",      "Webhook: Đã xuất Hóa Đơn Điện Tử thành công");
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant19",     contractOid);
                p.Add("@Variant26",     contractOid);
                p.Add("@Variant27",     "");
                p.Add("@Variant28",     "");
                p.Add("@Variant29",     "");
                p.Add("@Variant30",     "1");
                p.Add("@EntryID",       "JB:010");

                var result = await connApproval.QuerySingleAsync<dynamic>(
                    "zsgn_EContractJobs_NOR", p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();

                bool ok = (int)result.ExecValue == 1;
                return (ok, ok
                    ? $"Cập nhật SignNumb 201 thành công (Job: {jobOid})."
                    : $"zsgn_EContractJobs_NOR trả về thất bại (Job: {jobOid}).");
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi khi gọi zsgn_EContractJobs_NOR: {ex.Message}", ex);
            }
        }

        // ── Kế toán xem nháp → đảm bảo job tồn tại và ở SignNumb = 101 ────
        public async Task<(bool Success, string Message)> RequestInvoiceAsync(
            string contractOid, string userId = "WEBHOOK")
        {
            using var connOnline   = _dbConnectionFactory.GetConnection(BosOnline);
            using var connApproval = _dbConnectionFactory.GetConnection(BosApproval);

            // ── Bước 1: Kiểm tra trạng thái hiện tại ────────────────────────
            var currentSign = await connApproval.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 SignNumb
                  FROM dbo.zsgn_EContractJobs WITH (NOLOCK)
                  WHERE Variant19 = @OID AND FactorID = 'JOB_00005' AND EntryID = 'JB:010'
                  ORDER BY Crt_Date DESC",
                new { OID = contractOid });

            // Đã ở 101 hoặc 201 → idempotent
            if (currentSign == 101)
                return (true, $"Hợp đồng '{contractOid}' đã ở trạng thái 101 (đang chờ xuất). Không cần thao tác.");
            if (currentSign == 201)
                return (true, $"Hợp đồng '{contractOid}' đã xuất HĐĐT (SignNumb=201). Không cần thao tác.");

            // ── Bước 2: Tìm / tạo job JOB_00005 ────────────────────────────
            var jobOid = await connOnline.QueryFirstOrDefaultAsync<string?>(
                @"SELECT TOP 1 OID FROM dbo.EContractJobs WITH (NOLOCK)
                  WHERE ReferenceID = @OID AND FactorID = 'JOB_00005'
                  ORDER BY ODate DESC",
                new { OID = contractOid });

            if (string.IsNullOrEmpty(jobOid))
            {
                // Lấy thông tin hợp đồng để tạo job
                var contract = await connOnline.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT TOP 1 c.CusTax, c.CusName, c.CmpnID, c.Crt_User,
                             d.InvcSign, d.InvcFrm, d.InvcEnd, d.invcSample, d.ItemID
                      FROM dbo.EContracts c WITH (NOLOCK)
                      JOIN dbo.EContractDetails d WITH (NOLOCK) ON c.OID = d.OID
                      WHERE c.OID = @OID AND ISNULL(d.InvcSign,'') <> ''",
                    new { OID = contractOid });

                if (contract == null)
                    return (false, $"Không tìm thấy hợp đồng '{contractOid}' hoặc thiếu gói hóa đơn.");

                // Gọi sp_EContract_InsertJob_Full_v2 — tạo job VÀ đẩy 0→101 luôn
                var spParams = new DynamicParameters();
                spParams.Add("@ReferenceID",    contractOid);
                spParams.Add("@EntryID",        "JB:010");
                spParams.Add("@FactorID",       "JOB_00005");
                spParams.Add("@CmpnID",         (string)contract.CmpnID ?? "26");
                spParams.Add("@OperDept",       "");
                spParams.Add("@Crt_User",       userId);
                spParams.Add("@CusTax",         (string?)contract.CusTax ?? "");
                spParams.Add("@CusName",        (string?)contract.CusName ?? "");
                spParams.Add("@EntryName",      "Xuất hóa đơn HĐĐT");
                spParams.Add("@ItemID",         (string?)contract.ItemID ?? "");
                spParams.Add("@InvcSign",       (string?)contract.InvcSign ?? "");
                spParams.Add("@InvcFrm",        (int?)contract.InvcFrm ?? 0);
                spParams.Add("@InvcEnd",        (int?)contract.InvcEnd ?? 0);
                spParams.Add("@ReferenceDate",  DateTime.Now);
                spParams.Add("@ReferenceInfo",  $"Webhook: Kế toán yêu cầu xuất HĐĐT - {contractOid}");
                spParams.Add("@InvcSample",     (string?)contract.invcSample ?? "");
                spParams.Add("@FileInvoice",    "");
                spParams.Add("@FileOther",      "");
                spParams.Add("@Descrip",        "Webhook: Kế toán xem hóa đơn nháp");

                var spResult = await connOnline.QueryFirstOrDefaultAsync<dynamic>(
                    "dbo.sp_EContract_InsertJob_Full_v2",
                    spParams,
                    commandType: CommandType.StoredProcedure);

                string excStatus = spResult?.excStatus?.ToString() ?? "";
                if (!excStatus.StartsWith("1|"))
                    return (false, $"Tạo job thất bại: {excStatus}");

                return (true, $"Tạo job JOB_00005/JB:010 thành công, SignNumb=101. ({excStatus})");
            }

            // ── Bước 3: Job đã có, SignNumb = 0 → đẩy 0→101 ─────────────────
            if (connApproval.State == ConnectionState.Closed) connApproval.Open();
            using var trans = connApproval.BeginTransaction();
            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID",       "JOB_00005");
                p.Add("@OID",            jobOid);
                p.Add("@ODate",          DateTime.Now.ToString("yyyy-MM-dd"));
                p.Add("@CmpnID",         "26");
                p.Add("@Crt_User",       userId);
                p.Add("@DataTbl",        "EContractJobs");
                p.Add("@SignTble",       "zsgn_EContractJobs");
                p.Add("@SignChck",       0);
                p.Add("@holdSignNumb",   0);
                p.Add("@nextSignNumb",   101);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess",       "Webhook: Kế toán yêu cầu xuất hóa đơn điện tử");
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant19",      contractOid);
                p.Add("@Variant26",      contractOid);
                p.Add("@Variant27",      "");
                p.Add("@Variant28",      "");
                p.Add("@Variant29",      "");
                p.Add("@Variant30",      "1");
                p.Add("@EntryID",        "JB:010");

                var result = await connApproval.QuerySingleAsync<dynamic>(
                    "zsgn_EContractJobs_NOR", p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();
                bool ok = (int)result.ExecValue == 1;
                return (ok, ok
                    ? $"Nâng SignNumb 0→101 thành công (Job: {jobOid})."
                    : $"zsgn_EContractJobs_NOR thất bại (Job: {jobOid}).");
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi khi nâng 0→101: {ex.Message}", ex);
            }
        }
    }
}
