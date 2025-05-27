using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class FiscalPeriod
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public bool IsLocked { get; set; }
        
        public bool IsClosed { get; set; }
        
        public Guid FiscalYearId { get; set; }
        public FiscalYear FiscalYear { get; set; }
        
        public ICollection<JournalEntry> JournalEntries { get; set; }
    }
}
