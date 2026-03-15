using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class JobStatusResponse
    {
        public string JobOID { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty; 
        public int CurrentStatus { get; set; }

        public bool IsPending => CurrentStatus > 0 && CurrentStatus < 200;
        public bool IsCompleted => CurrentStatus >= 200;

        public string StatusColor => CurrentStatus switch
        {
            0 => "danger",              
            > 0 and < 200 => "warning",  
            >= 200 => "success",         
            _ => "secondary"             
        };
    }
}
