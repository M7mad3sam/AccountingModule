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
using AspNetCoreMvcTemplate.DTOs;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant,Manager,Auditor")]
    public class ReportsController : Controller
    {
        private readonly IFinancialReportingService _reportingService;
        private readonly ICostCenterService _costCenterService;
        private readonly IPeriodManagementService _periodManagementService;
        private readonly ITrialBalanceService _trialBalanceService;
        private readonly IFinancialStatementService _financialStatementService;
        private readonly IStringLocalizer<ReportsController> _localizer;

        public ReportsController(
            IFinancialReportingService reportingService,
            ICostCenterService costCenterService,
            IPeriodManagementService periodManagementService,
            ITrialBalanceService trialBalanceService,
            IFinancialStatementService financialStatementService,
            IStringLocalizer<ReportsController> localizer)
        {
            _reportingService = reportingService;
            _costCenterService = costCenterService;
            _periodManagementService = periodManagementService;
            _trialBalanceService = trialBalanceService;
            _financialStatementService = financialStatementService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> TrialBalance()
        {
            var viewModel = new TrialBalanceFilterViewModel
            {
                AsOfDate = DateTime.Today,
                CostCenters = await _costCenterService.GetAllCostCentersAsync(),
                Levels = Enumerable.Range(1, 5).Select(l => new SelectListItem { Value = l.ToString(), Text = l.ToString() }).ToList()
            };
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> TrialBalance(TrialBalanceFilterViewModel filter, DateTime? fromDate, DateTime? toDate)
        {
            if (ModelState.IsValid)
            {
                var from = fromDate.HasValue ? DateOnly.FromDateTime(fromDate.Value) : DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
                var to = toDate.HasValue ? DateOnly.FromDateTime(toDate.Value) : DateOnly.FromDateTime(DateTime.Today);
                var report = await _trialBalanceService.GetTrialBalanceAsync(from, to, filter.CostCenterId);
                ViewBag.FromDate = fromDate ?? DateTime.Today.AddYears(-1);
                ViewBag.ToDate = toDate ?? DateTime.Today;
                ViewBag.CostCenterId = filter.CostCenterId;
                return View("TrialBalanceReport", report);
            }
            
            filter.CostCenters = await _costCenterService.GetAllCostCentersAsync();
            filter.Levels = Enumerable.Range(1, 5).Select(l => new SelectListItem { Value = l.ToString(), Text = l.ToString() }).ToList();
            
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> ExportTrialBalanceToExcel(DateTime fromDate, DateTime toDate, Guid? costCenterId)
        {
            var from = DateOnly.FromDateTime(fromDate);
            var to = DateOnly.FromDateTime(toDate);
            var reportRows = await _trialBalanceService.GetTrialBalanceAsync(from, to, costCenterId);
            // TODO: Implement proper export functionality once FinancialReportingService is updated to accept TrialBalanceRow list
            // var excelBytes = await _reportingService.ExportTrialBalanceToExcelAsync(reportRows);
            // return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TrialBalance.xlsx");
            return Content("Export to Excel functionality is not yet implemented.");
        }

        [HttpGet]
        public async Task<IActionResult> ExportTrialBalanceToPdf(DateTime fromDate, DateTime toDate, Guid? costCenterId)
        {
            var from = DateOnly.FromDateTime(fromDate);
            var to = DateOnly.FromDateTime(toDate);
            var reportRows = await _trialBalanceService.GetTrialBalanceAsync(from, to, costCenterId);
            // TODO: Implement proper export functionality once FinancialReportingService is updated to accept TrialBalanceRow list
            // var pdfBytes = await _reportingService.ExportTrialBalanceToPdfAsync(reportRows);
            // return File(pdfBytes, "application/pdf", "TrialBalance.pdf");
            return Content("Export to PDF functionality is not yet implemented.");
        }

        [HttpGet]
        public async Task<IActionResult> BalanceSheet(Guid? periodId, Guid? costCenterId)
        {
            var periods = await _periodManagementService.GetAllFiscalPeriodsAsync();
            if (!periodId.HasValue && periods.Any())
                periodId = periods.OrderByDescending(p => p.EndDate).First().Id;

            var viewModel = new TrialBalanceFilterViewModel
            {
                PeriodId = periodId,
                CostCenterId = costCenterId,
                CostCenters = await _costCenterService.GetAllCostCentersAsync(),
                Periods = periods
            };

            if (periodId.HasValue)
            {
                var balanceSheet = await _financialStatementService.GetBalanceSheetAsync(periodId.Value, costCenterId);
                ViewBag.BalanceSheet = balanceSheet;
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> IncomeStatement(Guid? periodId, Guid? costCenterId)
        {
            var periods = await _periodManagementService.GetAllFiscalPeriodsAsync();
            if (!periodId.HasValue && periods.Any())
                periodId = periods.OrderByDescending(p => p.EndDate).First().Id;

            var viewModel = new TrialBalanceFilterViewModel
            {
                PeriodId = periodId,
                CostCenterId = costCenterId,
                CostCenters = await _costCenterService.GetAllCostCentersAsync(),
                Periods = periods
            };

            if (periodId.HasValue)
            {
                var incomeStatement = await _financialStatementService.GetIncomeStatementAsync(periodId.Value, costCenterId);
                ViewBag.IncomeStatement = incomeStatement;
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CashFlow(Guid? periodId, Guid? costCenterId)
        {
            var periods = await _periodManagementService.GetAllFiscalPeriodsAsync();
            if (!periodId.HasValue && periods.Any())
                periodId = periods.OrderByDescending(p => p.EndDate).First().Id;

            var viewModel = new TrialBalanceFilterViewModel
            {
                PeriodId = periodId,
                CostCenterId = costCenterId,
                CostCenters = await _costCenterService.GetAllCostCentersAsync(),
                Periods = periods
            };

            if (periodId.HasValue)
            {
                var cashFlow = await _financialStatementService.GetCashFlowAsync(periodId.Value, costCenterId);
                ViewBag.CashFlow = cashFlow;
            }

            return View(viewModel);
        }
    }
}
