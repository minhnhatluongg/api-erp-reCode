using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Common;
using ERP_Portal_RC.Domain.Common.Filters;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using ERP_Portal_RC.Domain.Interfaces.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    //public class ReconcileService : IReconcileService
    //{
    //    private readonly IReconcileRepository _reconcileRepo;
    //    private readonly IReconcileDetailRepository _detailRepo;
    //    private readonly IWorkflowRepository _workflowRepo;
    //    private readonly IServiceTypeRepository _serviceTypeRepo;

    //    public ReconcileService(
    //        IReconcileRepository reconcileRepo,
    //        IReconcileDetailRepository detailRepo,
    //        IWorkflowRepository workflowRepo,
    //        IServiceTypeRepository serviceTypeRepo)
    //    {
    //        _reconcileRepo = reconcileRepo;
    //        _detailRepo = detailRepo;
    //        _workflowRepo = workflowRepo;
    //        _serviceTypeRepo = serviceTypeRepo;
    //    }
    //    public async Task<long> CreateAsync(
    //    CreateReconcileDto dto, string currentUser, CancellationToken ct = default)
    //    {
    //        if (string.IsNullOrWhiteSpace(currentUser))
    //            throw new ValidationAppException("Current user không xác định.");

    //        await ValidateCreateDtoAsync(dto, ct);

    //        // 1. Resolve workflow — nếu client không truyền thì lấy default của service type
    //        var workflowId = dto.WorkflowID;
    //        if (workflowId is null)
    //        {
    //            var defaultWf = await _workflowRepo.GetDefaultByServiceTypeAsync(dto.ServiceTypeID, ct)
    //                ?? throw new ConflictException(
    //                    "WORKFLOW_NOT_CONFIGURED",
    //                    $"ServiceType {dto.ServiceTypeID} chưa có workflow mặc định.");
    //            workflowId = defaultWf.WorkflowID;
    //        }

    //        // 2. Initial state (IsInitial=1) — phiếu mới luôn bắt đầu ở đây
    //        var initialState = await _workflowRepo.GetInitialStateAsync(workflowId.Value, ct)
    //            ?? throw new ConflictException(
    //                "INITIAL_STATE_NOT_FOUND",
    //                $"Workflow {workflowId} chưa khai báo state khởi tạo (IsInitial=1).");

    //        // 3. Sinh mã phiếu (sp_Reconcile_NextCode)
    //        var newCode = await _reconcileRepo.GenerateNextCodeAsync(dto.ServiceTypeID, ct);
    //        if (string.IsNullOrWhiteSpace(newCode))
    //            throw new ConflictException("CODE_GEN_FAILED", "Không sinh được mã phiếu mới.");

    //        // 4. Build entity
    //        var header = new PaymentReconcile
    //        {
    //            ReconcileCode = newCode,
    //            ReconcileDate = dto.ReconcileDate == default ? DateTime.Now : dto.ReconcileDate,
    //            ContractOID = dto.ContractOID,
    //            InvoiceNumber = dto.InvoiceNumber,
    //            ServiceTypeID = dto.ServiceTypeID,
    //            WorkflowID = workflowId.Value,
    //            CurrentStateID = initialState.StateID,
    //            PayerName = dto.PayerName,
    //            PayerPhone = dto.PayerPhone,
    //            SaleEmID = dto.SaleEmID,
    //            SaleEmName = dto.SaleEmName,
    //            TotalAmount = dto.TotalAmount,
    //            PaidAmount = dto.PaidAmount,
    //            RemainingAmount = dto.RemainingAmount,
    //            PaymentMethod = dto.PaymentMethod,
    //            BankAccount = dto.BankAccount,
    //            TransferRef = dto.TransferRef,
    //            TransferImagePath = dto.TransferImagePath,
    //            TransferImageUrl = dto.TransferImageUrl,
    //            IsGoodsChecked = dto.IsGoodsChecked,
    //            Note = dto.Note,
    //            Crt_User = currentUser,
    //            Crt_Date = DateTime.Now
    //        };

    //        var details = dto.Details.Select(d => new PaymentReconcileDetail
    //        {
    //            ContractOID = d.ContractOID ?? dto.ContractOID,
    //            ContractItemNo = d.ContractItemNo,
    //            ItemID = d.ItemID,
    //            ItemName = d.ItemName,
    //            CustomerID = d.CustomerID,
    //            CustomerName = d.CustomerName,
    //            CustomerTax = d.CustomerTax,
    //            InvoiceNumber = d.InvoiceNumber,
    //            InvoicingUnit = d.InvoicingUnit,
    //            OrderAmount = d.OrderAmount,
    //            PaidBeforeAmount = d.PaidBeforeAmount,
    //            PayingAmount = d.PayingAmount,
    //            RemainingAmount = d.RemainingAmount,
    //            CommissionAmount = d.CommissionAmount,
    //            LineStateID = d.LineStateID,
    //            Note = d.Note,
    //            Crt_Date = DateTime.Now
    //        }).ToList();

    //        // 5. Persist (transaction nằm trong repo)
    //        return await _reconcileRepo.CreateAsync(header, details, ct);
    //    }

    //    public async Task<bool> DeleteAsync(
    //    long reconcileId, string currentUser, CancellationToken ct = default)
    //    {
    //        if (string.IsNullOrWhiteSpace(currentUser))
    //            throw new ValidationAppException("Current user không xác định.");

    //        var existing = await GetByIdAsync(reconcileId, ct);

    //        var curState = await _workflowRepo.GetStateByIdAsync(existing.CurrentStateID, ct);
    //        if (curState is { IsFinal: true })
    //            throw new ConflictException(
    //                "DELETE_FORBIDDEN",
    //                $"Phiếu đang ở trạng thái kết thúc ({curState.Name}) — không thể huỷ.");

    //        return await _reconcileRepo.DeleteAsync(reconcileId, currentUser, ct);
    //    }

    //    public async Task<long> DuplicateAsync(
    //    long sourceReconcileId, string currentUser, CancellationToken ct = default)
    //    {
    //        if (string.IsNullOrWhiteSpace(currentUser))
    //            throw new ValidationAppException("Current user không xác định.");

    //        _ = await GetByIdAsync(sourceReconcileId, ct); // ensure tồn tại
    //        return await _reconcileRepo.DuplicateAsync(sourceReconcileId, currentUser, ct);
    //    }

    //    public Task<IReadOnlyList<WorkflowState>> GetAvailableActionsAsync(long reconcileId, string? role, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<PaymentReconcile> GetByCodeAsync(string reconcileCode, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<PaymentReconcile> GetByIdAsync(long reconcileId, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<PaymentReconcile> GetForPrintAsync(long reconcileId, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<PaymentReconcile> GetFullAsync(string reconcileCode, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<IReadOnlyList<PaymentStateHistory>> GetHistoryAsync(long reconcileId, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<bool> RecalcTotalsAsync(long reconcileId, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<PagedResult<PaymentReconcile>> SearchAsync(ReconcileFilter filter, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<bool> TransitionAsync(long reconcileId, TransitionStateDto dto, string currentUser, string? currentRole, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<bool> UpdateHeaderAsync(long reconcileId, UpdateReconcileHeaderDto dto, string currentUser, CancellationToken ct = default)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
