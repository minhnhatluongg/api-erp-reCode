namespace ERP_Portal_RC.Domain.Entities
{
    public class SubEmpl
    {
        public string? EmployeeID { get; set; }
        public string? hoten_V { get; set; }
        public string? dutyname { get; set; }
        public string? PcNAME { get; set; }
        public DateTime Fromdate { get; set; }
        public DateTime Enddate { get; set; }
        public string id
        {
            get
            {
                return this.EmployeeID;
            }
        }
        public string text
        {
            get
            {
                return this.hoten_V;
            }
        }
    }
}