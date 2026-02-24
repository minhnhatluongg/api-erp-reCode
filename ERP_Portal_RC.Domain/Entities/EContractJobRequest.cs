namespace ERP_Portal_RC.Domain.Entities
{
    public class EContractJobRequest
    {
        /// <summary>Job OID. Thường để trống — SP tự sinh từ ReferenceID.</summary>
        public string OID { get; set; } = string.Empty;

        /// <summary>Contract OID (OID của EContract). BẮT BUỘC.</summary>
        public string ReferenceID { get; set; } = string.Empty;

        /// <summary>FactorID: JOB_00001 (chỉnh sửa mẫu), JOB_00002 (phát hành mẫu). BẮT BUỘC.</summary>
        public string FactorID { get; set; } = string.Empty;

        /// <summary>EntryID: JB:005 (chỉnh sửa mẫu), JB:004 (phát hành mẫu). BẮT BUỘC.</summary>
        public string EntryID { get; set; } = string.Empty;

        /// <summary>Mô tả / ghi chú yêu cầu. Tùy chọn.</summary>
        public string? Descrip { get; set; }

        /// <summary>URL file logo. Tùy chọn.</summary>
        public string? FileLogo { get; set; }

        /// <summary>URL file hóa đơn (XSLT). Tùy chọn.</summary>
        public string? FileInvoice { get; set; }

        /// <summary>URL file đính kèm khác. Tùy chọn.</summary>
        public string? FileOther { get; set; }

        /// <summary>Người tạo (userId). Tùy chọn — sẽ bị ghi đè bởi JWT UserCode.</summary>
        public string? Crt_User { get; set; }

        /// <summary>Email kế toán nhận thông báo. Tùy chọn.</summary>
        public string? MailAcc { get; set; }

        /// <summary>Thông tin tham chiếu bổ sung. Tùy chọn.</summary>
        public string? ReferenceInfo { get; set; }

        /// <summary>Mã ký hiệu hóa đơn (InvcSign). Tùy chọn.</summary>
        public string? InvcSign { get; set; }

        /// <summary>Số hóa đơn bắt đầu. Tùy chọn.</summary>
        public int? InvcFrm { get; set; }

        /// <summary>Số hóa đơn kết thúc. Tùy chọn.</summary>
        public int? InvcEnd { get; set; }

        /// <summary>Mã mẫu hóa đơn (invcSample). Tùy chọn.</summary>
        public string? invcSample { get; set; }
    }
    //JOB_00001	    JB:001		Tạo mẫu có sẵn	                            BosOnline.dboEContractJobs	BosOnline.dbo.EContractJobDetails
    //JOB_00001     JB:002		Tạo mẫu thiết kế                            BosOnline.dboEContractJobs  BosOnline.dbo.EContractJobDetails
    //JOB_00003     JB:003		Kích hoạt tài khoản sử dụng                 BosOnline.dboEContractJobs  BosOnline.dbo.EContractJobDetails
    //JOB_00002     JB:004		Phát hành hóa đơn                           BosOnline.dboEContractJobs  BosOnline.dbo.EContractJobDetails
    //JOB_00001     JB:005		Điều chỉnh mẫu                              BosOnline.dboEContractJobs  BosOnline.dbo.EContractJobDetails
}
