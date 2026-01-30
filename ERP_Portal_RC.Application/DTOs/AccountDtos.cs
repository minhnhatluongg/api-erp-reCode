namespace ERP_Portal_RC.Application.DTOs
{
    /// <summary>
    /// DTO cho thông tin user cơ bản
    /// </summary>
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string LoginName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string UserPosition { get; set; } = string.Empty;
        public string LanguageDefault { get; set; } = string.Empty;
        public string Grp_List { get; set; } = string.Empty;
        public string CmpnID { get; set; } = string.Empty;
        public string DefaultAppSite { get; set; } = string.Empty;
    }
    public class UserDetailDto : UserDto
    {
        public string CmpnKey { get; set; } = string.Empty;
        public string AppvHost { get; set; } = string.Empty;
        public string AppvSite { get; set; } = string.Empty;
        public string CLN { get; set; } = string.Empty;
        public string ASM { get; set; } = string.Empty;
        public string SUB { get; set; } = string.Empty;
        public string TEAM { get; set; } = string.Empty;
        public string ZoneID { get; set; } = string.Empty;
        public string RegionID { get; set; } = string.Empty;
        public string ClnType { get; set; } = string.Empty;
        public string ClnID { get; set; } = string.Empty;
        public string ClnPath { get; set; } = string.Empty;
        public string CmpnID_List { get; set; } = string.Empty;
        public string OperDeptList { get; set; } = string.Empty;
        public int OSLogin { get; set; }
        public int AcssRght { get; set; }
    }

    
}
