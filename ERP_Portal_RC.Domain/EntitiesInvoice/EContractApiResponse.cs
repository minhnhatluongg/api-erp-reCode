using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesInvoice
{
    public class EContractApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public EContractData? Data { get; set; }
        public int StatusCode { get; set; }
    }

    public class EContractData
    {
        public EContracts? EContracts { get; set; }
        public List<EContractDetail>? EContractDetails { get; set; }
    }

    /// Thông tin hợp đồng - thông tin bên mua
    public class EContracts
    {
        /// <summary>
        /// OID hợp đồng → dùng làm invRef (mã tham chiếu duy nhất)
        /// </summary>
        public string? Oid { get; set; }

        /// <summary>Tên khách hàng (công ty)</summary>
        public string? CusName { get; set; }

        /// <summary>Người đại diện ký</summary>
        public string? CusPeople_Sign { get; set; }

        /// <summary>Chức vụ người ký</summary>
        public string? CusPosition_BySign { get; set; }

        /// <summary>Mã số thuế khách hàng</summary>
        public string? CusTax { get; set; }

        /// <summary>Địa chỉ khách hàng</summary>
        public string? CusAddress { get; set; }

        /// <summary>Email khách hàng</summary>
        public string? CusEmail { get; set; }

        /// <summary>Số điện thoại</summary>
        public string? CusTel { get; set; }

        /// <summary>Số tài khoản ngân hàng</summary>
        public string? CusBankNumber { get; set; }

        /// <summary>Tên ngân hàng</summary>
        public string? CusBankAddress { get; set; }

        /// <summary>Ngày ký hợp đồng → dùng làm invDate</summary>
        public DateTime? ODate { get; set; }
    }

    /// <summary>
    /// Chi tiết sản phẩm/dịch vụ trong hợp đồng
    /// NOTE: itemPrice trong response đã bao gồm VAT 8%
    /// </summary>
    public class EContractDetail
    {
        public string? Oid { get; set; }

        /// <summary>Mã sản phẩm</summary>
        public string? ItemID { get; set; }

        /// <summary>Tên sản phẩm</summary>
        public string? ItemName { get; set; }

        /// <summary>Đơn vị tính</summary>
        public string? ItemUnit { get; set; }
        public string? ItemUnitName { get; set; }

        /// <summary>
        /// Đơn giá (ĐÃ bao gồm VAT 8%)
        /// → Khi mapping sang invoice, cần chia cho (1 + vatRate/100)
        /// </summary>
        public decimal ItemPrice { get; set; }

        /// <summary>Số lượng</summary>
        public decimal ItemQtty { get; set; }

        /// <summary>Thành tiền (bao gồm VAT)</summary>
        public decimal ItemAmnt { get; set; }

        /// <summary>Thuế suất VAT (%)</summary>
        public decimal VaT_Rate { get; set; }

        /// <summary>Tiền thuế VAT</summary>
        public decimal VaT_Amnt { get; set; }

        /// <summary>Tổng tiền sau VAT</summary>
        public decimal Sum_Amnt { get; set; }

        /// <summary>Ký hiệu hóa đơn → invSerial</summary>
        public string? InvcSign { get; set; }

        /// <summary>Mẫu số hóa đơn → invName</summary>
        public int InvcFrm { get; set; }

        /// <summary>Số thứ tự dòng</summary>
        public int ItemNo { get; set; }

        public string? Descrip { get; set; }
        public string? InvcSample { get; set; }
    }
}
