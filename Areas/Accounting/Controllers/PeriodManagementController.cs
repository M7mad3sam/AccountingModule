using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using System.Linq;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant,Manager")]
    public class PeriodManagementController : Controller
    {
        private readonly IPeriodManagementService _periodManagementService;
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IAuditService _auditService;

        public PeriodManagementController(
            IPeriodManagementService periodManagementService,
            IGeneralLedgerService generalLedgerService,
            IAuditService auditService)
        {
            _periodManagementService = periodManagementService;
            _generalLedgerService = generalLedgerService;
            _auditService = auditService;
        }

        #region Fiscal Year Actions

        [HttpGet]
        public async Task<IActionResult> FiscalYears()
        {
            var fiscalYears = await _periodManagementService.GetFiscalYearsAsync();
            return View(fiscalYears);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Accountant")]
        public IActionResult CreateFiscalYear()
        {
            return View(new FiscalYearViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> CreateFiscalYear(FiscalYearViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var fiscalYear = new FiscalYear
                    {
                        Name = model.Name,
                        Code = model.Code,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        IsActive = model.IsActive,
                        IsClosed = false
                    };

                    await _periodManagementService.AddFiscalYearAsync(fiscalYear);
                    await _auditService.LogActivityAsync("FiscalYear", "Create", $"Created fiscal year: {fiscalYear.Name}");
                    
                    TempData["SuccessMessage"] = "Fiscal year created successfully.";
                    return RedirectToAction(nameof(FiscalYears));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> EditFiscalYear(Guid id)
        {
            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(id);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            var model = new FiscalYearViewModel
            {
                Id = fiscalYear.Id,
                Name = fiscalYear.Name,
                Code = fiscalYear.Code,
                StartDate = fiscalYear.StartDate,
                EndDate = fiscalYear.EndDate,
                IsActive = fiscalYear.IsActive,
                IsClosed = fiscalYear.IsClosed
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> EditFiscalYear(FiscalYearViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(model.Id);
                    if (fiscalYear == null)
                    {
                        return NotFound();
                    }

                    fiscalYear.Name = model.Name;
                    fiscalYear.Code = model.Code;
                    fiscalYear.StartDate = model.StartDate;
                    fiscalYear.EndDate = model.EndDate;
                    fiscalYear.IsActive = model.IsActive;

                    await _periodManagementService.UpdateFiscalYearAsync(fiscalYear);
                    await _auditService.LogActivityAsync("FiscalYear", "Update", $"Updated fiscal year: {fiscalYear.Name}");
                    
                    TempData["SuccessMessage"] = "Fiscal year updated successfully.";
                    return RedirectToAction(nameof(FiscalYears));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFiscalYear(Guid id)
        {
            try
            {
                var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(id);
                if (fiscalYear == null)
                {
                    return NotFound();
                }
                
                await _periodManagementService.DeleteFiscalYearAsync(id);
                await _auditService.LogActivityAsync("FiscalYear", "Delete", $"Deleted fiscal year: {fiscalYear.Name}");
                
                TempData["SuccessMessage"] = "Fiscal year deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message.ToString();
            }

            return RedirectToAction(nameof(FiscalYears));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> YearEndClosing(Guid id)
        {
            // Redirect to the new YearEndClosingController
            return RedirectToAction("Validate", "YearEndClosing", new { fiscalYearId = id });
        }

        #endregion

        #region Fiscal Period Actions

        [HttpGet]
        public async Task<IActionResult> FiscalPeriods(Guid fiscalYearId)
        {
            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(fiscalYearId);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            var fiscalPeriods = await _periodManagementService.GetFiscalPeriodsAsync(fiscalYearId);
            
            var model = new FiscalPeriodsViewModel
            {
                FiscalYearId = fiscalYearId,
                FiscalYearName = fiscalYear.Name,
                FiscalYearCode = fiscalYear.Code,
                FiscalPeriods = fiscalPeriods.ToList()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> CreateFiscalPeriod(Guid fiscalYearId)
        {
            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(fiscalYearId);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            var model = new FiscalPeriodViewModel
            {
                FiscalYearId = fiscalYearId,
                FiscalYearName = fiscalYear.Name,
                StartDate = fiscalYear.StartDate,
                EndDate = fiscalYear.StartDate.AddMonths(1).AddDays(-1),
                IsActive = true
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> CreateFiscalPeriod(FiscalPeriodViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var fiscalPeriod = new FiscalPeriod
                    {
                        FiscalYearId = model.FiscalYearId,
                        Name = model.Name,
                        Code = model.Code,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        IsActive = model.IsActive,
                        IsClosed = false
                    };

                    await _periodManagementService.AddFiscalPeriodAsync(fiscalPeriod);
                    await _auditService.LogActivityAsync("FiscalPeriod", "Create", $"Created fiscal period: {fiscalPeriod.Name}");
                    
                    TempData["SuccessMessage"] = "Fiscal period created successfully.";
                    return RedirectToAction(nameof(FiscalPeriods), new { fiscalYearId = model.FiscalYearId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // Reload fiscal year data
            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(model.FiscalYearId);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            model.FiscalYearName = fiscalYear.Name;

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> EditFiscalPeriod(Guid id)
        {
            var fiscalPeriod = await _periodManagementService.GetFiscalPeriodByIdAsync(id);
            if (fiscalPeriod == null)
            {
                return NotFound();
            }

            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(fiscalPeriod.FiscalYearId);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            var model = new FiscalPeriodViewModel
            {
                Id = fiscalPeriod.Id,
                FiscalYearId = fiscalPeriod.FiscalYearId,
                FiscalYearName = fiscalYear.Name,
                Name = fiscalPeriod.Name,
                Code = fiscalPeriod.Code,
                StartDate = fiscalPeriod.StartDate,
                EndDate = fiscalPeriod.EndDate,
                IsActive = fiscalPeriod.IsActive,
                IsClosed = fiscalPeriod.IsClosed
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> EditFiscalPeriod(FiscalPeriodViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var fiscalPeriod = await _periodManagementService.GetFiscalPeriodByIdAsync(model.Id);
                    if (fiscalPeriod == null)
                    {
                        return NotFound();
                    }

                    fiscalPeriod.Name = model.Name;
                    fiscalPeriod.Code = model.Code;
                    fiscalPeriod.StartDate = model.StartDate;
                    fiscalPeriod.EndDate = model.EndDate;
                    fiscalPeriod.IsActive = model.IsActive;

                    await _periodManagementService.UpdateFiscalPeriodAsync(fiscalPeriod);
                    await _auditService.LogActivityAsync("FiscalPeriod", "Update", $"Updated fiscal period: {fiscalPeriod.Name}");
                    
                    TempData["SuccessMessage"] = "Fiscal period updated successfully.";
                    return RedirectToAction(nameof(FiscalPeriods), new { fiscalYearId = model.FiscalYearId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // Reload fiscal year data
            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(model.FiscalYearId);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            model.FiscalYearName = fiscalYear.Name;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFiscalPeriod(Guid id, Guid fiscalYearId)
        {
            try
            {
                var fiscalPeriod = await _periodManagementService.GetFiscalPeriodByIdAsync(id);
                if (fiscalPeriod == null)
                {
                    return NotFound();
                }
                
                await _periodManagementService.DeleteFiscalPeriodAsync(id);
                await _auditService.LogActivityAsync("FiscalPeriod", "Delete", $"Deleted fiscal period: {fiscalPeriod.Name}");
                
                TempData["SuccessMessage"] = "Fiscal period deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message.ToString();
            }

            return RedirectToAction(nameof(FiscalPeriods), new { fiscalYearId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> OpenPeriod(Guid id, Guid fiscalYearId)
        {
            try
            {
                var fiscalPeriod = await _periodManagementService.GetFiscalPeriodByIdAsync(id);
                if (fiscalPeriod == null)
                {
                    return NotFound();
                }
                
                await _periodManagementService.OpenPeriodAsync(id);
                await _auditService.LogActivityAsync("FiscalPeriod", "Open", $"Opened fiscal period: {fiscalPeriod.Name}");
                
                TempData["SuccessMessage"] = "Fiscal period opened successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message.ToString();
            }

            return RedirectToAction(nameof(FiscalPeriods), new { fiscalYearId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> ClosePeriod(Guid id, Guid fiscalYearId)
        {
            try
            {
                var fiscalPeriod = await _periodManagementService.GetFiscalPeriodByIdAsync(id);
                if (fiscalPeriod == null)
                {
                    return NotFound();
                }
                
                await _periodManagementService.ClosePeriodAsync(id);
                await _auditService.LogActivityAsync("FiscalPeriod", "Close", $"Closed fiscal period: {fiscalPeriod.Name}");
                
                TempData["SuccessMessage"] = "Fiscal period closed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message.ToString();
            }

            return RedirectToAction(nameof(FiscalPeriods), new { fiscalYearId });
        }

        #endregion
    }
}
