namespace ERP_Portal_RC.Domain.Entities
{
    public class UserOnAp
    {
        public string? UserCode { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? LoginName { get; set; }
        public int OSLogin { get; set; }
        public int AcssRght { get; set; }
        public string? LanguageDefault { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Fax { get; set; }
        public string? Grp_List { get; set; }
        public string? CmpnID_List { get; set; }
        public string? PasswordEx { get; set; }
        public string? AppvHost { get; set; }
        public string? CmpnKey { get; set; }
        public string? AppvSite { get; set; }
        public string? CLN { get; set; }
        public string? ASM { get; set; }
        public string? SUB { get; set; }
        public string? TEAM { get; set; }
        public string? ZoneID { get; set; }
        public string? ZoneName { get; set; }
        public string? RegionID { get; set; }
        public string? ClnType { get; set; }
        public string? ClnID { get; set; }
        public string? ClnPath { get; set; }
        public string? OperDeptList { get; set; }
        public string? APIlogin { get; set; }
    }

    public class ApplicationToolMenu
    {
        public string MenuDscpt { get; set; } = string.Empty;
        public string MenuRIDs { get; set; } = string.Empty;
        public string AppID { get; set; } = string.Empty;
        public string ParentID { get; set; } = string.Empty;
        public string MenuID { get; set; } = string.Empty;
        public string MenuIcon { get; set; } = string.Empty;
        public string AcssForm { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public bool IsFunct { get; set; }
        public bool IsToolBar { get; set; }
        public string? MnCtType { get; set; }
        public string? AcssReport { get; set; }

        public int AcssRght { get; set; }
        public int ViewRght { get; set; }

        #region 50 parameters
        public string? Param01 { get; set; }
        public string? Param02 { get; set; }
        public string? Param03 { get; set; }
        public string? Param04 { get; set; }
        public string? Param05 { get; set; }
        public string? Param06 { get; set; }
        public string? Param07 { get; set; }
        public string? Param08 { get; set; }
        public string? Param09 { get; set; }
        public string? Param10 { get; set; }
        public string? Param11 { get; set; }
        public string? Param12 { get; set; }
        public string? Param13 { get; set; }
        public string? Param14 { get; set; }
        public string? Param15 { get; set; }
        public string? Param16 { get; set; }
        public string? Param17 { get; set; }
        public string? Param18 { get; set; }
        public string? Param19 { get; set; }
        public string? Param20 { get; set; }
        public string? Param21 { get; set; }
        public string? Param22 { get; set; }
        public string? Param23 { get; set; }
        public string? Param24 { get; set; }
        public string? Param25 { get; set; }
        public string? Param26 { get; set; }
        public string? Param27 { get; set; }
        public string? Param28 { get; set; }
        public string? Param29 { get; set; }
        public string? Param30 { get; set; }
        public string? Param31 { get; set; }
        public string? Param32 { get; set; }
        public string? Param33 { get; set; }
        public string? Param34 { get; set; }
        public string? Param35 { get; set; }
        public string? Param36 { get; set; }
        public string? Param37 { get; set; }
        public string? Param38 { get; set; }
        public string? Param39 { get; set; }
        public string? Param40 { get; set; }
        public string? Param41 { get; set; }
        public string? Param42 { get; set; }
        public string? Param43 { get; set; }
        public string? Param44 { get; set; }
        public string? Param45 { get; set; }
        public string? Param46 { get; set; }
        public string? Param47 { get; set; }
        public string? Param48 { get; set; }
        public string? Param49 { get; set; }
        public string? Param50 { get; set; }
        #endregion

        #region Variant 1->30
        public string? Variant01 { get; set; }
        public string? Variant02 { get; set; }
        public string? Variant03 { get; set; }
        public string? Variant04 { get; set; }
        public string? Variant05 { get; set; }
        public string? Variant06 { get; set; }
        public string? Variant07 { get; set; }
        public string? Variant08 { get; set; }
        public string? Variant09 { get; set; }
        public string? Variant10 { get; set; }
        public string? Variant11 { get; set; }
        public string? Variant12 { get; set; }
        public string? Variant13 { get; set; }
        public string? Variant14 { get; set; }
        public string? Variant15 { get; set; }
        public string? Variant16 { get; set; }
        public string? Variant17 { get; set; }
        public string? Variant18 { get; set; }
        public string? Variant19 { get; set; }
        public string? Variant21 { get; set; }
        public string? Variant22 { get; set; }
        public string? Variant23 { get; set; }
        public string? Variant24 { get; set; }
        public string? Variant25 { get; set; }
        public string? Variant26 { get; set; }
        public string? Variant27 { get; set; }
        public string? Variant28 { get; set; }
        public string? Variant29 { get; set; }
        public string? Variant30 { get; set; }
        #endregion
    }

    public class web_bosMenu_ByGroup
    {
        public bool isshow { get; set; } = false;
        public string? MenuDscpt { get; set; }
        public string? MenuRlDs { get; set; }
        public string? AppID { get; set; }
        public string? ParentID { get; set; }
        public string? MenuID { get; set; }
        public string? MenuIcon { get; set; }
        public bool IsGroup { get; set; }
        public bool IsFunct { get; set; }
        public bool InToolBar { get; set; }
        public string?MnCtType { get; set; }
        public string? AcssReport { get; set; }
        public string? AcssForm { get; set; }
        public string? Param01 { get; set; }
        public string? Param02 { get; set; }
        public string? Param03 { get; set; }
        public string? Param04 { get; set; }
        public string? Param05 { get; set; }
        public string? Param06 { get; set; }
        public string? Param07 { get; set; }
        public string? Param08 { get; set; }
        public string? Param09 { get; set; }
        public string? Param10 { get; set; }
        public string? Param11 { get; set; }
        public string? Param12 { get; set; }
        public string? Param13 { get; set; }
        public string? Param14 { get; set; }
        public string? Param15 { get; set; }
        public string? Param16 { get; set; }
        public string? Param17 { get; set; }
        public string? Param18 { get; set; }
        public string? Param19 { get; set; }
        public string? Param20 { get; set; }
        public string? Param21 { get; set; }
        public string? Param22 { get; set; }
        public string? Param23 { get; set; }
        public string? Param24 { get; set; }
        public string? Param25 { get; set; }
        public string? Param26 { get; set; }
        public string? Param27 { get; set; }
        public string? Param28 { get; set; }
        public string? Param29 { get; set; }
        public string? Param30 { get; set; }
        public string? Param31 { get; set; }
        public string? Param32 { get; set; }
        public string? Param33 { get; set; }
        public string? Param34 { get; set; }
        public string? Param35 { get; set; }
        public string? Param36 { get; set; }
        public string? Param37 { get; set; }
        public string? Param38 { get; set; }
        public string? Param39 { get; set; }
        public string? Param40 { get; set; }
        public string? Param41 { get; set; }
        public string? Param42 { get; set; }
        public string? Param43 { get; set; }
        public string? Param44 { get; set; }
        public string? Param45 { get; set; }
        public string? Param46 { get; set; }
        public string? Param47 { get; set; }
        public string? Param48 { get; set; }
        public string? Param49 { get; set; }
        public string? Param50 { get; set; }
        public int Unlock { get; set; }
        public int DeadLock { get; set; }
        public int Lock { get; set; }
        public int ViewNumb { get; set; }
        public int AcssRght { get; set; }
        public int ViewRght { get; set; }
        public string? Variant01 { get; set; }
        public string? Variant02 { get; set; }
        public string? Variant03 { get; set; }
        public string? Variant04 { get; set; }
        public string? Variant05 { get; set; }
        public string? Variant06 { get; set; }
        public string? Variant07 { get; set; }
        public string? Variant08 { get; set; }
        public string? Variant09 { get; set; }
        public string? Variant10 { get; set; }
        public string? Variant11 { get; set; }
        public string? Variant12 { get; set; }
        public string? Variant13 { get; set; }
        public string? Variant14 { get; set; }
        public string? Variant15 { get; set; }
        public string? Variant16 { get; set; }
        public string? Variant17 { get; set; }
        public string? Variant18 { get; set; }
        public string? Variant19 { get; set; }
        public string? Variant21 { get; set; }
        public string? Variant22 { get; set; }
        public string? Variant23 { get; set; }
        public string? Variant24 { get; set; }
        public string? Variant25 { get; set; }
        public string? Variant26 { get; set; }
        public string? Variant27 { get; set; }
        public string? Variant28 { get; set; }
        public string? Variant29 { get; set; }
        public string? Variant30 { get; set; }
    }
}
