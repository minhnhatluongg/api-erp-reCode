using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class MenuResponseViewDto
    {
        public class MenuDto
        {
            public string MenuID { get; set; } = string.Empty;
            public string ParentID { get; set; } = string.Empty;
            public string MenuDscpt { get; set; } = string.Empty;
            public string MenuIcon { get; set; } = string.Empty;
            public string? AcssForm { get; set; } = string.Empty;

            public Dictionary<string, string> Params { get; set; } = new();
            public Dictionary<string, string> Variants { get; set; } = new();

            public List<MenuDto> Children { get; set; } = new();
        }
        public class MenuResponseDto
        {
            public List<MenuDto> Menu { get; set; } = new();
            public string LinkInvc_In { get; set; } = string.Empty;
            public string LinkInvc { get; set; } = string.Empty;

            // Cờ phân quyền ứng dụng
            public bool IsBos { get; set; }
            public bool IsWINECONTRACT { get; set; }
            public bool IsINVOICE_IN { get; set; }
            public bool IsINVOICE { get; set; }

            // Cờ hiển thị giao diện (Mapping trực tiếp với isshowType, isshowMenu trong Vue)
            public bool IsShowMenu { get; set; } = true;
            public bool IsShowType { get; set; } // Dashboard thống kê
            public bool IsShowMenuE { get; set; }
            public bool IsShowSign { get; set; }
            public bool IsShowECLi { get; set; }

            // Điều hướng trực tiếp
            public bool IsDirect_In { get; set; }
            public bool IsDirect { get; set; }
            public bool IsManager { get; set; }
            public int TotalMenuItems { get; set; }
        }
    }
}
