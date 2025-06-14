using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    // Keeping the more complete version from JournalEntryViewModels.cs
    // and removing the duplicate definitions from AccountingViewModels.cs

    public class AccountViewModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string NameEn { get; set; }

        public string NameAr { get; set; }

        public AccountType Type { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> AccountTypes { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> ParentAccounts { get; set; }
    }

    public class AccountCostCentersViewModel
    {
        public Guid AccountId { get; set; }

        public string AccountName { get; set; }

        [ValidateNever]
        public IEnumerable<AccountCostCenter> AccountCostCenters { get; set; }
        [ValidateNever]
        public IEnumerable<CostCenter> AvailableCostCenters { get; set; }
    }

    public class TrialBalanceFilterViewModel
    {
        public DateTime AsOfDate { get; set; }

        public Guid? CostCenterId { get; set; }

        public Guid? PeriodId { get; set; }

        public int Level { get; set; }
        [ValidateNever]
        public IEnumerable<CostCenter> CostCenters { get; set; }
        [ValidateNever]
        public IEnumerable<FiscalPeriod> Periods { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Levels { get; set; }
    }

    public class AssignAccountsVm
    {
        public Guid CostCenterId { get; set; }
        public List<SelectListItem> AvailableAccounts { get; set; }
        public List<SelectListItem> AssignedAccounts { get; set; }
        public List<Guid> SelectedIds { get; set; }
    }
}
