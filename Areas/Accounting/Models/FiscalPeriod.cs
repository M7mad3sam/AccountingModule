using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AspNetCoreMvcTemplate.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class FiscalPeriod
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Code { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public bool IsLocked { get; set; }
        
        public bool IsClosed { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public Guid FiscalYearId { get; set; }
        public FiscalYear FiscalYear { get; set; }
        
        public string CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; }
        
        public string ModifiedById { get; set; }
        public ApplicationUser ModifiedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        
        public string? ClosedById { get; set; }
        public ApplicationUser ClosedBy { get; set; }
        
        public DateTime? ClosedDate { get; set; }
        
        public ICollection<JournalEntry> JournalEntries { get; set; }
    }
}
