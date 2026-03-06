using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class EContractDetailItemDto
    {
        public int InvcEnd { get; set; }      
        public int ItemPerBox { get; set; }   
        public int sl_KM { get; set; }        
        public bool IsKM { get; set; }         
    }
}
