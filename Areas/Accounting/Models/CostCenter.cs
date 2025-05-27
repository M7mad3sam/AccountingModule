using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class CostCenter
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
        public CostCenterType Type { get; set; }
        
        public Guid? ParentId { get; set; }
        public CostCenter Parent { get; set; }
        
        public ICollection<CostCenter> Children { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<AccountCostCenter> AccountCostCenters { get; set; }
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; }
    }

    public enum CostCenterType
    {
        Company = 1,
        Branch = 2,
        Department = 3,
        Project = 4
    }
}
