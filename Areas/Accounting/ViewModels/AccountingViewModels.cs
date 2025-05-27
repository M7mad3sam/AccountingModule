using System;
using System.Collections.Generic;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    public class AccountViewModel
    {
        public Guid Id { get; set; }
        
        public string Code { get; set; }
        
        public string NameEn { get; set; }
        
        public string NameAr { get; set; }
        
        public AccountType Type { get; set; }
        
        public Guid? ParentId { get; set; }
        
        public bool IsActive { get; set; }
        
        public IEnumerable<SelectListItem> AccountTypes { get; set; }
        
        public IEnumerable<SelectListItem> ParentAccounts { get; set; }
    }
    
    public class AccountCostCentersViewModel
    {
        public Guid AccountId { get; set; }
        
        public string AccountName { get; set; }
        
        public IEnumerable<AccountCostCenter> AccountCostCenters { get; set; }
        
        public IEnumerable<CostCenter> AvailableCostCenters { get; set; }
    }
    
    public class JournalEntryViewModel
    {
        public Guid Id { get; set; }
        
        public DateTime Date { get; set; }
        
        public string Description { get; set; }
        
        public string Reference { get; set; }
        
        public Guid FiscalPeriodId { get; set; }
        
        public IEnumerable<SelectListItem> FiscalPeriods { get; set; }
        
        public List<JournalEntryLineViewModel> Lines { get; set; } = new List<JournalEntryLineViewModel>();
        
        public decimal TotalDebit => Lines?.Sum(l => l.DebitAmount) ?? 0;
        
        public decimal TotalCredit => Lines?.Sum(l => l.CreditAmount) ?? 0;
        
        public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.0001m;
    }
    
    public class JournalEntryLineViewModel
    {
        public Guid Id { get; set; }
        
        public Guid AccountId { get; set; }
        
        public Guid? CostCenterId { get; set; }
        
        public string Description { get; set; }
        
        public decimal DebitAmount { get; set; }
        
        public decimal CreditAmount { get; set; }
        
        public Guid? TaxRateId { get; set; }
        
        public decimal? TaxAmount { get; set; }
        
        public IEnumerable<SelectListItem> Accounts { get; set; }
        
        public IEnumerable<SelectListItem> CostCenters { get; set; }
        
        public IEnumerable<SelectListItem> TaxRates { get; set; }
    }
    
    public class JournalEntryListViewModel
    {
        public IEnumerable<JournalEntry> JournalEntries { get; set; }
        
        public DateTime? FromDate { get; set; }
        
        public DateTime? ToDate { get; set; }
        
        public JournalEntryStatus? Status { get; set; }
        
        public int CurrentPage { get; set; }
        
        public int TotalPages { get; set; }
        
        public int TotalCount { get; set; }
        
        public IEnumerable<SelectListItem> StatusList { get; set; }
    }
    
    public class TrialBalanceFilterViewModel
    {
        public DateTime AsOfDate { get; set; }
        
        public Guid? CostCenterId { get; set; }
        
        public int Level { get; set; }
        [ValidateNever]
        public IEnumerable<CostCenter> CostCenters { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Levels { get; set; }
    }
}
