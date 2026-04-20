using Dapper;
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
    public class ReconcileDetailRepository : IReconcileDetailRepository
    {
        private readonly IDbConnectionFactory _db;
        private const string BosOnline = "BosOnline";
        public ReconcileDetailRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _db = dbConnectionFactory;
        }
        public async Task<long> AddAsync(PaymentReconcileDetail detail, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);

            var p = new DynamicParameters();
            p.Add("@ReconcileID", detail.ReconcileID);
            p.Add("@ContractOID", detail.ContractOID);
            p.Add("@ContractItemNo", detail.ContractItemNo);
            p.Add("@ItemID", detail.ItemID);
            p.Add("@ItemName", detail.ItemName);
            p.Add("@CustomerID", detail.CustomerID);
            p.Add("@CustomerName", detail.CustomerName);
            p.Add("@CustomerTax", detail.CustomerTax);
            p.Add("@InvoiceNumber", detail.InvoiceNumber);
            p.Add("@InvoicingUnit", detail.InvoicingUnit);
            p.Add("@OrderAmount", detail.OrderAmount);
            p.Add("@PaidBeforeAmount", detail.PaidBeforeAmount);
            p.Add("@PayingAmount", detail.PayingAmount);
            p.Add("@RemainingAmount", detail.RemainingAmount);
            p.Add("@CommissionAmount", detail.CommissionAmount);
            p.Add("@LineStateID", detail.LineStateID);
            p.Add("@Note", detail.Note);
            p.Add("@NewID", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(new CommandDefinition(
                "sp_ReconcileDetail_Add", p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

            return p.Get<long>("@NewID");
        }

        public async Task<int> BulkAddAsync(
        long reconcileId,
        IEnumerable<PaymentReconcileDetail> details,
        CancellationToken ct = default)
        {
            // Dùng Table-Valued Parameter — yêu cầu tạo TVP type 'dbo.PaymentReconcileDetailType' trong DB.
            var dt = BuildDetailTable(details);

            using var conn = _db.GetConnection(BosOnline);

            var p = new DynamicParameters();
            p.Add("@ReconcileID", reconcileId);
            p.Add("@Details", dt.AsTableValuedParameter("dbo.PaymentReconcileDetailType"));

            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_BulkAdd", p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<bool> DeleteAsync(long detailId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_Delete",
                new { DetailID = detailId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var rows = await conn.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<int> DeleteByReconcileAsync(long reconcileId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_DeleteByReconcile",
                new { ReconcileID = reconcileId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            return await conn.ExecuteAsync(cmd);
        }

        public async Task<IReadOnlyList<PaymentReconcileDetail>> GetByContractAsync(
        string contractOID, int? contractItemNo = null, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_GetByContract",
                new { ContractOID = contractOID, ContractItemNo = contractItemNo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var list = await conn.QueryAsync<PaymentReconcileDetail>(cmd);
            return list.ToList();
        }

        public async Task<PaymentReconcileDetail?> GetByIdAsync(long detailId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_GetById",
                new { DetailID = detailId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            return await conn.QueryFirstOrDefaultAsync<PaymentReconcileDetail>(cmd);
        }

        public async Task<IReadOnlyList<PaymentReconcileDetail>> GetByReconcileIdAsync(
        long reconcileId, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_GetByReconcile",
                new { ReconcileID = reconcileId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var list = await conn.QueryAsync<PaymentReconcileDetail>(cmd);
            return list.ToList();
        }

        public async Task<decimal> GetRemainingDebtByContractAsync(
        string contractOID, int? contractItemNo = null, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_Lookup_ContractDebt",
                new { ContractOID = contractOID, ContractItemNo = contractItemNo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            return await conn.ExecuteScalarAsync<decimal>(cmd);
        }

        public async Task<bool> UpdateAsync(PaymentReconcileDetail detail, CancellationToken ct = default)
        {
            using var conn = _db.GetConnection(BosOnline);
            var cmd = new CommandDefinition(
                "sp_ReconcileDetail_Update",
                new
                {
                    detail.DetailID,
                    detail.ItemName,
                    detail.OrderAmount,
                    detail.PaidBeforeAmount,
                    detail.PayingAmount,
                    detail.RemainingAmount,
                    detail.CommissionAmount,
                    detail.LineStateID,
                    detail.Note
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);
            var rows = await conn.ExecuteAsync(cmd);
            return rows > 0;
        }

        #region Helpers
        private static DataTable BuildDetailTable(IEnumerable<PaymentReconcileDetail> details)
        {
            var dt = new DataTable();
            dt.Columns.Add("ContractOID", typeof(string));
            dt.Columns.Add("ContractItemNo", typeof(int));
            dt.Columns.Add("ItemID", typeof(string));
            dt.Columns.Add("ItemName", typeof(string));
            dt.Columns.Add("CustomerID", typeof(string));
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("CustomerTax", typeof(string));
            dt.Columns.Add("InvoiceNumber", typeof(string));
            dt.Columns.Add("InvoicingUnit", typeof(string));
            dt.Columns.Add("OrderAmount", typeof(decimal));
            dt.Columns.Add("PaidBeforeAmount", typeof(decimal));
            dt.Columns.Add("PayingAmount", typeof(decimal));
            dt.Columns.Add("RemainingAmount", typeof(decimal));
            dt.Columns.Add("CommissionAmount", typeof(decimal));
            dt.Columns.Add("LineStateID", typeof(int));
            dt.Columns.Add("Note", typeof(string));

            foreach (var d in details)
            {
                dt.Rows.Add(
                    (object?)d.ContractOID ?? DBNull.Value,
                    (object?)d.ContractItemNo ?? DBNull.Value,
                    (object?)d.ItemID ?? DBNull.Value,
                    (object?)d.ItemName ?? DBNull.Value,
                    (object?)d.CustomerID ?? DBNull.Value,
                    (object?)d.CustomerName ?? DBNull.Value,
                    (object?)d.CustomerTax ?? DBNull.Value,
                    (object?)d.InvoiceNumber ?? DBNull.Value,
                    (object?)d.InvoicingUnit ?? DBNull.Value,
                    d.OrderAmount,
                    d.PaidBeforeAmount,
                    d.PayingAmount,
                    d.RemainingAmount,
                    d.CommissionAmount,
                    (object?)d.LineStateID ?? DBNull.Value,
                    (object?)d.Note ?? DBNull.Value
                );
            }
            return dt;
        }
        #endregion
    }
}
