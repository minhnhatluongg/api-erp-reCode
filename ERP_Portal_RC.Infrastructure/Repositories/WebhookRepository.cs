using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class WebhookRepository : IWebhookRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnline = "BosOnline";
        private const string BosApproval = "BosApproval";
        private const string BosControlEVAT = "BosControlEVAT";

        public WebhookRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        // ── Nâng trạng thái 101 → 301 ────────────────────────────────────────
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

            if (currentSign == 301)
                return (false, $"Hợp đồng '{contractOid}' đã ở trạng thái 301 (đã xuất HĐĐT). Bỏ qua.");

            if (currentSign == null)
                return (false, $"Không tìm thấy bản ghi zsgn cho Job JOB_00005/JB:010 của '{contractOid}'.");

            if (currentSign != 101)
                return (false, $"Hợp đồng '{contractOid}' đang ở SignNumb={currentSign}, không phải 101. Không thể nâng lên 301.");

            // 3. Gọi zsgn_EContractJobs_NOR: 101 → 301
            if (connApproval.State == ConnectionState.Closed) connApproval.Open();
            using var trans = connApproval.BeginTransaction();
            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID", "JOB_00005");
                p.Add("@OID", jobOid);
                p.Add("@ODate", DateTime.Now.ToString("yyyy-MM-dd"));
                p.Add("@CmpnID", "26");
                p.Add("@Crt_User", userId);
                p.Add("@DataTbl", "EContractJobs");
                p.Add("@SignTble", "zsgn_EContractJobs");
                p.Add("@SignChck", 0);
                p.Add("@holdSignNumb", 101);
                p.Add("@nextSignNumb", 301);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess", "Webhook-App: Đã xuất Hóa Đơn Điện Tử thành công");
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant19", contractOid);
                p.Add("@Variant26", contractOid);
                p.Add("@Variant27", "");
                p.Add("@Variant28", "");
                p.Add("@Variant29", "");
                p.Add("@Variant30", "1");
                p.Add("@EntryID", "JB:010");

                var result = await connApproval.QuerySingleAsync<dynamic>(
                    "zsgn_EContractJobs_NOR", p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();

                bool ok = (int)result.ExecValue == 1;
                return (ok, ok
                    ? $"Cập nhật SignNumb 301 thành công (Job: {jobOid})."
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
            using var connOnline = _dbConnectionFactory.GetConnection(BosOnline);
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
            if (currentSign == 301)
                return (true, $"Hợp đồng '{contractOid}' đã xuất HĐĐT (SignNumb=301). Không cần thao tác.");

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
                {
                    // Không có gói hóa đơn → bypass EContractJobs
                    // Insert thẳng vào zsgn_EContractJobs với SignNumb=101
                    //
                    // QUAN TRỌNG: OID phải có suffix "-000" để SP tính COID đúng:
                    //   COID = LEFT(OID, LEN(OID)-4) → cắt "-000" → trả về contractOid
                    //   → JOIN a.OID = tt8.COID trong CTE sẽ match đúng
                    var jobOidNoPackage = contractOid + "-000";

                    if (connApproval.State == ConnectionState.Closed) connApproval.Open();

                    bool alreadyIn101 = await connApproval.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(1) FROM dbo.zsgn_EContractJobs WITH (NOLOCK)
                          WHERE Variant19 = @OID AND FactorID = 'JOB_00005'
                            AND EntryID = 'JB:010' AND SignNumb = 101",
                        new { OID = contractOid }) > 0;

                    if (alreadyIn101)
                        return (true, $"[NO_PACKAGE] Đã có dòng 101 cho '{contractOid}'.");

                    await connApproval.ExecuteAsync(
                        @"INSERT INTO dbo.zsgn_EContractJobs
                            (FactorID, OID, ODate, CmpnID, DataTbl,
                             SignNumb, SignDate, Crt_Date, Crt_User,
                             AppvRouteGroup, AppvRouteGrpTp, AppvMess, AppvMess_Html,
                             Variant19, Variant26, Variant30, EntryID)
                          VALUES
                            ('JOB_00005', @JobOid, GETDATE(), '26', 'EContractJobs',
                             101, GETDATE(), GETDATE(), @CrtUser,
                             '', 1, @AppvMess, @AppvMess,
                             @ContractOid, @ContractOid, '1', 'JB:010')",
                        new
                        {
                            JobOid      = jobOidNoPackage,  // contractOid + "-000"
                            ContractOid = contractOid,       // Variant19 = contract OID gốc
                            CrtUser     = userId,
                            AppvMess    = "[NO_PACKAGE] Webhook: Kế toán xem hóa đơn nháp (không có gói)"
                        });

                    return (true, $"[NO_PACKAGE] Đã insert dòng 101 cho '{contractOid}' (jobOid={jobOidNoPackage}).");
                }

                // Gọi sp_EContract_InsertJob_Full_v2 — tạo job VÀ đẩy 0→101 luôn
                var spParams = new DynamicParameters();
                spParams.Add("@ReferenceID", contractOid);
                spParams.Add("@EntryID", "JB:010");
                spParams.Add("@FactorID", "JOB_00005");
                spParams.Add("@CmpnID", (string)contract.CmpnID ?? "26");
                spParams.Add("@OperDept", "");
                spParams.Add("@Crt_User", userId);
                spParams.Add("@CusTax", (string?)contract.CusTax ?? "");
                spParams.Add("@CusName", (string?)contract.CusName ?? "");
                spParams.Add("@EntryName", "Xuất hóa đơn HĐĐT");
                spParams.Add("@ItemID", (string?)contract.ItemID ?? "");
                spParams.Add("@InvcSign", (string?)contract.InvcSign ?? "");
                spParams.Add("@InvcFrm", (int?)contract.InvcFrm ?? 0);
                spParams.Add("@InvcEnd", (int?)contract.InvcEnd ?? 0);
                spParams.Add("@ReferenceDate", DateTime.Now);
                spParams.Add("@ReferenceInfo", $"Webhook: Kế toán yêu cầu xuất HĐĐT - {contractOid}");
                spParams.Add("@InvcSample", (string?)contract.invcSample ?? "");
                spParams.Add("@FileInvoice", "");
                spParams.Add("@FileOther", "");
                spParams.Add("@Descrip", "Webhook: Kế toán xem hóa đơn nháp");

                await connOnline.ExecuteAsync(
                    "dbo.sp_EContract_InsertJob_Full_v2",
                    spParams,
                    commandType: CommandType.StoredProcedure);

                jobOid = await connOnline.QueryFirstOrDefaultAsync<string?>(
                    @"SELECT TOP 1 OID FROM dbo.EContractJobs WITH (NOLOCK)
                      WHERE ReferenceID = @OID AND FactorID = 'JOB_00005'
                      ORDER BY ODate DESC",
                    new { OID = contractOid });

                if (string.IsNullOrEmpty(jobOid))
                    return (false, $"Tạo job thất bại — không tìm thấy job trong EContractJobs sau khi chạy SP.");

                return (true, $"Tạo job JOB_00005/JB:010 thành công, SignNumb=101.");
            }

            // ── Bước 3: Job đã có, SignNumb = 0 → đẩy 0→101 ─────────────────
            if (connApproval.State == ConnectionState.Closed) connApproval.Open();
            using var trans = connApproval.BeginTransaction();
            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID", "JOB_00005");
                p.Add("@OID", jobOid);
                p.Add("@ODate", DateTime.Now.ToString("yyyy-MM-dd"));
                p.Add("@CmpnID", "26");
                p.Add("@Crt_User", userId);
                p.Add("@DataTbl", "EContractJobs");
                p.Add("@SignTble", "zsgn_EContractJobs");
                p.Add("@SignChck", 0);
                p.Add("@holdSignNumb", 0);
                p.Add("@nextSignNumb", 101);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess", "Webhook: Kế toán yêu cầu xuất hóa đơn điện tử");
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant19", contractOid);
                p.Add("@Variant26", contractOid);
                p.Add("@Variant27", "");
                p.Add("@Variant28", "");
                p.Add("@Variant29", "");
                p.Add("@Variant30", "1");
                p.Add("@EntryID", "JB:010");

                var result = await connApproval.QueryFirstOrDefaultAsync<dynamic>(
                    "zsgn_EContractJobs_NOR", p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();
                bool ok = SafeExecValue(result) == 1;
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

        // ── App đẩy HĐĐT đã xuất → tự động 0→101→201 nếu chưa có ────────────
        public async Task<(bool Success, string Message)> AdvanceInvoiceExportedFullAsync(
            string contractOid, string userId = "WEBHOOK")
        {
            using var connOnline = _dbConnectionFactory.GetConnection(BosOnline);
            using var connApproval = _dbConnectionFactory.GetConnection(BosApproval);

            // ── Bước 1: Trạng thái hiện tại ─────────────────────────────────
            var currentSign = await connApproval.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 SignNumb
                  FROM dbo.zsgn_EContractJobs WITH (NOLOCK)
                  WHERE Variant19 = @OID AND FactorID = 'JOB_00005' AND EntryID = 'JB:010'
                  ORDER BY Crt_Date DESC",
                new { OID = contractOid });

            if (currentSign == 301)
                return (true, $"Hợp đồng '{contractOid}' đã xuất HĐĐT (SignNumb=301). Idempotent.");

            // ── Bước 2: Tìm Job OID ──────────────────────────────────────────
            var jobOid = await connOnline.QueryFirstOrDefaultAsync<string?>(
                @"SELECT TOP 1 OID FROM dbo.EContractJobs WITH (NOLOCK)
                  WHERE ReferenceID = @OID AND FactorID = 'JOB_00005'
                  ORDER BY ODate DESC",
                new { OID = contractOid });

            // ── Bước 3: Nếu chưa có job → tạo job (SP tạo + 0→101 tự động) ─
            if (string.IsNullOrEmpty(jobOid))
            {
                var contract = await connOnline.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT TOP 1 c.CusTax, c.CusName, c.CmpnID, c.Crt_User,
                             d.InvcSign, d.InvcFrm, d.InvcEnd, d.invcSample, d.ItemID
                      FROM dbo.EContracts c WITH (NOLOCK)
                      JOIN dbo.EContractDetails d WITH (NOLOCK) ON c.OID = d.OID
                      WHERE c.OID = @OID AND ISNULL(d.InvcSign,'') <> ''",
                    new { OID = contractOid });

                if (contract == null)
                {
                    // Đơn KHÔNG CÓ GÓI HÓA ĐƠN → tạo job tối giản + INSERT thẳng 301
                    var contractBasic = await connOnline.QueryFirstOrDefaultAsync<ContractBasicInfo>(
                        "SELECT TOP 1 CusTax, CusName, CmpnID FROM dbo.EContracts WITH (NOLOCK) WHERE OID = @OID",
                        new { OID = contractOid });

                    if (contractBasic == null)
                        return (false, $"Không tìm thấy hợp đồng '{contractOid}' trong hệ thống.");

                    jobOid = await CreateMinimalJobAsync(
                        connOnline, contractOid, userId,
                        contractBasic.CusTax ?? "",
                        contractBasic.CusName ?? "",
                        contractBasic.CmpnID ?? "26");

                    if (string.IsNullOrEmpty(jobOid))
                        return (false, $"Tạo job tối giản thất bại cho '{contractOid}'.");

                    // INSERT trực tiếp 301 vào zsgn_EContractJobs (bỏ qua 0→101 trung gian)
                    if (connApproval.State == ConnectionState.Closed) connApproval.Open();
                    await connApproval.ExecuteAsync(
                        @"INSERT INTO dbo.zsgn_EContractJobs
                            (FactorID, OID, ODate, CmpnID, DataTbl,
                             SignNumb, SignDate, Crt_Date, Crt_User,
                             AppvRouteGroup, AppvRouteGrpTp, AppvMess, AppvMess_Html,
                             Variant19, Variant26, Variant30, EntryID)
                          VALUES
                            ('JOB_00005', @JobOid, @ODate, '26', 'EContractJobs',
                             301, @ODate, @ODate, @CrtUser,
                             '', 1, @AppvMess, @AppvMess,
                             @ContractOid, @ContractOid, '1', 'JB:010')",
                        new
                        {
                            JobOid = jobOid,
                            ODate = DateTime.Now,
                            CrtUser = userId,
                            AppvMess = "[NO_PACKAGE] Webhook: Đã xuất HĐĐT — hợp đồng không có gói",
                            ContractOid = contractOid
                        });

                    return (true, $"[NO_PACKAGE] Đã đánh dấu SignNumb=301 cho '{contractOid}' (không có gói hóa đơn). Job: {jobOid}");
                }

                var spParams = new DynamicParameters();
                spParams.Add("@ReferenceID", contractOid);
                spParams.Add("@EntryID", "JB:010");
                spParams.Add("@FactorID", "JOB_00005");
                spParams.Add("@CmpnID", (string)contract.CmpnID ?? "26");
                spParams.Add("@OperDept", "");
                spParams.Add("@Crt_User", userId);
                spParams.Add("@CusTax", (string?)contract.CusTax ?? "");
                spParams.Add("@CusName", (string?)contract.CusName ?? "");
                spParams.Add("@EntryName", "Xuất hóa đơn HĐĐT");
                spParams.Add("@ItemID", (string?)contract.ItemID ?? "");
                spParams.Add("@InvcSign", (string?)contract.InvcSign ?? "");
                spParams.Add("@InvcFrm", (int?)contract.InvcFrm ?? 0);
                spParams.Add("@InvcEnd", (int?)contract.InvcEnd ?? 0);
                spParams.Add("@ReferenceDate", DateTime.Now);
                spParams.Add("@ReferenceInfo", $"Webhook: App đẩy HĐĐT đã xuất - {contractOid}");
                spParams.Add("@InvcSample", (string?)contract.invcSample ?? "");
                spParams.Add("@FileInvoice", "");
                spParams.Add("@FileOther", "");
                spParams.Add("@Descrip", "Webhook: Tự động hoàn thành HĐĐT");

                await connOnline.ExecuteAsync(
                    "dbo.sp_EContract_InsertJob_Full_v2",
                    spParams,
                    commandType: CommandType.StoredProcedure);

                jobOid = await connOnline.QueryFirstOrDefaultAsync<string?>(
                    @"SELECT TOP 1 OID FROM dbo.EContractJobs WITH (NOLOCK)
                      WHERE ReferenceID = @OID AND FactorID = 'JOB_00005'
                      ORDER BY ODate DESC",
                    new { OID = contractOid });

                if (string.IsNullOrEmpty(jobOid))
                    return (false, "Tạo job thất bại — không tìm thấy job trong EContractJobs sau khi chạy SP.");

                // SP đã tạo 0→101, tiếp tục nâng 101→301 bên dưới
                // (jobOid đã được set từ re-query ở trên)
            } // end if (string.IsNullOrEmpty(jobOid))

            else if (currentSign == 0 || currentSign == null)
            {
                // ── Bước 3b: Có job nhưng chưa có 101 → nâng 0→101 ──────────
                var (ok101, msg101) = await CallNorAsync(connApproval, "JOB_00005", jobOid,
                    contractOid, userId, holdSign: 0, nextSign: 101,
                    appvMess: "Webhook: Tự động tạo yêu cầu xuất HĐĐT");

                if (!ok101)
                    return (false, $"Không thể nâng 0→101: {msg101}");
            }

            // ── Bước 4: Nâng 101→301 (hoàn thành) ──────────────────────────
            return await CallNorAsync(connApproval, "JOB_00005", jobOid!,
                contractOid, userId, holdSign: 101, nextSign: 301,
                appvMess: "Webhook: App xác nhận đã xuất Hóa Đơn Điện Tử thành công");
        }

        // ── Helper: đọc ExecValue an toàn từ dynamic kết quả SP ─────────────
        // Tránh lỗi "Cannot perform runtime binding on a null reference"
        // khi SP trả DBNull hoặc result là null.
        private static int SafeExecValue(dynamic? result)
        {
            if (result == null) return 0;
            try
            {
                var row = (IDictionary<string, object>)result;
                if (row.TryGetValue("ExecValue", out var ev)
                    && ev != null && ev != DBNull.Value)
                    return Convert.ToInt32(ev);
            }
            catch { }
            return 0;
        }

        // ── Helper: tạo job tối giản cho HĐ không có gói hóa đơn ───────────
        private static async Task<string?> CreateMinimalJobAsync(
            System.Data.IDbConnection conn,
            string contractOid,
            string userId,
            string cusTax,
            string cusName,
            string cmpnId)
        {
            // Sinh job OID kế tiếp (giống logic trong sp_EContract_InsertJob_Full_v2)
            var maxSuffix = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT MAX(CAST(RIGHT(OID, 3) AS INT))
                  FROM dbo.EContractJobs WITH (NOLOCK)
                  WHERE ReferenceID = @OID",
                new { OID = contractOid }) ?? 0;

            var jobOid = $"{contractOid}-{(maxSuffix + 1):D3}";

            await conn.ExecuteAsync(
                @"INSERT INTO dbo.EContractJobs
                    (CmpnID, OID, ODate, EntryID, FactorID, ReferenceID, ReferenceDate,
                     ReferenceInfo, OperDept, Descrip,
                     FileLogo, FileInvoice, FileOther,
                     exeDate, exeDeadLineDate,
                     PackID, InvcSign, InvcFrm, InvcEnd, invcSample,
                     MailAcc, Crt_User, Crt_Date)
                  VALUES
                    (@CmpnID, @OID, GETDATE(), 'JB:010', 'JOB_00005', @ReferenceID, GETDATE(),
                     @ReferenceInfo, '', @Descrip,
                     '', '', '',
                     '2019-11-20', GETDATE(),
                     '', '', 0, 0, '',
                     'ketoan.hoadonso@gmail.com', @Crt_User, GETDATE())",
                new
                {
                    CmpnID = cmpnId,
                    OID = jobOid,
                    ReferenceID = contractOid,
                    ReferenceInfo = $"{userId} yêu cầu Xuất hóa đơn HĐĐT {cusTax} - {cusName} [NO_PACKAGE]",
                    Descrip = "Webhook: Hợp đồng không có gói hóa đơn — tự động hoàn thành",
                    Crt_User = userId
                });

            return jobOid;
        }

        // ── Helper: gọi zsgn_EContractJobs_NOR ──────────────────────────────
        private static async Task<(bool Success, string Message)> CallNorAsync(
            System.Data.IDbConnection conn,
            string factorId, string jobOid,
            string contractOid, string userId,
            int holdSign, int nextSign,
            string appvMess)
        {
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var p = new DynamicParameters();
                p.Add("@FactorID", factorId);
                p.Add("@OID", jobOid);
                p.Add("@ODate", DateTime.Now.ToString("yyyy-MM-dd"));
                p.Add("@CmpnID", "26");
                p.Add("@Crt_User", userId);
                p.Add("@DataTbl", "EContractJobs");
                p.Add("@SignTble", "zsgn_EContractJobs");
                p.Add("@SignChck", 0);
                p.Add("@holdSignNumb", holdSign);
                p.Add("@nextSignNumb", nextSign);
                p.Add("@AppvRouteGroup", "");
                p.Add("@AppvRouteGrpTp", 1);
                p.Add("@AppvMess", appvMess);
                foreach (var i in Enumerable.Range(1, 25))
                    p.Add($"@Variant{i:D2}", "");
                p.Add("@Variant19", contractOid);
                p.Add("@Variant26", contractOid);
                p.Add("@Variant27", "");
                p.Add("@Variant28", "");
                p.Add("@Variant29", "");
                p.Add("@Variant30", "1");
                p.Add("@EntryID", "JB:010");

                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "zsgn_EContractJobs_NOR", p,
                    transaction: trans,
                    commandType: CommandType.StoredProcedure);

                trans.Commit();
                bool ok = SafeExecValue(result) == 1;
                return (ok, ok
                    ? $"Nâng {holdSign}→{nextSign} thành công (Job: {jobOid})."
                    : $"zsgn_EContractJobs_NOR thất bại {holdSign}→{nextSign} (Job: {jobOid}).");
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception($"Lỗi khi nâng {holdSign}→{nextSign}: {ex.Message}", ex);
            }
        }
    }
}

