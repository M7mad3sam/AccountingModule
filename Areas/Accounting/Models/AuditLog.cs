using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AspNetCoreMvcTemplate.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string EntityName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string EntityId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Action { get; set; }
        
        public string OldValues { get; set; }
        
        public string NewValues { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [StringLength(50)]
        public string IpAddress { get; set; }
    }
}
