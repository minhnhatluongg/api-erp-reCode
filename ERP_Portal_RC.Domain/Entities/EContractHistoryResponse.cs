using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractHistoryResponse
    {
        /// <summary>Lịch sử quy trình ký hợp đồng (zsgn_webContracts).</summary>
        public List<HistoryItemDTO>     HistoryList  { get; set; } = new();
        /// <summary>Lịch sử duyệt các Job/Task (zsgn_EContractJobs).</summary>
        public List<JobHistoryItemDTO>  JobList      { get; set; } = new();
        /// <summary>Tracking chỉnh sửa: gỡ ký, edit, gửi lại (ECtr_ContractTrackingLog).</summary>
        public List<TrackingItemDTO>    TrackingList { get; set; } = new();
    }

    /// <summary>1 sự kiện trong lịch sử chỉnh sửa hợp đồng.</summary>
    public class TrackingItemDTO
    {
        public long     Id           { get; set; }
        public string?  ActionType   { get; set; }   // UNSIGN | EDIT | RESUBMIT
        public string?  ActionLabel  { get; set; }   // "Gỡ ký hợp đồng"...
        public string?  ActionByName { get; set; }
        public string?  Role         { get; set; }
        public DateTime ActionDate   { get; set; }
        public string?  Reason       { get; set; }
        public int?     PrevSignNumb { get; set; }
    }

    /// <summary>Mỗi bước duyệt của 1 Job/Task.</summary>
    public class JobHistoryItemDTO
    {
        public string?   JobOID      { get; set; }
        public string?   ContractOID { get; set; }
        public string?   FactorID    { get; set; }
        public string?   EntryID     { get; set; }
        public string?   EntryName   { get; set; }  
        public string?   SignNumb    { get; set; }  
        public string?   SignStatus  { get; set; }  
        public DateTime? SignDate    { get; set; }
        public DateTime? Crt_Date    { get; set; }
        public string?   FullName    { get; set; }
        public string?   AppvMess    { get; set; }
    }

    /// <summary>Lịch sử trình ký hợp đồng (từ zsgn_webContracts).</summary>
    public class HistoryItemDTO
    {
        public string?   OID         { get; set; }
        /// <summary>Số SignNumb gốc từ DB: 101, 201, 301, 501...</summary>
        public string?   CurrSignNum { get; set; }
        /// <summary>Text mô tả trạng thái: "Đề xuất ký", "Hợp đồng đã ký"...</summary>
        public string?   Status      { get; set; }
        public string?   AppvMess    { get; set; }
        public string?   FullName    { get; set; }
        public DateTime? Crt_Date    { get; set; }
        /// <summary>Ngày ký thực tế (SignDate). Có giá trị ở mốc 301 (kế toán ký) / 501 (khách ký).</summary>
        public DateTime? SignDate    { get; set; }
        public string?   ExcHost     { get; set; }
    }

    /// <summary>Tóm tắt Job — chỉ giữ field cần thiết cho hiển thị.</summary>
    public class JobSummaryDTO
    {
        public string?   OID           { get; set; }  // Job OID (VD: 000152/...-001)
        public string?   ReferenceID   { get; set; }  // Contract OID
        public string?   FactorID      { get; set; }  // JOB_00001 / JOB_00003 ...
        public string?   EntryID       { get; set; }  // JB:001 / JB:003 ...
        public string?   EntryName     { get; set; }  // Tên loại job
        public int       CurrSignNumb  { get; set; }  // Trạng thái hiện tại
        public string?   Descrip       { get; set; }  // Mô tả job
        public string?   OperDept      { get; set; }  // Bộ phận thực hiện
        public DateTime? Crt_Date      { get; set; }
        public string?   InvcSign      { get; set; }  // Ký hiệu hóa đơn
        public int       InvcFrm       { get; set; }  // Từ số
        public int       InvcEnd       { get; set; }  // Đến số
        public string?   InvcSample    { get; set; }  // Mẫu hóa đơn
        public string?   FileInvoice   { get; set; }  // File đính kèm hóa đơn
        public string?   FileOther     { get; set; }  // File đính kèm khác
        public bool      IsSubmit      { get; set; }
    }
}
