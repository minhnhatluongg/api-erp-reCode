using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class AdjustConfigDto
    {
        public bool IsEmail { get; set; }
        public bool IsFax { get; set; }
        public bool IsSoDT { get; set; }
        public bool IsTaiKhoanNganHang { get; set; }
        public bool IsWebsite { get; set; }
        public bool IsSongNgu { get; set; }
        public bool IsThayDoiVien { get; set; }
        public VienConfig VienConfig { get; set; }
        public PosConfig LogoPos { get; set; }
        public PosConfig BackgroundPos { get; set; }
    }
    public class VienConfig
    {
        public string SelectedVien { get; set; }
        public decimal DoManh { get; set; }
    }

    public class PosConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
    }
}
