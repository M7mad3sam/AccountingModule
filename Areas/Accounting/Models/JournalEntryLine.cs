using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class JournalEntryLine
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid JournalEntryId { get; set; }
        public JournalEntry JournalEntry { get; set; }
        
        [Required]
        public Guid AccountId { get; set; }
        public Account Account { get; set; }
        
        public Guid? CostCenterId { get; set; }
        public CostCenter CostCenter { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        [Required]
        public decimal DebitAmount { get; set; }
        
        [Required]
        public decimal CreditAmount { get; set; }
        
        public Guid? TaxRateId { get; set; }
        public TaxRate TaxRate { get; set; }
        
        public decimal? TaxAmount { get; set; }
    }
}
