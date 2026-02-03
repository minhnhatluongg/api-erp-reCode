using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Enum
{
    public class PublicEnum
    {
        public enum CurrSignNum
        {
            TRINH_KY = 0,
            TRA_VE = 100,
            TRA_VE200 = 200,
            TRA_VE300 = 300,
            TRA_VE500 = 500,
            CHO_KIEM_TRA = 101,
            CHO_GD_DUYEN = 201,
            HD_DA_DUYET = 301,
            KH_DA_KY = 501,
            HD_DONG = 1001,
        }
        public enum JobFactor
        {
            JOB_00001,
            JOB_00002,
            JOB_00003,
            JOB_00004,
            JOB_00005,
            JOB_00006,
            JOB_00007,
            JOB_00008
        }
        public static class UseFactorID
        {
            public const string
                EMinutes1 = "EMinutes1",
                EMinutes = "EMinutes",
                EContract = "EContract",
                EContractExt = "EContractExt",
                EContractExt1 = "EContractExt1";
        }
        public static class DebitStatus
        {
            public const string
                DONE = "done",
                INFINISH = "unfinish";
        }
        public static class TTStatus
        {
            public const string
                TT3_CHUACAP = "Chưa cấp tài khoản hệ thống",
                TT3_DACAP = "Đã cấp tài khoản hệ thống",
                TT4_CHUACAP = "Chưa phát hành hóa đơn",
                TT4_KHOANV = "Phát hành hóa đơn: Khóa nghiệp vụ",
                TT4_DACAP = "Phát hành hóa đơn: Thực hiện",
                TT2_THIETKE = "Mẫu KD thiết kế";
        }
        public static class ProductType
        {
            public const string
                HD3ben = "HĐ 3 bên",
                Model3ben = "3ben",
                Model2ben = "2ben",
                HD2ben = "HĐ 2 bên";

        }
        public static class EmailNoReply
        {
            public const string
                EMAIL = "noreply@win-tech.vn",
                PASSWORD = "^[H{l0V!(7fl",
                HOST = "mail.win-tech.vn";
        }
        public static class UserMaster
        {
            public const string
                UserCode = "%";
        }
        public static class JobEntry
        {
            public const string
                JB001 = "JB:001",
                JB002 = "JB:002",
                JB003 = "JB:003",
                JB004 = "JB:004",
                JB005 = "JB:005",
                JB006 = "JB:006",
                JB007 = "JB:007",
                JB008 = "JB:008",
                JB010 = "JB:010",
                JB011 = "JB:011",
                JB012 = "JB:012",
                JB013 = "JB:013",
                JB014 = "JB:014",
                JB015 = "JB:015";
        }
        public static class StatusSignnum
        {
            public const string
                TRINH_KY = "0",
                TRA_VE = "100",
                CHO_KIEM_TRA = "101",
                CHO_GD_DUYEN = "201",
                HD_DA_DUYET = "301",
                KH_DA_KY = "501",
                HD_DONG = "1001";
        }
        public static class OptionCompleteDoc
        {
            public const string
                CHUA_SOAN = "Chưa soạn xong",
                CHUA_GUI = "Chưa gửi yêu cầu";
        }
        public static class CompanyName
        {
            public const string
                MONET = "Công Ty TNHH Thương Mại Dịch Vụ Trực Tuyến Monet",
                WINTECH = "CÔNG TY TNHH WIN TECH SOLUTION";
        }
        public static class CompanyAddress
        {
            public const string
                WINTECH = "232/17 Cộng Hòa, Phường 12, Quận Tân Bình, TP.HCM",
                KHANHLINH = "232/17 Cộng Hòa, Phường 12, Quận Tân Bình, TP.HCM";
        }
        public static class SoftWare
        {
            public const string
                MONET = "WININVOICE", //"BOS'EVAT"
                DCV = "WININVOICE",
                KHANHLINH = "WININVOICE";
        }
        public static class InvoiceSample
        {
            public const string
                GTKT = "HÓA ĐƠN GTGT",
                GTTT = "HÓA ĐƠN BÁN HÀNG",
                VEDB = "VÉ VẬN TẢI ĐƯỜNG BỘ THEO PP KHẤU TRỪ",
                XKNB = "PHIẾU XUẤT KHO KIÊM VCNB";
        }
        public static class Characters
        {
            public const string
                Percent = "%";
        }
        public static class DocumentType
        {
            public const string
                HST = "HST",
                HSS = "HSS";
        }
        public static class DocumentService
        {
            public const string
                BANGHIEU = "banghieu",
                HDDT = "HDDT",
                HSBD = "HSBD",
                HSCQ = "HSCQ",
                DVKT = "DVKT",
                DANGBAO = "dangbao";
        }
        public static class ProcessNote
        {
            public const string
                NAME = "Chưa liên hệ",
                TYPE = "0001";
        }
        public static class MessageContent
        {
            public const string
                SUCCESSPAYTHEBILL = "Gửi yêu cầu thanh toán thành công!",
                SUCCESSCAPTK = "Gửi yêu cầu cấp tài khoản thành công!",
                FAILPAYTHEBILL = "Gửi yêu cầu thanh toán thất bại!",
                FAILCAPTK = "Gửi yêu cầu cấp tài khoản thất bại!",
                SUCCESS = "Thành Công!",
                SUCCESSCANCELNOTICE = "Báo hủy thành công!",
                FAILCANCELNOTICE = "Báo hủy thất bại!",
                FAILXuatHD = "Gửi yêu cầu Xuất HĐ thất bại!",
                SUCCESSXuatHD = "Gửi yêu cầu Xuất HĐ thành công!",
                SUCCESSAPPROVE = "Gửi yêu cầu thành công!",
                FAILAPPROVE = "Gửi yêu cầu thất bại!",
                FAIL = "Thất Bại!";
        }
        public static class Gender
        {
            public const string
                MALE = "Nam",
                FEMALE = "Nữ";
        }
        public static class UrlFile
        {
            public const string
                UrlOut = "./fileout",
                UrlUpload = "Uploads\\Upload",
                UrlTemplateFile = "Uploads\\Upload\\FileTemplate",
                UrlTemplateFileKHTGK = "Uploads\\Upload\\FileTemplateKHTGK",
                UrlTOutXSLT = "Uploads\\Upload\\XSLT\\out",
                UrlHS = "\\Uploads\\Upload\\HS\\";

        }
        public static class SessionKey
        {
            public const string
                SessionKeyName = "userName",
                SessionOID = "OID",
                AppSite = "",
                AppWeb = "";
        }
    }
}
