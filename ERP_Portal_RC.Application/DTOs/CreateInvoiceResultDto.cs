using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class CreateInvoiceResultDto
    {
        public bool IsSuccess { get; set; }

        /// <summary>
        /// "Internal"   – lỗi hệ thống nội bộ
        /// "WinInvoice" – WinInvoice trả về isSuccess=false
        /// </summary>
        public string? ErrorSource { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public InvoiceDraftDataDto? Data { get; set; }
    }

    public class InvoiceDraftDataDto
    {
        /// <summary>OID nội bộ do WinInvoice phát sinh</summary>
        public string? Oid { get; set; }

        /// <summary>Số hóa đơn – nháp luôn trả "0000000"</summary>
        public string? InvCode { get; set; }

        /// <summary>Mã tham chiếu – chính là contractOid đã gửi lên</summary>
        public string? InvRef { get; set; }

        /// <summary>Ký hiệu hóa đơn (VD: C26TAT)</summary>
        public string? InvSerial { get; set; }

        /// <summary>Mẫu số + ký hiệu (VD: 1C26TAT)</summary>
        public string? InvName { get; set; }

        /// <summary>Ngày hóa đơn</summary>
        public string? InvDate { get; set; }

        /// <summary>Mã CQT cấp – null khi là nháp chưa ký</summary>
        public string? GovCode { get; set; }

        /// <summary>Đã truyền lên CQT chưa</summary>
        public string? GovTransfer { get; set; }

        /// <summary>Loại mẫu đã dùng</summary>
        public string? InvoiceType { get; set; }

        /// <summary>Trạng thái dễ đọc</summary>
        public string Status => InvCode == "0000000" ? "Nháp" : "Đã phát hành";
    }
    public enum InvoiceType
    {
        /// <summary>Đa thuế suất → 1C26TAT, invVatRate = -1</summary>
        Multi,
        /// <summary>Đơn thuế suất → 1C26TAA, invVatRate = thuế suất thực</summary>
        Single
    }

    public class CreateInvoiceFromContractDto
    {
        public string ContractOid { get; set; } = string.Empty;

        /// <summary>Loại mẫu hóa đơn. Default: Multi</summary>
        public InvoiceType InvoiceType { get; set; } = InvoiceType.Multi;
    }
}
