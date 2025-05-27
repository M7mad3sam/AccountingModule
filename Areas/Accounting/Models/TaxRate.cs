using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class TaxRate
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
        
        [Required]
        public decimal Rate { get; set; }
        
        [Required]
        public TaxType Type { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; }
    }

    public enum TaxType
    {
        VAT = 1,
        Withholding = 2
    }
}
