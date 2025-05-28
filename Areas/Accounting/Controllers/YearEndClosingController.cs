using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant")]
    public class YearEndClosingController : Controller
    {
        private readonly IPeriodManagementService _periodManagementService;
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IAuditService _auditService;

        public YearEndClosingController(
            IPeriodManagementService periodManagementService,
            IGeneralLedgerService generalLedgerService,
            IAuditService auditService)
        {
            _periodManagementService = periodManagementService;
            _generalLedgerService = generalLedgerService;
            _auditService = auditService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> Validate(Guid fiscalYearId)
        {
            var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(fiscalYearId);
            if (fiscalYear == null)
            {
                return NotFound();
            }

            var validationResult = await _periodManagementService.ValidateYearEndClosingAsync(fiscalYearId);
            
            var model = new YearEndClosingViewModel
            {
                FiscalYearId = fiscalYear.Id,
                FiscalYearName = fiscalYear.Name,
                FiscalYearCode = fiscalYear.Code,
                ValidationResult = validationResult
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Execute(YearEndClosingViewModel model)
        {
            if (!model.ConfirmClosing)
            {
                ModelState.AddModelError("ConfirmClosing", "You must confirm the year-end closing process");
                
                var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(model.FiscalYearId);
                if (fiscalYear == null)
                {
                    return NotFound();
                }
                
                model.FiscalYearName = fiscalYear.Name;
                model.FiscalYearCode = fiscalYear.Code;
                
                return View("Validate", model);
            }

            try
            {
                await _periodManagementService.PerformYearEndClosingAsync(model.FiscalYearId);
                await _auditService.LogActivityAsync("YearEndClosing", "Execute", $"Executed year-end closing for fiscal year: {model.FiscalYearName}");
                
                TempData["SuccessMessage"] = "Year-end closing process completed successfully.";
                return RedirectToAction("FiscalYears", "PeriodManagement");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                
                var fiscalYear = await _periodManagementService.GetFiscalYearByIdAsync(model.FiscalYearId);
                if (fiscalYear == null)
                {
                    return NotFound();
                }
                
                model.FiscalYearName = fiscalYear.Name;
                model.FiscalYearCode = fiscalYear.Code;
                
                return View("Validate", model);
            }
        }
    }
}
