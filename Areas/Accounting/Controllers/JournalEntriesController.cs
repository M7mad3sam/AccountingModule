using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using AspNetCoreMvcTemplate.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant,Manager")]
    public class JournalEntriesController : Controller
    {
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IChartOfAccountsService _chartOfAccountsService;
        private readonly ICostCenterService _costCenterService;
        private readonly IPeriodManagementService _periodManagementService;
        private readonly ITaxService _taxService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<JournalEntriesController> _localizer;

        public JournalEntriesController(
            IGeneralLedgerService generalLedgerService,
            IChartOfAccountsService chartOfAccountsService,
            ICostCenterService costCenterService,
            IPeriodManagementService periodManagementService,
            ITaxService taxService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<JournalEntriesController> localizer)
        {
            _generalLedgerService = generalLedgerService;
            _chartOfAccountsService = chartOfAccountsService;
            _costCenterService = costCenterService;
            _periodManagementService = periodManagementService;
            _taxService = taxService;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(DateTime? fromDate = null, DateTime? toDate = null, JournalEntryStatus? status = null, int page = 1)
        {
            var pageSize = 50;
            var result = await _generalLedgerService.GetJournalEntriesAsync(fromDate, toDate, status, page, pageSize);
            
            var viewModel = new JournalEntryListViewModel
            {
                JournalEntries = result.Items,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status,
                CurrentPage = page,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                StatusList = GetJournalEntryStatusSelectList()
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var journalEntry = await _generalLedgerService.GetJournalEntryWithLinesAsync(id);
            if (journalEntry == null)
                return NotFound();

            return View(journalEntry);
        }

        public async Task<IActionResult> Create()
        {
            var currentPeriod = await _periodManagementService.GetCurrentFiscalPeriodAsync();
            if (currentPeriod == null)
            {
                TempData["Error"] = _localizer["No active fiscal period found"];
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new JournalEntryViewModel
            {
                Date = DateTime.Today,
                FiscalPeriodId = currentPeriod.Id,
                FiscalPeriods = await GetFiscalPeriodSelectListAsync(),
                Lines = new List<JournalEntryLineViewModel>
                {
                    new JournalEntryLineViewModel
                    {
                        Accounts = await GetAccountSelectListAsync(),
                        CostCenters = await GetCostCenterSelectListAsync(),
                        TaxRates = await GetTaxRateSelectListAsync()
                    }
                }
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalEntryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Validate that debit equals credit
                if (!viewModel.IsBalanced)
                {
                    ModelState.AddModelError("", _localizer["Total debit must equal total credit"]);
                    viewModel.FiscalPeriods = await GetFiscalPeriodSelectListAsync();
                    
                    foreach (var line in viewModel.Lines)
                    {
                        line.Accounts = await GetAccountSelectListAsync();
                        line.CostCenters = await GetCostCenterSelectListAsync();
                        line.TaxRates = await GetTaxRateSelectListAsync();
                    }
                    
                    return View(viewModel);
                }

                var journalEntry = new JournalEntry
                {
                    Date = viewModel.Date,
                    Description = viewModel.Description,
                    Reference = viewModel.Reference,
                    FiscalPeriodId = viewModel.FiscalPeriodId,
                    CreatedById = _userManager.GetUserId(User),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    Lines = viewModel.Lines.Select(l => new JournalEntryLine
                    {
                        AccountId = l.AccountId,
                        CostCenterId = l.CostCenterId,
                        Description = l.Description,
                        DebitAmount = l.DebitAmount,
                        CreditAmount = l.CreditAmount,
                        TaxRateId = l.TaxRateId,
                        TaxAmount = l.TaxAmount
                    }).ToList()
                };

                try
                {
                    await _generalLedgerService.CreateJournalEntryAsync(journalEntry);
                    return RedirectToAction(nameof(Details), new { id = journalEntry.Id });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            viewModel.FiscalPeriods = await GetFiscalPeriodSelectListAsync();
            
            foreach (var line in viewModel.Lines)
            {
                line.Accounts = await GetAccountSelectListAsync();
                line.CostCenters = await GetCostCenterSelectListAsync();
                line.TaxRates = await GetTaxRateSelectListAsync();
            }
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Approve(Guid id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _generalLedgerService.ApproveJournalEntryAsync(id, userId);
                TempData["Success"] = _localizer["Journal entry approved successfully"];
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Reject(Guid id, string reason)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _generalLedgerService.RejectJournalEntryAsync(id, userId, reason);
                TempData["Success"] = _localizer["Journal entry rejected successfully"];
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> AddLine(int index)
        {
            var viewModel = new JournalEntryLineViewModel
            {
                Accounts = await GetAccountSelectListAsync(),
                CostCenters = await GetCostCenterSelectListAsync(),
                TaxRates = await GetTaxRateSelectListAsync()
            };
            
            ViewBag.Index = index;
            return PartialView("_JournalEntryLinePartial", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CalculateTax(Guid taxRateId, decimal amount)
        {
            var taxRate = await _taxService.GetTaxRateByIdAsync(taxRateId);
            if (taxRate == null)
                return Json(0);
                
            if (taxRate.Type == TaxType.VAT)
            {
                var tax = await _taxService.CalculateVatAsync(amount, taxRateId);
                return Json(tax);
            }
            else
            {
                var tax = await _taxService.CalculateWithholdingTaxAsync(amount, taxRateId);
                return Json(tax);
            }
        }

        private IEnumerable<SelectListItem> GetJournalEntryStatusSelectList()
        {
            return Enum.GetValues(typeof(JournalEntryStatus))
                .Cast<JournalEntryStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToString()
                });
        }

        private async Task<IEnumerable<SelectListItem>> GetFiscalPeriodSelectListAsync()
        {
            var periods = await _periodManagementService.GetAllFiscalYearsAsync();
            var selectList = new List<SelectListItem>();
            
            foreach (var year in periods)
            {
                var yearPeriods = await _periodManagementService.GetFiscalPeriodsForYearAsync(year.Id);
                var yearGroup = new SelectListGroup { Name = year.Name };
                
                foreach (var period in yearPeriods.Where(p => !p.IsLocked && !p.IsClosed))
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = period.Id.ToString(),
                        Text = period.Name,
                        Group = yearGroup
                    });
                }
            }
            
            return selectList;
        }

        private async Task<IEnumerable<SelectListItem>> GetAccountSelectListAsync()
        {
            var accounts = await _chartOfAccountsService.GetAllAccountsAsync();
            return accounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.Code)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Code} - {a.NameEn}"
                });
        }

        private async Task<IEnumerable<SelectListItem>> GetCostCenterSelectListAsync()
        {
            var costCenters = await _costCenterService.GetAllCostCentersAsync();
            return costCenters
                .Where(cc => cc.IsActive)
                .OrderBy(cc => cc.Code)
                .Select(cc => new SelectListItem
                {
                    Value = cc.Id.ToString(),
                    Text = $"{cc.Code} - {cc.NameEn}"
                });
        }

        private async Task<IEnumerable<SelectListItem>> GetTaxRateSelectListAsync()
        {
            var taxRates = await _taxService.GetAllTaxRatesAsync();
            return taxRates
                .OrderBy(tr => tr.Code)
                .Select(tr => new SelectListItem
                {
                    Value = tr.Id.ToString(),
                    Text = $"{tr.Code} - {tr.NameEn} ({tr.Rate}%)"
                });
        }
    }
}
