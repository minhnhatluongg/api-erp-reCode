using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class EApprove
    {
        public string FactorID { get; set; } = "EContract";
        public string OID { get; set; }
        public bool Party_A_IsSigned { get; set; }
        public bool Party_B_IsSigned { get; set; }
        public DateTime ODate { get; set; }
        public string CmpnID { get; set; }
        public string Crt_User { get; set; }
        public string SaleEmpID { get; set; }
        public string DataTbl { get; set; }
        public string SignTble { get; set; }
        public int SignChck { get; set; } = 0;
        public int holdSignNumb { get; set; }
        public int nextSignNumb { get; set; }
        public string Variant12 { get; set; }
        public string Variant22 { get; set; }
        public string Variant23 { get; set; }
        public string Variant30 { get; set; } = "1";
        public string EntryID { get; set; }
        public string AppvMess { get; set; } = "Trình ký";
        public string CustomerID { get; set; }
        public string CusTax { get; set; }
        public string CmpnTax { get; set; }
        public string CmpnKey { get; set; }
        public string CsName { get; set; }
        public string InvcDate { get; set; }
        public string InvcSign { get; set; }
        public string InvcCode { get; set; }
        public string LinkHtml { get; set; }
        public string PrivateCode { get; set; }
        public string InvcHtml { get; set; }
        public string InvcXml { get; set; }
        public string InvcXslt { get; set; }
        public string SampleID { get; set; }
        public string ReferenceID { get; set; }
        public byte[] InvcContent { get; set; }
        public int AppvRouteGrpTp { get; set; } = 1;
        public string Party_A_Taxcode { get; set; }
        public string Party_B_Taxcode { get; set; }
        public string Party_A_Name { get; set; }
        public string Party_B_Name { get; set; }
        public string OIDJob { get; set; }
        public string EmailGD { get; set; }
        public string AppvMess_Html { get; set; } = "Trình ký";
        public string SaleEmail { get; set; }
        public int InvcFrm { get; set; }
        public int InvcEnd { get; set; }
        public string invcSample { get; set; }
        public string SaleName { get; set; }
        public string KTName { get; set; }
        public string UseFactorID { get; set; }
        public bool IsExtensionNoSample { get; set; }
        public bool IsPLHD { get; set; }
        public bool IsCapBu { get; set; }
        public bool IsGiaHan { get; set; }
        public bool IsDefauld { get; set; }
        public string Ression { get; set; }
        public bool isContractPaper { get; set; }
        public bool isHDTH { get; set; } = false;
        public string user { get; set; } = "minhnhatluong";
    }
}
