using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EContract_Monitor_Refactor
    {
        //clone from EContract_Monitor
        public string? OID { get; set; }
        public DateTime ODATE { get; set; }
        public string? CusName { get; set; }
        public string? CusTax { get; set; }
        public string? CusPeople_Sign { get; set; }
        public string? CusPosition_BySign { get; set; }
        public string? DESCRIP { get; set; }
        public string? SaleEmID { get; set; }
        public int CurrSignNumb { get; set; }
        public int currSignNumbJobKT { get; set; }
        public DateTime Crt_Date { get; set; }
        public string? TT1 { get; set; }
        public string? TT2 { get; set; }
        public string? TT3 { get; set; }
        public string? TT4 { get; set; }
        public string? TT5 { get; set; }
        public string? TT6 { get; set; }
        public string? TT8 { get; set; }
        public string? EmplName { get; set; }
        public string? CmpnName { get; set; }
        public string? CmpnID { get; set; }
        public string? SiteName { get; set; }
        public string? Descript_Cus { get; set; }
        public string? CusAddress { get; set; }
        public string? Crt_User { get; set; }
        public bool isTT78 { get; set; } = false;
        public bool isTool { get; set; } = false;
        public bool isGiaHan { get; set; }
        public bool isCapBu { get; set; }

        // Trạng thái ký — lấy từ BosControlEVAT.dbo.ECtr_PublicInfo (join InvcCode = OID)
        /// <summary>true nếu OID đã có bản ghi trong ECtr_PublicInfo (đã ký).</summary>
        public bool IsSign { get; set; } = false;
        /// <summary>InvcCode bên ECtr_PublicInfo — chỉ có giá trị khi IsSign = true.</summary>
        public string? Public_InvcCode { get; set; }
        /// <summary>Ngày + giờ ký (Crt_Date của ECtr_PublicInfo) — null nếu chưa ký.</summary>
        public DateTime? Sign_Crt_Date { get; set; }
        /// <summary>Bên A đã ký?</summary>
        public bool Is_Party_A_Sign { get; set; } = false;
        /// <summary>Bên B đã ký?</summary>
        public bool Is_Party_B_Sign { get; set; } = false;

        //Paged
        public int TotalCount { get; set; }
        public int RowNum { get; set; }
    }
}
