using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class InsertJobRequest
    {
        [Required]
        [StringLength(50)]
        public string ReferenceID { get; set; }

        [Required]
        [StringLength(50)]
        public string EntryID { get; set; }

        [Required]
        [StringLength(50)]
        public string FactorID { get; set; }

        [StringLength(10)]
        public string CmpnID { get; set; } = "26"; // Mặc định là '26' như trong SP

        [Required]
        [StringLength(50)]
        public string OperDept { get; set; }

        [Required]
        [StringLength(50)]
        public string Crt_User { get; set; }

        [Required]
        [StringLength(50)]
        public string CusTax { get; set; }

        [Required]
        [StringLength(500)]
        public string CusName { get; set; }

        [Required]
        [StringLength(250)]
        public string EntryName { get; set; }

        [Required]
        [StringLength(50)]
        public string ItemID { get; set; }

        [Required]
        [StringLength(50)]
        public string InvcSign { get; set; }

        [Required]
        public int InvcFrm { get; set; }

        [Required]
        public int InvcEnd { get; set; }

        [Required]
        public DateTime ReferenceDate { get; set; }

        [StringLength(500)]
        public string ReferenceInfo { get; set; }

        [Required]
        [StringLength(50)]
        public string InvcSample { get; set; }

        [StringLength(500)]
        public string FileInvoice { get; set; } = "";

        [StringLength(500)]
        public string FileOther { get; set; } = "";
    }
}
