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
    public class FinancialStatementsController : Controller
    {
        private readonly IFinancialReportingService _reportingService;
        private readonly ICostCenterService _costCenterService;
        private readonly IPeriodManagementService _periodManagementService;
        private readonly IStringLocalizer<FinancialStatementsController> _localizer;

        public FinancialStatementsController(
            IFinancialReportingService reportingService,
            ICostCenterService costCenterService,
            IPeriodManagementService periodManagementService,
            IStringLocalizer<FinancialStatementsController> localizer)
        {
            _reportingService = reportingService;
            _costCenterService = costCenterService;
            _periodManagementService = periodManagementService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> BalanceSheet()
        {
            var viewModel = new BalanceSheetFilterViewModel
            {
                AsOfDate = DateTime.Today,
                CostCenters = await _costCenterService.GetAllCostCentersAsync()
            };
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BalanceSheet(BalanceSheetFilterViewModel filter)
        {
            if (ModelState.IsValid)
            {
                var report = await _reportingService.GenerateBalanceSheetAsync(filter.AsOfDate, filter.CostCenterId);
                return View("BalanceSheetReport", report);
            }
            
            filter.CostCenters = await _costCenterService.GetAllCostCentersAsync();
            
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> IncomeStatement()
        {
            var viewModel = new IncomeStatementFilterViewModel
            {
                FromDate = new DateTime(DateTime.Today.Year, 1, 1),
                ToDate = DateTime.Today,
                CostCenters = await _costCenterService.GetAllCostCentersAsync()
            };
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> IncomeStatement(IncomeStatementFilterViewModel filter)
        {
            if (ModelState.IsValid)
            {
                var report = await _reportingService.GenerateIncomeStatementAsync(filter.FromDate, filter.ToDate, filter.CostCenterId);
                return View("IncomeStatementReport", report);
            }
            
            filter.CostCenters = await _costCenterService.GetAllCostCentersAsync();
            
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> CashFlow()
        {
            var viewModel = new CashFlowFilterViewModel
            {
                FromDate = new DateTime(DateTime.Today.Year, 1, 1),
                ToDate = DateTime.Today,
                CostCenters = await _costCenterService.GetAllCostCentersAsync()
            };
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CashFlow(CashFlowFilterViewModel filter)
        {
            if (ModelState.IsValid)
            {
                var report = await _reportingService.GenerateCashFlowStatementAsync(filter.FromDate, filter.ToDate, filter.CostCenterId);
                return View("CashFlowReport", report);
            }
            
            filter.CostCenters = await _costCenterService.GetAllCostCentersAsync();
            
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string reportType, DateTime? asOfDate, DateTime? fromDate, DateTime? toDate, Guid? costCenterId)
        {
            byte[] pdfBytes;
            string fileName;
            
            switch (reportType.ToLower())
            {
                case "balancesheet":
                    var balanceSheet = await _reportingService.GenerateBalanceSheetAsync(asOfDate ?? DateTime.Today, costCenterId);
                    pdfBytes = await _reportingService.ExportFinancialStatementToPdfAsync(balanceSheet, "BalanceSheet");
                    fileName = "BalanceSheet.pdf";
                    break;
                    
                case "incomestatement":
                    var incomeStatement = await _reportingService.GenerateIncomeStatementAsync(
                        fromDate ?? new DateTime(DateTime.Today.Year, 1, 1),
                        toDate ?? DateTime.Today,
                        costCenterId);
                    pdfBytes = await _reportingService.ExportFinancialStatementToPdfAsync(incomeStatement, "IncomeStatement");
                    fileName = "IncomeStatement.pdf";
                    break;
                    
                case "cashflow":
                    var cashFlow = await _reportingService.GenerateCashFlowStatementAsync(
                        fromDate ?? new DateTime(DateTime.Today.Year, 1, 1),
                        toDate ?? DateTime.Today,
                        costCenterId);
                    pdfBytes = await _reportingService.ExportFinancialStatementToPdfAsync(cashFlow, "CashFlow");
                    fileName = "CashFlow.pdf";
                    break;
                    
                default:
                    return BadRequest("Invalid report type");
            }
            
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
