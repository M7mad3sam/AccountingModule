using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
        [ValidateNever]
        public CostCenter Parent { get; set; }
        [ValidateNever]
        public ICollection<CostCenter> Children { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(500)]
        public string? Description { get; set; }
        [ValidateNever]
        public ICollection<AccountCostCenter> AccountCostCenters { get; set; }
        [ValidateNever]
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; }
        
        public int Level { get; set; } = 0; // 0 represents root level
    }

    public enum CostCenterType
    {
        Company = 1,
        Branch = 2,
        Department = 3,
        Project = 4
    }
}
