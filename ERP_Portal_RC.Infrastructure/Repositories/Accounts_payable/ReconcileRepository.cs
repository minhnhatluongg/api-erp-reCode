using Dapper;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Filters;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using ERP_Portal_RC.Domain.Interfaces;
using ERP_Portal_RC.Domain.Interfaces.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories.Accounts_payable
{
    public class ReconcileRepository : IReconcileRepository
    {
        private readonly IDbConnectionFactory _db;
        private const string BosOnline = "BosOnline";
        public ReconcileRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _db = dbConnectionFactory;
        }
        public async Task<long> CreateAsync(PaymentReconcile header, IEnumerable<PaymentReconcileDetail> details, CancellationToken ct = default)
        {
            // Mở connection + transaction thủ công vì cần nhiều SP trong 1 atomic op.
            using var conn = _db.GetConnection(BosOnline);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                // 1. Insert header — SP trả về ReconcileID mới
                var p = new DynamicParameters();
                p.Add("@ReconcileCode", header.ReconcileCode);
                p.Add("@ReconcileDate", header.ReconcileDate);
                p.Add("@ContractOID", header.ContractOID);
                p.Add("@InvoiceNumber", header.InvoiceNumber);
                p.Add("@ServiceTypeID", header.ServiceTypeID);
                p.Add("@WorkflowID", header.WorkflowID);
                p.Add("@CurrentStateID", header.CurrentStateID);
                p.Add("@PayerName", header.PayerName);
                p.Add("@PayerPhone", header.PayerPhone);
                p.Add("@SaleEmID", header.SaleEmID);
                p.Add("@SaleEmName", header.SaleEmName);
                p.Add("@TotalAmount", header.TotalAmount);
                p.Add("@PaidAmount", header.PaidAmount);
                p.Add("@RemainingAmount", header.RemainingAmount);
                p.Add("@PaymentMethod", header.PaymentMethod);
                p.Add("@BankAccount", header.BankAccount);
                p.Add("@TransferRef", header.TransferRef);
                p.Add("@TransferImagePath", header.TransferImagePath);
                p.Add("@TransferImageUrl", header.TransferImageUrl);
                p.Add("@IsGoodsChecked", header.IsGoodsChecked);
                p.Add("@Note", header.Note);
                p.Add("@Crt_User", header.Crt_User);
                p.Add("@NewID", dbType: DbType.Int64, direction: ParameterDirection.Output);

                await conn.ExecuteAsync(new CommandDefinition(
                    "sp_Reconcile_Create", p,
                    transaction: tran,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

                var newId = p.Get<long>("@NewID");

                // 2. Insert từng dòng detail
                foreach (var d in details)
                {
                    await conn.ExecuteAsync(new CommandDefinition(
                        "sp_ReconcileDetail_Add",
                        new
                        {
                            ReconcileID = newId,
                            d.ContractOID,
                            d.ContractItemNo,
                            d.ItemID,
                            d.ItemName,
                            d.CustomerID,
                            d.CustomerName,
                            d.CustomerTax,
                            d.InvoiceNumber,
                            d.InvoicingUnit,
                            d.OrderAmount,
                            d.PaidBeforeAmount,
                            d.PayingAmount,
                            d.RemainingAmount,
                            d.CommissionAmount,
                            d.LineStateID,
                            d.Note
                        },
                        transaction: tran,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));
                }

                // 3. Insert history dòng đầu tiên (state khởi tạo)
                await conn.ExecuteAsync(new CommandDefinition(
                    "sp_Reconcile_LogHistory",
                    new
                    {
                        ReconcileID = newId,
                        FromStateID = (int?)null,
                        ToStateID = header.CurrentStateID,
                        ActionUser = header.Crt_User ?? "system",
                        ActionNote = (string?)"Tạo phiếu"
                    },
                    transaction: tran,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

                tran.Commit();
                return newId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long reconcileId, string actionUser, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_Delete",
                new { ReconcileID = reconcileId, ActionUser = actionUser },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var rows = await conn.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<long> DuplicateAsync(long sourceReconcileId, string actionUser, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);

            var p = new DynamicParameters();
            p.Add("@SourceID", sourceReconcileId);
            p.Add("@ActionUser", actionUser);
            p.Add("@NewID", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(new CommandDefinition(
                "sp_Reconcile_Duplicate", p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

            return p.Get<long>("@NewID");
        }

        public async Task<string> GenerateNextCodeAsync(int serviceTypeId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_NextCode",
                new { ServiceTypeID = serviceTypeId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            return await conn.ExecuteScalarAsync<string>(cmd) ?? string.Empty;
        }

        public async Task<IReadOnlyList<WorkflowState>> GetAvailableActionsAsync(long reconcileId, string? role, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_GetAvailableActions",
                new { ReconcileID = reconcileId, Role = role },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var list = await conn.QueryAsync<WorkflowState>(cmd);
            return list.ToList();
        }

        public async Task<PaymentReconcile?> GetByCodeAsync(string reconcileCode, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_GetByCode",
                new { ReconcileCode = reconcileCode },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            return await conn.QueryFirstOrDefaultAsync<PaymentReconcile>(cmd);
        }

        public async Task<PaymentReconcile?> GetByIdAsync(long reconcileId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_GetById",
                new { ReconcileID = reconcileId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            return await conn.QueryFirstOrDefaultAsync<PaymentReconcile>(cmd);
        }

        public async Task<PaymentReconcile?> GetForPrintAsync(long reconcileId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_GetForPrint",
                new { ReconcileID = reconcileId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            // SP trả full header + details (không cần history khi in)
            using var multi = await conn.QueryMultipleAsync(cmd);

            var header = await multi.ReadFirstOrDefaultAsync<PaymentReconcile>();
            if (header is null) return null;

            header.Details = (await multi.ReadAsync<PaymentReconcileDetail>()).ToList();
            return header;
        }

        public async Task<PaymentReconcile?> GetFullAsync(string reconcileCode, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_GetFull",
                new { ReconcileCode = reconcileCode },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            // SP trả 3 result set:
            //   (1) Header         — PaymentReconcile
            //   (2) Details        — PaymentReconcileDetail[]
            //   (3) History        — PaymentStateHistory[]
            using var multi = await conn.QueryMultipleAsync(cmd);

            var header = await multi.ReadFirstOrDefaultAsync<PaymentReconcile>();
            if (header is null) return null;

            header.Details = (await multi.ReadAsync<PaymentReconcileDetail>()).ToList();
            header.History = (await multi.ReadAsync<PaymentStateHistory>()).ToList();

            return header;
        }

        public async Task<IReadOnlyList<PaymentStateHistory>> GetHistoryAsync(long reconcileId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_GetHistory",
                new { ReconcileID = reconcileId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var list = await conn.QueryAsync<PaymentStateHistory>(cmd);
            return list.ToList();
        }

        public async Task<bool> RecalcTotalsAsync(long reconcileId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_RecalcTotals",
                new { ReconcileID = reconcileId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var rows = await conn.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<PagedResult<PaymentReconcile>> SearchAsync(ReconcileFilter filter, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);

            var p = new
            {
                filter.Page,
                filter.Size,
                filter.StateCode,
                filter.ServiceTypeID,
                filter.SaleEmID,
                filter.ContractOID,
                filter.FromDate,
                filter.ToDate,
                filter.Keyword,
                filter.OrderBy
            };

            var cmd = new CommandDefinition(
                "sp_Reconcile_Search", p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            // SP trả 2 result set: (1) danh sách phiếu, (2) tổng số record
            using var multi = await conn.QueryMultipleAsync(cmd);
            var items = (await multi.ReadAsync<PaymentReconcile>()).ToList();
            var total = await multi.ReadFirstAsync<int>();

            return new PagedResult<PaymentReconcile>(items, total, filter.Page, filter.Size);
        }

        public async Task<bool> TransitionStateAsync(long reconcileId, int toStateId, string actionUser, string? note, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_Transition",
                new
                {
                    ReconcileID = reconcileId,
                    ToStateID = toStateId,
                    ActionUser = actionUser,
                    ActionNote = note
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            var rows = await conn.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<bool> UpdateHeaderAsync(PaymentReconcile header, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Reconcile_UpdateHeader",
                new
                {
                    header.ReconcileID,
                    header.PayerName,
                    header.PayerPhone,
                    header.SaleEmID,
                    header.SaleEmName,
                    header.TotalAmount,
                    header.PaymentMethod,
                    header.BankAccount,
                    header.TransferRef,
                    header.TransferImagePath,
                    header.TransferImageUrl,
                    header.IsGoodsChecked,
                    header.Note,
                    header.ChgeUser
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var rows = await conn.ExecuteAsync(cmd);
            return rows > 0;
        }
    }
}
