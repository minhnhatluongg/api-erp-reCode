namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Snapshot toàn bộ trạng thái 1 hợp đồng — kết quả của SP sp_GetEContract_Summary.
    /// Đặt ở Domain.Entities vì IAdminContractSummaryRepository (Domain) cần reference.
    /// </summary>
    public class ContractSummaryResponse
    {
        public ContractInfoEntity?          Contract    { get; set; }
        public List<SignHistoryEntity>       SignHistory { get; set; } = new();
        public List<JobStatusSummaryEntity>  Jobs        { get; set; } = new();
        public List<TrackingEntity>          Tracking    { get; set; } = new();
        public PublicInfoEntity?             PublicInfo  { get; set; }
    }

    public class ContractInfoEntity
    {
        public string?   OID                { get; set; }
        public DateTime  ODate              { get; set; }
        public string?   CusName            { get; set; }
        public string?   CusTax             { get; set; }
        public string?   CusAddress         { get; set; }
        public string?   CusTel             { get; set; }
        public string?   CusEmail           { get; set; }
        public string?   CusPeople_Sign     { get; set; }
        public string?   CusPosition_BySign { get; set; }
        public string?   CmpnName           { get; set; }
        public string?   SaleEmID           { get; set; }
        public string?   SaleName           { get; set; }
        public string?   SampleID           { get; set; }
        public string?   Descript_Cus       { get; set; }
        public DateTime  Crt_Date           { get; set; }
        public string?   Crt_User           { get; set; }
        public DateTime? ChgeDate           { get; set; }
        public int       CurrSignNumb       { get; set; }
        public bool      IsTT78             { get; set; }
        public bool      IsGiaHan           { get; set; }
        public bool      IsCapBu            { get; set; }
    }

    public class SignHistoryEntity
    {
        public int       SignNumb    { get; set; }
        public string?   SignStatus  { get; set; }
        public DateTime? SignDate    { get; set; }
        public string?   SignByName  { get; set; }
        public string?   Crt_User   { get; set; }
        public string?   AppvMess   { get; set; }
        public string?   ExcHost    { get; set; }
    }

    public class JobStatusSummaryEntity
    {
        public string?   FactorID     { get; set; }
        public string?   EntryID      { get; set; }
        public string?   JobName      { get; set; }
        public int       SignNumb     { get; set; }
        public string?   JobStatus    { get; set; }
        public DateTime? SignDate     { get; set; }
        public string?   ActionByName { get; set; }
        public string?   AppvMess     { get; set; }
    }

    public class TrackingEntity
    {
        public string?   ActionType   { get; set; }
        public string?   ActionLabel  { get; set; }
        public string?   ActionByName { get; set; }
        public DateTime  ActionDate   { get; set; }
        public string?   Reason       { get; set; }
        public int?      PrevSignNumb { get; set; }
    }

    public class PublicInfoEntity
    {
        public string?   InvcCode         { get; set; }
        public DateTime? InvcDate         { get; set; }
        public string?   PrivateCode      { get; set; }
        public string?   Party_A_Name     { get; set; }
        public string?   Party_A_Taxcode  { get; set; }
        public string?   Party_B_Name     { get; set; }
        public string?   Party_B_Taxcode  { get; set; }
        public bool      Party_A_IsSigned { get; set; }
        public bool      Party_B_IsSigned { get; set; }
        public DateTime? PublicAt         { get; set; }
        public string?   SignedHost       { get; set; }
    }
}
