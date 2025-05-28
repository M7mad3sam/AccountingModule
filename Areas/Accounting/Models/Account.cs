using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class Account
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
        public AccountType Type { get; set; }
        
        public Guid? ParentId { get; set; }
        public Account Parent { get; set; }
        
        public ICollection<Account> Children { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int Level { get; set; }
        
        public bool IsRetainedEarnings { get; set; }
        
        public ICollection<AccountCostCenter> AccountCostCenters { get; set; }
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; }
    }

    public enum AccountType
    {
        Asset = 1,
        Liability = 2,
        Equity = 3,
        Revenue = 4,
        Expense = 5
    }
}
