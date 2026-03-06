using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class DepartmentDTO
    {
        public string? DNAME { get; set; }
        public string? ParentID { get; set; }
        public string? DID { get; set; }
        public string? ROOM { get; set; }
        public string? id
        {
            get
            {
                return this.DID;
            }
        }
        public string? text
        {
            get
            {
                return this.ROOM;
            }
        }
    }
}
