using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using System.Linq;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    public class JournalEntryViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [Display(Name = "Entry Date")]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; } = DateTime.Today;
        
        [Required]
        [Display(Name = "Posting Date")]
        [DataType(DataType.Date)]
        public DateTime PostingDate { get; set; } = DateTime.Today;
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Reference")]
        public string Reference { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Status")]
        public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
        
        [Display(Name = "Client")]
        public Guid? ClientId { get; set; }
        
        [Display(Name = "Vendor")]
        public Guid? VendorId { get; set; }
        
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "EGP";
        
        [Display(Name = "Exchange Rate")]
        [Range(0.01, 1000)]
        public decimal ExchangeRate { get; set; } = 1;
        
        [Display(Name = "Is Recurring")]
        public bool IsRecurring { get; set; }
        
        [Display(Name = "Recurrence Pattern")]
        public string RecurrencePattern { get; set; }
        
        [Display(Name = "Next Recurrence Date")]
        [DataType(DataType.Date)]
        public DateTime? NextRecurrenceDate { get; set; }
        
        [Display(Name = "End Recurrence Date")]
        [DataType(DataType.Date)]
        public DateTime? EndRecurrenceDate { get; set; }
        
        [Display(Name = "Is System Generated")]
        public bool IsSystemGenerated { get; set; }
        
        [Display(Name = "Source Document")]
        public string SourceDocument { get; set; }
        
        [Display(Name = "Attachment")]
        public string AttachmentUrl { get; set; }
        
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        [Display(Name = "Fiscal Period")]
        public Guid FiscalPeriodId { get; set; }
        
        public List<JournalEntryLineViewModel> Lines { get; set; } = new List<JournalEntryLineViewModel>();
        
        public IEnumerable<SelectListItem> AvailableClients { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableAccounts { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableCostCenters { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableTaxRates { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableWithholdingTaxes { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableFiscalPeriods { get; set; } = new List<SelectListItem>();
        
        [Display(Name = "Total Debit")]
        public decimal TotalDebit => Lines.Sum(l => l.Debit);
        
        [Display(Name = "Total Credit")]
        public decimal TotalCredit => Lines.Sum(l => l.Credit);
        
        [Display(Name = "Is Balanced")]
        public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
    }
    
    public class JournalEntryLineViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [Display(Name = "Account")]
        public Guid AccountId { get; set; }
        
        [Display(Name = "Cost Center")]
        public Guid? CostCenterId { get; set; }
        
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Debit")]
        [Range(0, 9999999999.99)]
        public decimal Debit { get; set; }
        
        [Display(Name = "Credit")]
        [Range(0, 9999999999.99)]
        public decimal Credit { get; set; }
        
        [Display(Name = "Tax Rate")]
        public Guid? TaxRateId { get; set; }
        
        [Display(Name = "Tax Amount")]
        public decimal? TaxAmount { get; set; }
        
        [Display(Name = "Withholding Tax")]
        public Guid? WithholdingTaxId { get; set; }
        
        [Display(Name = "Withholding Tax Amount")]
        public decimal? WithholdingTaxAmount { get; set; }
        
        public IEnumerable<SelectListItem> AvailableAccounts { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableCostCenters { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableTaxRates { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableWithholdingTaxes { get; set; } = new List<SelectListItem>();
    }
    
    public class JournalEntryListViewModel
    {
        public IEnumerable<JournalEntry> JournalEntries { get; set; }
        public string SearchTerm { get; set; }
        public JournalEntryStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? ClientId { get; set; }
        public Guid? VendorId { get; set; }
        public bool? IsRecurring { get; set; }
        public bool? IsSystemGenerated { get; set; }
        
        public IEnumerable<SelectListItem> AvailableClients { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
    }
    
    public class JournalEntryApprovalViewModel
    {
        public Guid Id { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime PostingDate { get; set; }
        public JournalEntryStatus Status { get; set; }
        public string ApprovalNotes { get; set; }
        public decimal TotalAmount { get; set; }
        public string ClientName { get; set; }
        public string VendorName { get; set; }
        public List<JournalEntryLineViewModel> Lines { get; set; }
    }
    
    public class RecurringJournalEntryViewModel
    {
        public Guid Id { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public string RecurrencePattern { get; set; }
        public DateTime? NextRecurrenceDate { get; set; }
        public DateTime? EndRecurrenceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
