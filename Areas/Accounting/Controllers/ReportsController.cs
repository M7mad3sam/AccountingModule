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

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant,Manager,Auditor")]
    public class ReportsController : Controller
    {
        private readonly IFinancialReportingService _reportingService;
        private readonly ICostCenterService _costCenterService;
        private readonly IPeriodManagementService _periodManagementService;
        private readonly IStringLocalizer<ReportsController> _localizer;

        public ReportsController(
            IFinancialReportingService reportingService,
            ICostCenterService costCenterService,
            IPeriodManagementService periodManagementService,
            IStringLocalizer<ReportsController> localizer)
        {
            _reportingService = reportingService;
            _costCenterService = costCenterService;
            _periodManagementService = periodManagementService;
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
        public async Task<IActionResult> TrialBalance(TrialBalanceFilterViewModel filter)
        {
            if (ModelState.IsValid)
            {
                var report = await _reportingService.GenerateTrialBalanceAsync(filter.AsOfDate, filter.CostCenterId, filter.Level);
                return View("TrialBalanceReport", report);
            }
            
            filter.CostCenters = await _costCenterService.GetAllCostCentersAsync();
            filter.Levels = Enumerable.Range(1, 5).Select(l => new SelectListItem { Value = l.ToString(), Text = l.ToString() }).ToList();
            
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> ExportTrialBalanceToExcel(DateTime asOfDate, Guid? costCenterId, int level)
        {
            var excelBytes = await _reportingService.ExportTrialBalanceToExcelAsync(
                await _reportingService.GenerateTrialBalanceAsync(asOfDate, costCenterId, level));
            
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TrialBalance.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportTrialBalanceToPdf(DateTime asOfDate, Guid? costCenterId, int level)
        {
            var pdfBytes = await _reportingService.ExportTrialBalanceToPdfAsync(
                await _reportingService.GenerateTrialBalanceAsync(asOfDate, costCenterId, level));
            
            return File(pdfBytes, "application/pdf", "TrialBalance.pdf");
        }
    }
}
