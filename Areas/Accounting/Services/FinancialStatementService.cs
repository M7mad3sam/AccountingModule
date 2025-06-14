using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.DTOs;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IFinancialStatementService
    {
        Task<BalanceSheetDTO> GetBalanceSheetAsync(Guid periodId, Guid? costCenterId);
        Task<IncomeStatementDTO> GetIncomeStatementAsync(Guid periodId, Guid? costCenterId);
        Task<CashFlowDTO> GetCashFlowAsync(Guid periodId, Guid? costCenterId);
    }

    public class FinancialStatementService : IFinancialStatementService
    {
        private readonly ITrialBalanceService _trialBalanceService;
        private readonly IRepository<FiscalPeriod> _periodRepository;
        private readonly StatementMapping _mapping;

        public FinancialStatementService(
            ITrialBalanceService trialBalanceService,
            IRepository<FiscalPeriod> periodRepository,
            IOptions<StatementMapping> mappingOptions)
        {
            _trialBalanceService = trialBalanceService;
            _periodRepository = periodRepository;
            _mapping = mappingOptions.Value;
        }

        public async Task<BalanceSheetDTO> GetBalanceSheetAsync(Guid periodId, Guid? costCenterId)
        {
            var period = await _periodRepository.GetByIdAsync(periodId);
            if (period == null)
                throw new ArgumentException("Invalid period ID.", nameof(periodId));

            var trialBalance = await _trialBalanceService.GetTrialBalanceAsync(
                DateOnly.FromDateTime(period.StartDate), 
                DateOnly.FromDateTime(period.EndDate), 
                costCenterId);

            var balanceSheet = new BalanceSheetDTO
            {
                Assets = new AssetsSection(),
                Liabilities = new LiabilitiesSection(),
                Equity = new EquitySection()
            };

            foreach (var row in trialBalance)
            {
                var accountCode = row.AccountCode;
                if (IsInRange(accountCode, _mapping.BalanceSheet.Assets.FixedAssets))
                    balanceSheet.Assets.FixedAssets.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.BalanceSheet.Assets.CurrentAssets))
                    balanceSheet.Assets.CurrentAssets.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.BalanceSheet.Assets.OtherAssets))
                    balanceSheet.Assets.OtherAssets.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.BalanceSheet.Liabilities.LongTermLiabilities))
                    balanceSheet.Liabilities.LongTermLiabilities.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.BalanceSheet.Liabilities.CurrentLiabilities))
                    balanceSheet.Liabilities.CurrentLiabilities.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.BalanceSheet.Equity))
                    balanceSheet.Equity.EquityAccounts.Add(MapToAccountBalance(row));
            }

            // Calculate totals
            balanceSheet.Assets.TotalFixedAssets = balanceSheet.Assets.FixedAssets.Sum(a => a.Balance);
            balanceSheet.Assets.TotalCurrentAssets = balanceSheet.Assets.CurrentAssets.Sum(a => a.Balance);
            balanceSheet.Assets.TotalOtherAssets = balanceSheet.Assets.OtherAssets.Sum(a => a.Balance);
            balanceSheet.TotalAssets = balanceSheet.Assets.TotalFixedAssets + balanceSheet.Assets.TotalCurrentAssets + balanceSheet.Assets.TotalOtherAssets;

            balanceSheet.Liabilities.TotalLongTermLiabilities = balanceSheet.Liabilities.LongTermLiabilities.Sum(a => a.Balance);
            balanceSheet.Liabilities.TotalCurrentLiabilities = balanceSheet.Liabilities.CurrentLiabilities.Sum(a => a.Balance);
            balanceSheet.TotalLiabilities = balanceSheet.Liabilities.TotalLongTermLiabilities + balanceSheet.Liabilities.TotalCurrentLiabilities;

            balanceSheet.TotalEquity = balanceSheet.Equity.EquityAccounts.Sum(a => a.Balance);
            balanceSheet.TotalLiabilitiesAndEquity = balanceSheet.TotalLiabilities + balanceSheet.TotalEquity;

            return balanceSheet;
        }

        public async Task<IncomeStatementDTO> GetIncomeStatementAsync(Guid periodId, Guid? costCenterId)
        {
            var period = await _periodRepository.GetByIdAsync(periodId);
            if (period == null)
                throw new ArgumentException("Invalid period ID.", nameof(periodId));

            var trialBalance = await _trialBalanceService.GetTrialBalanceAsync(
                DateOnly.FromDateTime(period.StartDate), 
                DateOnly.FromDateTime(period.EndDate), 
                costCenterId);

            var incomeStatement = new IncomeStatementDTO
            {
                Revenue = new RevenueSection(),
                Expenses = new ExpensesSection()
            };

            foreach (var row in trialBalance)
            {
                var accountCode = row.AccountCode;
                if (IsInRange(accountCode, _mapping.IncomeStatement.Revenue))
                    incomeStatement.Revenue.RevenueAccounts.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.IncomeStatement.CostOfSales))
                    incomeStatement.Expenses.CostOfSales.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.IncomeStatement.OperatingExpenses))
                    incomeStatement.Expenses.OperatingExpenses.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.IncomeStatement.OtherIncomeExpense))
                    incomeStatement.Expenses.OtherIncomeExpense.Add(MapToAccountBalance(row));
                else if (IsInRange(accountCode, _mapping.IncomeStatement.TaxExpense))
                    incomeStatement.Expenses.TaxExpense.Add(MapToAccountBalance(row));
            }

            // Calculate totals
            incomeStatement.TotalRevenue = incomeStatement.Revenue.RevenueAccounts.Sum(a => a.Balance);
            incomeStatement.Expenses.TotalCostOfSales = incomeStatement.Expenses.CostOfSales.Sum(a => a.Balance);
            incomeStatement.Expenses.TotalOperatingExpenses = incomeStatement.Expenses.OperatingExpenses.Sum(a => a.Balance);
            incomeStatement.Expenses.TotalOtherIncomeExpense = incomeStatement.Expenses.OtherIncomeExpense.Sum(a => a.Balance);
            incomeStatement.Expenses.TotalTaxExpense = incomeStatement.Expenses.TaxExpense.Sum(a => a.Balance);
            incomeStatement.TotalExpenses = incomeStatement.Expenses.TotalCostOfSales + incomeStatement.Expenses.TotalOperatingExpenses + incomeStatement.Expenses.TotalOtherIncomeExpense + incomeStatement.Expenses.TotalTaxExpense;
            incomeStatement.NetIncome = incomeStatement.TotalRevenue - incomeStatement.TotalExpenses;

            return incomeStatement;
        }

        public async Task<CashFlowDTO> GetCashFlowAsync(Guid periodId, Guid? costCenterId)
        {
            var period = await _periodRepository.GetByIdAsync(periodId);
            if (period == null)
                throw new ArgumentException("Invalid period ID.", nameof(periodId));

            // Get trial balance for the current period
            var currentTrialBalance = await _trialBalanceService.GetTrialBalanceAsync(
                DateOnly.FromDateTime(period.StartDate), 
                DateOnly.FromDateTime(period.EndDate), 
                costCenterId);

            // Assume previous period for working capital changes (simplified logic for demo)
            var previousPeriodEndDate = period.StartDate.AddDays(-1);
            var previousPeriodStartDate = previousPeriodEndDate.AddYears(-1);
            var previousTrialBalance = await _trialBalanceService.GetTrialBalanceAsync(
                DateOnly.FromDateTime(previousPeriodStartDate), 
                DateOnly.FromDateTime(previousPeriodEndDate), 
                costCenterId);

            // Get Income Statement for Net Income
            var incomeStatement = await GetIncomeStatementAsync(periodId, costCenterId);

            var cashFlow = new CashFlowDTO
            {
                OperatingActivities = new OperatingActivitiesSection(),
                InvestingActivities = new InvestingActivitiesSection(),
                FinancingActivities = new FinancingActivitiesSection()
            };

            // Operating Activities
            cashFlow.OperatingActivities.NetIncome = incomeStatement?.NetIncome ?? 0;

            // Adjustments (e.g., Depreciation)
            if (currentTrialBalance != null)
            {
                foreach (var row in currentTrialBalance)
                {
                    var accountCode = row?.AccountCode;
                    if (accountCode != null && IsInRange(accountCode, _mapping?.CashFlow?.OperatingActivities?.Adjustments ?? new string[0]))
                        cashFlow.OperatingActivities.Adjustments.Add(new CashFlowItem { Description = row.AccountName ?? "Unknown Account", Amount = row.Net });
                }
            }
            cashFlow.OperatingActivities.TotalAdjustments = cashFlow.OperatingActivities.Adjustments.Sum(a => a.Amount);

            // Changes in Working Capital
            var changesInWorkingCapitalRanges = _mapping?.CashFlow?.OperatingActivities?.ChangesInWorkingCapital ?? new string[0];
            foreach (var range in changesInWorkingCapitalRanges)
            {
                var currentBalance = currentTrialBalance?.Where(r => r != null && IsInRange(r.AccountCode, new[] { range })).Sum(r => r.Net) ?? 0;
                var previousBalance = previousTrialBalance?.Where(r => r != null && IsInRange(r.AccountCode, new[] { range })).Sum(r => r.Net) ?? 0;
                var change = currentBalance - previousBalance;
                cashFlow.OperatingActivities.ChangesInWorkingCapital.Add(new CashFlowItem { Description = $"Change in {range}", Amount = -change }); // Negative because increase in asset is outflow
            }
            cashFlow.OperatingActivities.TotalChangesInWorkingCapital = cashFlow.OperatingActivities.ChangesInWorkingCapital.Sum(a => a.Amount);

            cashFlow.NetCashFromOperating = cashFlow.OperatingActivities.NetIncome + cashFlow.OperatingActivities.TotalAdjustments + cashFlow.OperatingActivities.TotalChangesInWorkingCapital;

            // Investing Activities
            if (currentTrialBalance != null)
            {
                foreach (var row in currentTrialBalance)
                {
                    if (row != null && IsInRange(row.AccountCode, _mapping?.CashFlow?.InvestingActivities ?? new string[0]))
                        cashFlow.InvestingActivities.Items.Add(new CashFlowItem { Description = row.AccountName ?? "Unknown Account", Amount = -row.Net }); // Negative for cash outflow on asset purchase
                }
            }
            cashFlow.NetCashFromInvesting = cashFlow.InvestingActivities.Items.Sum(a => a.Amount);

            // Financing Activities
            if (currentTrialBalance != null)
            {
                foreach (var row in currentTrialBalance)
                {
                    if (row != null && IsInRange(row.AccountCode, _mapping?.CashFlow?.FinancingActivities ?? new string[0]))
                        cashFlow.FinancingActivities.Items.Add(new CashFlowItem { Description = row.AccountName ?? "Unknown Account", Amount = row.Net });
                }
            }
            cashFlow.NetCashFromFinancing = cashFlow.FinancingActivities.Items.Sum(a => a.Amount);

            // Net Increase in Cash
            cashFlow.NetIncreaseInCash = cashFlow.NetCashFromOperating + cashFlow.NetCashFromInvesting + cashFlow.NetCashFromFinancing;

            // Cash at Beginning and End (simplified, assuming cash accounts are in Current Assets)
            var currentAssetsRanges = _mapping?.BalanceSheet?.Assets?.CurrentAssets ?? new string[0];
            cashFlow.CashAtBeginning = previousTrialBalance?.Where(r => r != null && IsInRange(r.AccountCode, currentAssetsRanges)).Sum(r => r.Net) ?? 0;
            cashFlow.CashAtEnd = cashFlow.CashAtBeginning + cashFlow.NetIncreaseInCash;

            return cashFlow;
        }

        private bool IsInRange(string accountCode, string[] ranges)
        {
            foreach (var range in ranges)
            {
                var parts = range.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end))
                {
                    if (int.TryParse(accountCode, out var code) && code >= start && code <= end)
                        return true;
                }
            }
            return false;
        }

        private AccountBalance MapToAccountBalance(TrialBalanceRow row)
        {
            return new AccountBalance
            {
                Code = row.AccountCode,
                NameEn = row.AccountName,
                Balance = row.Net
            };
        }
    }

    public class StatementMapping
    {
        public BalanceSheetMapping BalanceSheet { get; set; }
        public IncomeStatementMapping IncomeStatement { get; set; }
        public CashFlowMapping CashFlow { get; set; }
    }

    public class BalanceSheetMapping
    {
        public AssetsMapping Assets { get; set; }
        public LiabilitiesMapping Liabilities { get; set; }
        public string[] Equity { get; set; }
    }

    public class AssetsMapping
    {
        public string[] FixedAssets { get; set; }
        public string[] CurrentAssets { get; set; }
        public string[] OtherAssets { get; set; }
    }

    public class LiabilitiesMapping
    {
        public string[] LongTermLiabilities { get; set; }
        public string[] CurrentLiabilities { get; set; }
    }

    public class IncomeStatementMapping
    {
        public string[] Revenue { get; set; }
        public string[] CostOfSales { get; set; }
        public string[] OperatingExpenses { get; set; }
        public string[] OtherIncomeExpense { get; set; }
        public string[] TaxExpense { get; set; }
    }

    public class CashFlowMapping
    {
        public OperatingActivitiesMapping OperatingActivities { get; set; }
        public string[] InvestingActivities { get; set; }
        public string[] FinancingActivities { get; set; }
    }

    public class OperatingActivitiesMapping
    {
        public string[] Adjustments { get; set; }
        public string[] ChangesInWorkingCapital { get; set; }
    }
}
