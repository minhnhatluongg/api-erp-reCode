using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class RegistertrationCodes
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public string? UsedByEmail { get; set; }
        public DateTime? UsedAt { get; set; }
        public virtual TechnicalUser CreatedByUser { get; set; } = null!;
    }
}
