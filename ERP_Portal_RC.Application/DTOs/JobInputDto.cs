using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class JobInputDto
    {
        public string FactorID { get; set; } = "JOB_00001"; 
        public string EntryID { get; set; }     
        public string? TemplateID { get; set; } 
        public string? OperDept { get; set; }   
        public bool IsDesignInvoices { get; set; } 
        public string? Crt_User { get; set; } 

        public string? FileOther { get; set; }  
        public string? FileName0 { get; set; }  
        public string? FileName1 { get; set; }  
        public string? FileName2 { get; set; }  
        public string? FileName3 { get; set; }  
        public string? FileName4 { get; set; }  
        public string? FileName5 { get; set; }  
        public string? FileName6 { get; set; }  

        public string? ChangeOption { get; set; } 
        public string? DescriptChange { get; set; } 
    }
}
