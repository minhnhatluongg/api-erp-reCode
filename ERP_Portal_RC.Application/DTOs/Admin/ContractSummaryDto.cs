namespace ERP_Portal_RC.Application.DTOs.Admin
{
    public class ContractSummaryDto
    {
        public ContractInfoDto?          Contract  { get; set; }
        public List<SignHistoryItemDto>  SignHistory { get; set; } = new();
        public List<JobStatusSummaryDto> Jobs       { get; set; } = new();
        public List<TrackingItemDto2>    Tracking   { get; set; } = new();
        public PublicInfoDto?            PublicInfo { get; set; }
    }

    public class ContractInfoDto
    {
        public string?   OID               { get; set; }
        public DateTime  ODate             { get; set; }
        public string?   CusName           { get; set; }
        public string?   CusTax            { get; set; }
        public string?   CusAddress        { get; set; }
        public string?   CusTel            { get; set; }
        public string?   CusEmail          { get; set; }
        public string?   CusPeople_Sign    { get; set; }
        public string?   CusPosition_BySign { get; set; }
        public string?   CmpnName          { get; set; }
        public string?   SaleEmID          { get; set; }
        public string?   SaleName          { get; set; }
        public string?   SampleID          { get; set; }
        public string?   Descript_Cus      { get; set; }
        public DateTime  Crt_Date          { get; set; }
        public string?   Crt_User          { get; set; }
        public DateTime? ChgeDate          { get; set; }
        public int       CurrSignNumb      { get; set; }
        public bool      IsTT78            { get; set; }
        public bool      IsGiaHan          { get; set; }
        public bool      IsCapBu           { get; set; }
    }

    public class SignHistoryItemDto
    {
        public int       SignNumb    { get; set; }
        public string?   SignStatus  { get; set; }
        public DateTime? SignDate    { get; set; }
        public string?   SignByName  { get; set; }
        public string?   Crt_User   { get; set; }
        public string?   AppvMess   { get; set; }
        public string?   ExcHost    { get; set; }
    }

    public class JobStatusSummaryDto
    {
        public string?   FactorID      { get; set; }
        public string?   EntryID       { get; set; }
        public string?   JobName       { get; set; }
        public int       SignNumb      { get; set; }
        public string?   JobStatus     { get; set; }
        public DateTime? SignDate      { get; set; }
        public string?   ActionByName  { get; set; }
        public string?   AppvMess      { get; set; }
    }

    public class TrackingItemDto2
    {
        public string?   ActionType    { get; set; }
        public string?   ActionLabel   { get; set; }
        public string?   ActionByName  { get; set; }
        public DateTime  ActionDate    { get; set; }
        public string?   Reason        { get; set; }
        public int?      PrevSignNumb  { get; set; }
    }

    public class PublicInfoDto
    {
        public string?   InvcCode       { get; set; }
        public DateTime? InvcDate       { get; set; }
        public string?   PrivateCode    { get; set; }
        public string?   Party_A_Name   { get; set; }
        public string?   Party_A_Taxcode { get; set; }
        public string?   Party_B_Name   { get; set; }
        public string?   Party_B_Taxcode { get; set; }
        public bool      Party_A_IsSigned { get; set; }
        public bool      Party_B_IsSigned { get; set; }
        public DateTime? PublicAt       { get; set; }
        public string?   SignedHost     { get; set; }
    }
}
