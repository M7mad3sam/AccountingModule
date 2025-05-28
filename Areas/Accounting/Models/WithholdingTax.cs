using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class WithholdingTax
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NameEn { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NameAr { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public decimal Rate { get; set; }
        
        public string ApplicableVendorTypes { get; set; }
        
        public decimal? MinimumThreshold { get; set; }
        
        [Required]
        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; }
    }
}
