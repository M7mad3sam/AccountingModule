using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class FiscalYear
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
        
        public bool IsClosed { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<FiscalPeriod> FiscalPeriods { get; set; }
    }
}
