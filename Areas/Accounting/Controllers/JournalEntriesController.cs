using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant")]
    public class JournalEntriesController : Controller
    {
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IChartOfAccountsService _chartOfAccountsService;
        private readonly ICostCenterService _costCenterService;
        private readonly IClientVendorService _clientVendorService;
        private readonly ITaxService _taxService;
        private readonly IPeriodManagementService _periodManagementService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<JournalEntriesController> _localizer;

        public JournalEntriesController(
            IGeneralLedgerService generalLedgerService,
            IChartOfAccountsService chartOfAccountsService,
            ICostCenterService costCenterService,
            IClientVendorService clientVendorService,
            ITaxService taxService,
            IPeriodManagementService periodManagementService,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizer<JournalEntriesController> localizer)
        {
            _generalLedgerService = generalLedgerService;
            _chartOfAccountsService = chartOfAccountsService;
            _costCenterService = costCenterService;
            _clientVendorService = clientVendorService;
            _taxService = taxService;
            _periodManagementService = periodManagementService;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string searchTerm = null, 
            JournalEntryStatus? status = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            Guid? clientId = null, 
            Guid? vendorId = null, 
            bool? isRecurring = null, 
            bool? isSystemGenerated = null)
        {
            var journalEntries = await _generalLedgerService.GetJournalEntriesAsync(
                searchTerm, status, fromDate, toDate, clientId, vendorId, isRecurring, isSystemGenerated);
            
            var clients = await _clientVendorService.GetClientSelectListAsync(isActive: true);
            var vendors = await _clientVendorService.GetVendorSelectListAsync(isActive: true);
            
            var viewModel = new JournalEntryListViewModel
            {
                JournalEntries = journalEntries,
                SearchTerm = searchTerm,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                ClientId = clientId,
                VendorId = vendorId,
                IsRecurring = isRecurring,
                IsSystemGenerated = isSystemGenerated,
                AvailableClients = clients,
                AvailableVendors = vendors,
                AvailableStatuses = Enum.GetValues(typeof(JournalEntryStatus))
                    .Cast<JournalEntryStatus>()
                    .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
            };
            
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = await PrepareJournalEntryViewModel(new JournalEntryViewModel
            {
                EntryDate = DateTime.Today,
                PostingDate = DateOnly.FromDateTime(DateTime.Today),
                Status = JournalEntryStatus.Draft,
                Currency = "EGP",
                ExchangeRate = 1,
                Lines = new List<JournalEntryLineViewModel>
                {
                    new JournalEntryLineViewModel()
                }
            });
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalEntryViewModel viewModel, IFormFile? attachment)
        {
            if (ModelState.IsValid)
            {
                // Validate journal entry
                if (!viewModel.IsBalanced)
                {
                    ModelState.AddModelError("", _localizer["Journal entry must be balanced (total debits must equal total credits)."]);
                    viewModel = await PrepareJournalEntryViewModel(viewModel);
                    return View(viewModel);
                }
                
                // Check if posting date is in a closed period
                var postingDate = viewModel.PostingDate.ToDateTime(TimeOnly.MinValue);
                var isPeriodClosed = await _periodManagementService.IsPeriodClosedAsync(postingDate);
                if (isPeriodClosed)
                {
                    ModelState.AddModelError("PostingDate", _localizer["Cannot post to a closed period."]);
                    viewModel = await PrepareJournalEntryViewModel(viewModel);
                    return View(viewModel);
                }
                
                // Handle attachment upload
                string attachmentUrl = null;
                if (attachment != null && attachment.Length > 0)
                {
                    attachmentUrl = await SaveAttachmentAsync(attachment);
                }
                
                // Create journal entry
                var journalEntry = new JournalEntry
                {
                    Number = viewModel.EntryNumber,
                    EntryDate = viewModel.EntryDate,
                    PostingDate = viewModel.PostingDate.ToDateTime(TimeOnly.MinValue),
                    Description = viewModel.Description,
                    Status = viewModel.Status,
                    ClientId = viewModel.ClientId,
                    VendorId = viewModel.VendorId,
                    Currency = viewModel.Currency,
                    ExchangeRate = viewModel.ExchangeRate,
                    IsRecurring = viewModel.IsRecurring,
                    RecurrencePattern = viewModel.RecurrencePattern,
                    NextRecurrenceDate = viewModel.NextRecurrenceDate,
                    EndRecurrenceDate = viewModel.EndRecurrenceDate,
                    IsSystemGenerated = false,
                    SourceDocument = viewModel.SourceDocument,
                    AttachmentUrl = attachmentUrl,
                    Notes = viewModel.Notes,
                    FiscalPeriodId = viewModel.FiscalPeriodId,
                    Lines = new List<JournalEntryLine>()
                };
                
                // Add journal entry lines
                foreach (var lineViewModel in viewModel.Lines.Where(l => l.AccountId != Guid.Empty && (l.DebitAmount > 0 || l.CreditAmount > 0)))
                {
                    journalEntry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = lineViewModel.AccountId,
                        CostCenterId = lineViewModel.CostCenterId,
                        Description = lineViewModel.Description,
                        Debit = lineViewModel.DebitAmount,
                        Credit = lineViewModel.CreditAmount,
                        TaxRateId = lineViewModel.TaxRateId,
                        TaxAmount = lineViewModel.TaxAmount,
                        WithholdingTaxId = lineViewModel.WithholdingTaxId,
                        WithholdingTaxAmount = lineViewModel.WithholdingTaxAmount
                    });
                }
                
                // Save journal entry
                await _generalLedgerService.AddJournalEntryAsync(journalEntry);
                
                // If status is Posted, post the journal entry
                if (journalEntry.Status == JournalEntryStatus.Posted)
                {
                    await _generalLedgerService.PostJournalEntryAsync(journalEntry.Id);
                }
                
                TempData["SuccessMessage"] = _localizer["Journal entry created successfully."].ToString();
                return RedirectToAction(nameof(Index));
            }
            
            viewModel = await PrepareJournalEntryViewModel(viewModel);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be edited
            if (journalEntry.Status == JournalEntryStatus.Posted)
            {
                TempData["ErrorMessage"] = _localizer["Posted journal entries cannot be edited."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            var viewModel = await PrepareJournalEntryViewModel(new JournalEntryViewModel
            {
                Id = journalEntry.Id,
                EntryNumber = journalEntry.Number,
                EntryDate = journalEntry.EntryDate,
                PostingDate = DateOnly.FromDateTime(journalEntry.PostingDate),
                Reference = journalEntry.Reference,
                Description = journalEntry.Description,
                Status = journalEntry.Status,
                ClientId = journalEntry.ClientId,
                VendorId = journalEntry.VendorId,
                Currency = journalEntry.Currency,
                ExchangeRate = journalEntry.ExchangeRate,
                IsRecurring = journalEntry.IsRecurring,
                RecurrencePattern = journalEntry.RecurrencePattern,
                NextRecurrenceDate = journalEntry.NextRecurrenceDate,
                EndRecurrenceDate = journalEntry.EndRecurrenceDate,
                IsSystemGenerated = journalEntry.IsSystemGenerated,
                SourceDocument = journalEntry.SourceDocument,
                AttachmentUrl = journalEntry.AttachmentUrl,
                Notes = journalEntry.Notes,
                FiscalPeriodId = journalEntry.FiscalPeriodId,
                Lines = journalEntry.Lines.Select(l => new JournalEntryLineViewModel
                {
                    Id = l.Id,
                    AccountId = l.AccountId,
                    CostCenterId = l.CostCenterId,
                    Description = l.Description,
                    DebitAmount = l.Debit,
                    CreditAmount = l.Credit,
                    TaxRateId = l.TaxRateId,
                    TaxAmount = l.TaxAmount,
                    WithholdingTaxId = l.WithholdingTaxId,
                    WithholdingTaxAmount = l.WithholdingTaxAmount
                }).ToList()
            });
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, JournalEntryViewModel viewModel, IFormFile? attachment)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
                if (journalEntry == null)
                {
                    return NotFound();
                }
                
                // Check if journal entry can be edited
                if (journalEntry.Status == JournalEntryStatus.Posted)
                {
                    TempData["ErrorMessage"] = _localizer["Posted journal entries cannot be edited."].ToString();
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Validate journal entry
                if (!viewModel.IsBalanced)
                {
                    ModelState.AddModelError("", _localizer["Journal entry must be balanced (total debits must equal total credits)."]);
                    viewModel = await PrepareJournalEntryViewModel(viewModel);
                    return View(viewModel);
                }
                
                // Check if posting date is in a closed period
                var editPostingDate = viewModel.PostingDate.ToDateTime(TimeOnly.MinValue);
                var isPeriodClosed = await _periodManagementService.IsPeriodClosedAsync(editPostingDate);
                if (isPeriodClosed)
                {
                    ModelState.AddModelError("PostingDate", _localizer["Cannot post to a closed period."]);
                    viewModel = await PrepareJournalEntryViewModel(viewModel);
                    return View(viewModel);
                }
                
                // Handle attachment upload
                string attachmentUrl = journalEntry.AttachmentUrl;
                if (attachment != null && attachment.Length > 0)
                {
                    // Delete old attachment if exists
                    if (!string.IsNullOrEmpty(attachmentUrl))
                    {
                        DeleteAttachment(attachmentUrl);
                    }
                    
                    attachmentUrl = await SaveAttachmentAsync(attachment);
                }
                
                // Update journal entry
                journalEntry.EntryDate = viewModel.EntryDate;
                journalEntry.PostingDate = viewModel.PostingDate.ToDateTime(TimeOnly.MinValue);
                journalEntry.Description = viewModel.Description;
                journalEntry.Status = viewModel.Status;
                journalEntry.ClientId = viewModel.ClientId;
                journalEntry.VendorId = viewModel.VendorId;
                journalEntry.Currency = viewModel.Currency;
                journalEntry.ExchangeRate = viewModel.ExchangeRate;
                journalEntry.IsRecurring = viewModel.IsRecurring;
                journalEntry.RecurrencePattern = viewModel.RecurrencePattern;
                journalEntry.NextRecurrenceDate = viewModel.NextRecurrenceDate;
                journalEntry.EndRecurrenceDate = viewModel.EndRecurrenceDate;
                journalEntry.SourceDocument = viewModel.SourceDocument;
                journalEntry.AttachmentUrl = attachmentUrl;
                journalEntry.Notes = viewModel.Notes;
                journalEntry.FiscalPeriodId = viewModel.FiscalPeriodId;
                
                // Update journal entry lines
                journalEntry.Lines.Clear();
                foreach (var lineViewModel in viewModel.Lines.Where(l => l.AccountId != Guid.Empty && (l.DebitAmount > 0 || l.CreditAmount > 0)))
                {
                    journalEntry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = lineViewModel.AccountId,
                        CostCenterId = lineViewModel.CostCenterId,
                        Description = lineViewModel.Description,
                        Debit = lineViewModel.DebitAmount,
                        Credit = lineViewModel.CreditAmount,
                        TaxRateId = lineViewModel.TaxRateId,
                        TaxAmount = lineViewModel.TaxAmount,
                        WithholdingTaxId = lineViewModel.WithholdingTaxId,
                        WithholdingTaxAmount = lineViewModel.WithholdingTaxAmount
                    });
                }
                
                // Save journal entry
                await _generalLedgerService.UpdateJournalEntryAsync(journalEntry);
                
                // If status is Posted, post the journal entry
                if (journalEntry.Status == JournalEntryStatus.Posted)
                {
                    await _generalLedgerService.PostJournalEntryAsync(journalEntry.Id);
                }
                
                TempData["SuccessMessage"] = _localizer["Journal entry updated successfully."].ToString();
                return RedirectToAction(nameof(Index));
            }
            
            viewModel = await PrepareJournalEntryViewModel(viewModel);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            return View(new JournalEntryViewModel
            {
                Id = journalEntry.Id,
                EntryDate = journalEntry.EntryDate,
                PostingDate = DateOnly.FromDateTime(journalEntry.PostingDate),
                Reference = journalEntry.Reference,
                Description = journalEntry.Description,
                Status = journalEntry.Status,
                ClientId = journalEntry.ClientId,
                ClientName = journalEntry.Client?.NameEn,
                VendorId = journalEntry.VendorId,
                VendorName = journalEntry.Vendor?.NameEn,
                Currency = journalEntry.Currency,
                ExchangeRate = journalEntry.ExchangeRate,
                IsRecurring = journalEntry.IsRecurring,
                RecurrencePattern = journalEntry.RecurrencePattern,
                NextRecurrenceDate = journalEntry.NextRecurrenceDate,
                EndRecurrenceDate = journalEntry.EndRecurrenceDate,
                IsSystemGenerated = journalEntry.IsSystemGenerated,
                SourceDocument = journalEntry.SourceDocument,
                AttachmentUrl = journalEntry.AttachmentUrl,
                Notes = journalEntry.Notes,
                DebitTotal = journalEntry.DebitTotal,
                CreditTotal = journalEntry.CreditTotal,
                IsBalanced = Math.Abs(journalEntry.DebitTotal - journalEntry.CreditTotal) < 0.01m,
                Lines = journalEntry.Lines.Select(l => new JournalEntryLineViewModel
                {
                    Id = l.Id,
                    AccountId = l.AccountId,
                    AccountCode = l.Account?.Code,
                    AccountName = l.Account?.NameEn,
                    CostCenterId = l.CostCenterId,
                    CostCenterName = l.CostCenter?.NameEn,
                    Description = l.Description,
                    DebitAmount = l.Debit,
                    CreditAmount = l.Credit,
                    TaxRateId = l.TaxRateId,
                    TaxAmount = l.TaxAmount,
                    WithholdingTaxId = l.WithholdingTaxId,
                    WithholdingTaxAmount = l.WithholdingTaxAmount
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be deleted
            if (journalEntry.Status == JournalEntryStatus.Posted)
            {
                TempData["ErrorMessage"] = _localizer["Posted journal entries cannot be deleted."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            return View(journalEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be deleted
            if (journalEntry.Status == JournalEntryStatus.Posted)
            {
                TempData["ErrorMessage"] = _localizer["Posted journal entries cannot be deleted."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Delete attachment if exists
            if (!string.IsNullOrEmpty(journalEntry.AttachmentUrl))
            {
                DeleteAttachment(journalEntry.AttachmentUrl);
            }
            
            await _generalLedgerService.DeleteJournalEntryAsync(id);
            TempData["SuccessMessage"] = _localizer["Journal entry deleted successfully."].ToString();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> SubmitForApproval(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be submitted for approval
            if (journalEntry.Status != JournalEntryStatus.Draft)
            {
                TempData["ErrorMessage"] = _localizer["Only draft journal entries can be submitted for approval."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Submit for approval
            journalEntry.Status = JournalEntryStatus.Pending;
            await _generalLedgerService.UpdateJournalEntryAsync(journalEntry);
            
            TempData["SuccessMessage"] = _localizer["Journal entry submitted for approval."].ToString();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be approved
            if (journalEntry.Status != JournalEntryStatus.Pending)
            {
                TempData["ErrorMessage"] = _localizer["Only pending journal entries can be approved."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Approve journal entry
            await _generalLedgerService.ApproveJournalEntryAsync(id);
            
            TempData["SuccessMessage"] = _localizer["Journal entry approved."].ToString();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be rejected
            if (journalEntry.Status != JournalEntryStatus.Pending)
            {
                TempData["ErrorMessage"] = _localizer["Only pending journal entries can be rejected."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Validate rejection reason
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = _localizer["Rejection reason is required."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Reject journal entry
            await _generalLedgerService.RejectJournalEntryAsync(id, rejectionReason);
            
            TempData["SuccessMessage"] = _localizer["Journal entry rejected."].ToString();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> Post(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryByIdAsync(id);
            if (journalEntry == null)
            {
                return NotFound();
            }
            
            // Check if journal entry can be posted
            if (journalEntry.Status != JournalEntryStatus.Approved)
            {
                TempData["ErrorMessage"] = _localizer["Only approved journal entries can be posted."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Check if posting date is in a closed period
            var postDate = journalEntry.PostingDate;
            var isPeriodClosed = await _periodManagementService.IsPeriodClosedAsync(postDate);
            if (isPeriodClosed)
            {
                TempData["ErrorMessage"] = _localizer["Cannot post to a closed period."].ToString();
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Post journal entry
            await _generalLedgerService.PostJournalEntryAsync(id);
            
            TempData["SuccessMessage"] = _localizer["Journal entry posted."].ToString();
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<JournalEntryViewModel> PrepareJournalEntryViewModel(JournalEntryViewModel viewModel)
        {
            // Get accounts
            viewModel.AvailableAccounts = await _chartOfAccountsService.GetAccountSelectListAsync(isActive: true);
            
            // Get cost centers
            viewModel.AvailableCostCenters = await _costCenterService.GetCostCenterSelectListAsync(isActive: true);
            
            // Get clients
            viewModel.AvailableClients = await _clientVendorService.GetClientSelectListAsync(isActive: true);
            
            // Get vendors
            viewModel.AvailableVendors = await _clientVendorService.GetVendorSelectListAsync(isActive: true);
            
            // Get tax rates
            viewModel.AvailableTaxRates = await _taxService.GetTaxRateSelectListAsync(isActive: true);
            
            // Get withholding taxes
            viewModel.AvailableWithholdingTaxes = await _taxService.GetWithholdingTaxSelectListAsync(isActive: true);
            
            // Get fiscal periods
            viewModel.AvailableFiscalPeriods = await _periodManagementService.GetOpenFiscalPeriodSelectListAsync();
            
            return viewModel;
        }

        private async Task<string> SaveAttachmentAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "journal-entries");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            
            return $"/uploads/journal-entries/{uniqueFileName}";
        }

        private void DeleteAttachment(string attachmentUrl)
        {
            if (string.IsNullOrEmpty(attachmentUrl))
            {
                return;
            }
            
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, attachmentUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
