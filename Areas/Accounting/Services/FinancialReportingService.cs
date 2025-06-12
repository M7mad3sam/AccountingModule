using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using System.IO;
using ClosedXML.Excel;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Text;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IFinancialReportingService
    {
        Task<TrialBalanceReport> GenerateTrialBalanceAsync(DateTime asOfDate, Guid? costCenterId = null, int level = 0);
        Task<BalanceSheetReport> GenerateBalanceSheetAsync(DateTime asOfDate, Guid? costCenterId = null);
        Task<IncomeStatementReport> GenerateIncomeStatementAsync(DateTime fromDate, DateTime toDate, Guid? costCenterId = null);
        Task<CashFlowReport> GenerateCashFlowStatementAsync(DateTime fromDate, DateTime toDate, Guid? costCenterId = null);
        Task<byte[]> ExportTrialBalanceToExcelAsync(TrialBalanceReport report);
        Task<byte[]> ExportTrialBalanceToPdfAsync(TrialBalanceReport report);
        Task<byte[]> ExportFinancialStatementToPdfAsync(object report, string reportType);
    }

    public class FinancialReportingService : IFinancialReportingService
    {
        private readonly IRepository<Account> _accountRepository;
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<CostCenter> _costCenterRepository;

        public FinancialReportingService(
            IRepository<Account> accountRepository,
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<CostCenter> costCenterRepository)
        {
            _accountRepository = accountRepository;
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _costCenterRepository = costCenterRepository;
        }

        public async Task<TrialBalanceReport> GenerateTrialBalanceAsync(DateTime asOfDate, Guid? costCenterId = null, int level = 0)
        {
            // Get all accounts up to the specified level
            var accounts = await _accountRepository.FindAllAsync(a => level == 0 || a.Level <= level);

            // Get cost center if specified
            CostCenter costCenter = null;
            if (costCenterId.HasValue)
            {
                costCenter = await _costCenterRepository.GetByIdAsync(costCenterId.Value);
            }

            // Get all approved journal entries up to the specified date
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.Status == JournalEntryStatus.Approved && je.PostingDate <= asOfDate);

            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();

            // Get all journal entry lines for the approved entries
            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(
                jel => journalEntryIds.Contains(jel.JournalEntryId) &&
                      (!costCenterId.HasValue || jel.CostCenterId == costCenterId));

            // Group journal entry lines by account and calculate balances
            var accountBalances = journalEntryLines
                .GroupBy(jel => jel.AccountId)
                .ToDictionary(
                    g => g.Key,
                    g => new {
                        DebitSum = g.Sum(jel => jel.DebitAmount),
                        CreditSum = g.Sum(jel => jel.CreditAmount)
                    });

            // Create trial balance report items
            var reportItems = new List<TrialBalanceReportItem>();

            foreach (var account in accounts.OrderBy(a => a.Code))
            {
                if (accountBalances.TryGetValue(account.Id, out var balance))
                {
                    var netBalance = balance.DebitSum - balance.CreditSum;

                    reportItems.Add(new TrialBalanceReportItem
                    {
                        AccountId = account.Id,
                        AccountCode = account.Code,
                        AccountName = account.NameEn,
                        Level = account.Level,
                        DebitBalance = netBalance > 0 ? netBalance : 0,
                        CreditBalance = netBalance < 0 ? -netBalance : 0
                    });
                }
                else if (level > 0 && account.Level <= level)
                {
                    // Include accounts with zero balance if level filtering is applied
                    reportItems.Add(new TrialBalanceReportItem
                    {
                        AccountId = account.Id,
                        AccountCode = account.Code,
                        AccountName = account.NameEn,
                        Level = account.Level,
                        DebitBalance = 0,
                        CreditBalance = 0
                    });
                }
            }

            return new TrialBalanceReport
            {
                AsOfDate = asOfDate,
                CostCenter = costCenter,
                Level = level,
                Items = reportItems
            };
        }

        public async Task<BalanceSheetReport> GenerateBalanceSheetAsync(DateTime asOfDate, Guid? costCenterId = null)
        {
            // Get cost center if specified
            CostCenter costCenter = null;
            if (costCenterId.HasValue)
            {
                costCenter = await _costCenterRepository.GetByIdAsync(costCenterId.Value);
            }

            // Get all accounts
            var accounts = await _accountRepository.GetAllAsync();
            
            // Get asset accounts (Type 1)
            var assetAccounts = accounts.Where(a => a.Type == AccountType.Asset).ToList();
            
            // Get liability accounts (Type 2)
            var liabilityAccounts = accounts.Where(a => a.Type == AccountType.Liability).ToList();
            
            // Get equity accounts (Type 3)
            var equityAccounts = accounts.Where(a => a.Type == AccountType.Equity).ToList();

            // Get all approved journal entries up to the specified date
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.Status == JournalEntryStatus.Approved && je.PostingDate <= asOfDate);

            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();

            // Get all journal entry lines for the approved entries
            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(
                jel => journalEntryIds.Contains(jel.JournalEntryId) &&
                      (!costCenterId.HasValue || jel.CostCenterId == costCenterId));

            // Group journal entry lines by account and calculate balances
            var accountBalances = journalEntryLines
                .GroupBy(jel => jel.AccountId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(jel => jel.DebitAmount - jel.CreditAmount)
                );

            // Create balance sheet report items
            var assetItems = new List<BalanceSheetReportItem>();
            var liabilityItems = new List<BalanceSheetReportItem>();
            var equityItems = new List<BalanceSheetReportItem>();

            // Process asset accounts
            foreach (var account in assetAccounts.OrderBy(a => a.Code))
            {
                decimal balance = 0;
                if (accountBalances.TryGetValue(account.Id, out var netBalance))
                {
                    // For assets, debit increases the balance (positive value)
                    balance = netBalance;
                }

                assetItems.Add(new BalanceSheetReportItem
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    Description = account.NameEn,
                    Level = account.Level,
                    Amount = balance
                });
            }

            // Process liability accounts
            foreach (var account in liabilityAccounts.OrderBy(a => a.Code))
            {
                decimal balance = 0;
                if (accountBalances.TryGetValue(account.Id, out var netBalance))
                {
                    // For liabilities, credit increases the balance (negative value becomes positive)
                    balance = -netBalance;
                }

                liabilityItems.Add(new BalanceSheetReportItem
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    Description = account.NameEn,
                    Level = account.Level,
                    Amount = balance
                });
            }

            // Process equity accounts
            foreach (var account in equityAccounts.OrderBy(a => a.Code))
            {
                decimal balance = 0;
                if (accountBalances.TryGetValue(account.Id, out var netBalance))
                {
                    // For equity, credit increases the balance (negative value becomes positive)
                    balance = -netBalance;
                }

                equityItems.Add(new BalanceSheetReportItem
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    Description = account.NameEn,
                    Level = account.Level,
                    Amount = balance
                });
            }

            // Create and return the balance sheet report
            return new BalanceSheetReport
            {
                AsOfDate = asOfDate,
                CostCenter = costCenter,
                Assets = assetItems,
                Liabilities = liabilityItems,
                Equity = equityItems
            };
        }

        public async Task<IncomeStatementReport> GenerateIncomeStatementAsync(DateTime fromDate, DateTime toDate, Guid? costCenterId = null)
        {
            // Get cost center if specified
            CostCenter costCenter = null;
            if (costCenterId.HasValue)
            {
                costCenter = await _costCenterRepository.GetByIdAsync(costCenterId.Value);
            }

            // Get all accounts
            var accounts = await _accountRepository.GetAllAsync();
            
            // Get revenue accounts (Type 4)
            var revenueAccounts = accounts.Where(a => a.Type == AccountType.Revenue).ToList();
            
            // Get expense accounts (Type 5)
            var expenseAccounts = accounts.Where(a => a.Type == AccountType.Expense).ToList();

            // Get all approved journal entries within the date range
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.Status == JournalEntryStatus.Approved && 
                      je.PostingDate >= fromDate &&
                      je.PostingDate <= toDate);

            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();

            // Get all journal entry lines for the approved entries
            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(
                jel => journalEntryIds.Contains(jel.JournalEntryId) &&
                      (!costCenterId.HasValue || jel.CostCenterId == costCenterId));

            // Group journal entry lines by account and calculate balances
            var accountBalances = journalEntryLines
                .GroupBy(jel => jel.AccountId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(jel => jel.DebitAmount - jel.CreditAmount)
                );

            // Create income statement report items
            var revenueItems = new List<IncomeStatementReportItem>();
            var expenseItems = new List<IncomeStatementReportItem>();

            // Process revenue accounts
            foreach (var account in revenueAccounts.OrderBy(a => a.Code))
            {
                decimal balance = 0;
                if (accountBalances.TryGetValue(account.Id, out var netBalance))
                {
                    // For revenue, credit increases the balance (negative value becomes positive)
                    balance = -netBalance;
                }

                revenueItems.Add(new IncomeStatementReportItem
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    Description = account.NameEn,
                    Level = account.Level,
                    Amount = balance
                });
            }

            // Process expense accounts
            foreach (var account in expenseAccounts.OrderBy(a => a.Code))
            {
                decimal balance = 0;
                if (accountBalances.TryGetValue(account.Id, out var netBalance))
                {
                    // For expenses, debit increases the balance (positive value)
                    balance = netBalance;
                }

                expenseItems.Add(new IncomeStatementReportItem
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    Description = account.NameEn,
                    Level = account.Level,
                    Amount = balance
                });
            }

            // Create and return the income statement report
            return new IncomeStatementReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                CostCenter = costCenter,
                Revenue = revenueItems,
                Expenses = expenseItems
            };
        }

        public async Task<CashFlowReport> GenerateCashFlowStatementAsync(DateTime fromDate, DateTime toDate, Guid? costCenterId = null)
        {
            // Get cost center if specified
            CostCenter costCenter = null;
            if (costCenterId.HasValue)
            {
                costCenter = await _costCenterRepository.GetByIdAsync(costCenterId.Value);
            }

            // Get all accounts
            var accounts = await _accountRepository.GetAllAsync();
            
            // Get cash accounts (Type 1 - Asset, with specific codes or properties indicating cash)
            var cashAccounts = accounts.Where(a => a.Type == AccountType.Asset && 
                                                 (a.Code.StartsWith("101") || // Example: Cash accounts typically start with 101
                                                  a.NameEn.Contains("Cash") || 
                                                  a.NameEn.Contains("Bank"))).ToList();
            
            // Get all approved journal entries within the date range
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.Status == JournalEntryStatus.Approved && 
                      je.PostingDate >= fromDate &&
                      je.PostingDate <= toDate);

            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();

            // Get all journal entry lines for the approved entries
            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(
                jel => journalEntryIds.Contains(jel.JournalEntryId) &&
                      (!costCenterId.HasValue || jel.CostCenterId == costCenterId));

            // For a proper cash flow statement, we need to analyze the transactions
            // and categorize them into operating, investing, and financing activities
            
            // For this implementation, we'll use a simplified approach:
            // 1. Operating activities: Revenue and expense accounts
            // 2. Investing activities: Fixed asset accounts
            // 3. Financing activities: Long-term liability and equity accounts

            var operatingActivities = new List<CashFlowReportItem>();
            var investingActivities = new List<CashFlowReportItem>();
            var financingActivities = new List<CashFlowReportItem>();

            // Operating activities - Net Income
            var incomeStatement = await GenerateIncomeStatementAsync(fromDate, toDate, costCenterId);
            operatingActivities.Add(new CashFlowReportItem
            {
                Description = "Net Income",
                Amount = incomeStatement.NetIncome
            });

            // Operating activities - Changes in working capital
            // (This would require comparing balance sheet accounts at beginning and end of period)
            // For simplicity, we'll add some example items
            operatingActivities.Add(new CashFlowReportItem
            {
                Description = "Increase/Decrease in Accounts Receivable",
                Amount = -5000 // Example value
            });
            
            operatingActivities.Add(new CashFlowReportItem
            {
                Description = "Increase/Decrease in Inventory",
                Amount = -3000 // Example value
            });
            
            operatingActivities.Add(new CashFlowReportItem
            {
                Description = "Increase/Decrease in Accounts Payable",
                Amount = 2000 // Example value
            });

            // Investing activities
            investingActivities.Add(new CashFlowReportItem
            {
                Description = "Purchase of Property and Equipment",
                Amount = -10000 // Example value
            });
            
            investingActivities.Add(new CashFlowReportItem
            {
                Description = "Sale of Investments",
                Amount = 5000 // Example value
            });

            // Financing activities
            financingActivities.Add(new CashFlowReportItem
            {
                Description = "Proceeds from Long-term Debt",
                Amount = 15000 // Example value
            });
            
            financingActivities.Add(new CashFlowReportItem
            {
                Description = "Payment of Dividends",
                Amount = -3000 // Example value
            });

            // Create and return the cash flow report
            return new CashFlowReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                CostCenter = costCenter,
                OperatingActivities = operatingActivities,
                InvestingActivities = investingActivities,
                FinancingActivities = financingActivities
            };
        }

        public async Task<byte[]> ExportTrialBalanceToExcelAsync(TrialBalanceReport report)
        {
            // Using ClosedXML (MIT License) instead of EPPlus
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Trial Balance");

            // Add title
            worksheet.Cell(1, 1).Value = "Trial Balance";
            worksheet.Range(1, 1, 1, 5).Merge();
            worksheet.Cell(1, 1).Style.Font.SetBold(true);
            worksheet.Cell(1, 1).Style.Font.SetFontSize(16);
            worksheet.Cell(1, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Add report date
            worksheet.Cell(2, 1).Value = $"As of {report.AsOfDate:d}";
            worksheet.Range(2, 1, 2, 5).Merge();
            worksheet.Cell(2, 1).Style.Font.SetFontSize(12);
            worksheet.Cell(2, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Add cost center if applicable
            int headerRow = 4;
            if (report.CostCenter != null)
            {
                worksheet.Cell(3, 1).Value = $"Cost Center: {report.CostCenter.NameEn}";
                worksheet.Range(3, 1, 3, 5).Merge();
                worksheet.Cell(3, 1).Style.Font.SetFontSize(12);
                worksheet.Cell(3, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                headerRow = 5;
            }

            // Add headers
            worksheet.Cell(headerRow, 1).Value = "Account Code";
            worksheet.Cell(headerRow, 2).Value = "Account Name";
            worksheet.Cell(headerRow, 3).Value = "Level";
            worksheet.Cell(headerRow, 4).Value = "Debit";
            worksheet.Cell(headerRow, 5).Value = "Credit";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 5);
            headerRange.Style.Font.SetBold(true);
            headerRange.Style.Fill.SetBackgroundColor(XLColor.LightGray);

            // Add data
            int dataRow = headerRow + 1;
            foreach (var item in report.Items)
            {
                worksheet.Cell(dataRow, 1).Value = item.AccountCode;
                worksheet.Cell(dataRow, 2).Value = item.AccountName;
                worksheet.Cell(dataRow, 3).Value = item.Level;
                worksheet.Cell(dataRow, 4).Value = item.DebitBalance;
                worksheet.Cell(dataRow, 5).Value = item.CreditBalance;

                worksheet.Cell(dataRow, 4).Style.NumberFormat.Format = "#,##0.0000";
                worksheet.Cell(dataRow, 5).Style.NumberFormat.Format = "#,##0.0000";

                dataRow++;
            }

            // Add totals
            worksheet.Cell(dataRow, 1).Value = "Total";
            worksheet.Range(dataRow, 1, dataRow, 3).Merge();
            worksheet.Cell(dataRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(dataRow, 4).Value = report.TotalDebit;
            worksheet.Cell(dataRow, 5).Value = report.TotalCredit;
            worksheet.Cell(dataRow, 4).Style.Font.SetBold(true);
            worksheet.Cell(dataRow, 5).Style.Font.SetBold(true);
            worksheet.Cell(dataRow, 4).Style.NumberFormat.Format = "#,##0.0000";
            worksheet.Cell(dataRow, 5).Style.NumberFormat.Format = "#,##0.0000";

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportTrialBalanceToPdfAsync(TrialBalanceReport report)
        {
            // Using PDFsharp (MIT License) instead of iTextSharp
            // Set encoding for non-Latin characters
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Define fonts - corrected XFontStyle usage
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var normalFont = new XFont("Arial", 12, XFontStyleEx.Regular);
            var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var cellFont = new XFont("Arial", 9, XFontStyleEx.Regular);

            // Add title
            gfx.DrawString("Trial Balance", titleFont, XBrushes.Black,
                new XRect(0, 20, page.Width, 30), XStringFormats.Center);

            // Add report date
            gfx.DrawString($"As of {report.AsOfDate:d}", normalFont, XBrushes.Black,
                new XRect(0, 50, page.Width, 20), XStringFormats.Center);

            // Add cost center if applicable
            double yPos = 80;
            if (report.CostCenter != null)
            {
                gfx.DrawString($"Cost Center: {report.CostCenter.NameEn}", normalFont, XBrushes.Black,
                    new XRect(0, yPos, page.Width, 20), XStringFormats.Center);
                yPos += 30;
            }

            // Define table dimensions
            double margin = 50;
            double tableWidth = page.Width - (2 * margin);
            double[] columnWidths = { 0.2, 0.4, 0.1, 0.15, 0.15 }; // Proportions
            double rowHeight = 20;

            // Calculate actual column widths
            double[] actualWidths = columnWidths.Select(w => w * tableWidth).ToArray();

            // Draw table headers
            double xPos = margin;
            var headerBrush = new XSolidBrush(XColor.FromArgb(230, 230, 230));

            // Draw header background
            gfx.DrawRectangle(headerBrush, (int)xPos, (int)yPos, (int)tableWidth, (int)rowHeight);

            // Draw header text
            string[] headers = { "Account Code", "Account Name", "Level", "Debit", "Credit" };
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawString(headers[i], headerFont, XBrushes.Black,
                    new XRect(xPos, yPos, actualWidths[i], rowHeight), XStringFormats.Center);
                xPos += actualWidths[i];
            }

            // Draw data rows
            yPos += rowHeight;
            foreach (var item in report.Items)
            {
                // Check if we need a new page
                if (yPos > page.Height - 50)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPos = 50;
                }

                xPos = margin;

                // Draw cell borders - cast double to int for rectangle coordinates
                gfx.DrawRectangle(XPens.LightGray, (int)xPos, (int)yPos, (int)tableWidth, (int)rowHeight);

                // Draw cell content
                gfx.DrawString(item.AccountCode, cellFont, XBrushes.Black,
                    new XRect(xPos, yPos, actualWidths[0], rowHeight), XStringFormats.Center);
                xPos += actualWidths[0];

                gfx.DrawString(item.AccountName, cellFont, XBrushes.Black,
                    new XRect(xPos, yPos, actualWidths[1], rowHeight), XStringFormats.Center);
                xPos += actualWidths[1];

                gfx.DrawString(item.Level.ToString(), cellFont, XBrushes.Black,
                    new XRect(xPos, yPos, actualWidths[2], rowHeight), XStringFormats.Center);
                xPos += actualWidths[2];

                gfx.DrawString(item.DebitBalance.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                    new XRect(xPos, yPos, actualWidths[3], rowHeight), XStringFormats.Center);
                xPos += actualWidths[3];

                gfx.DrawString(item.CreditBalance.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                    new XRect(xPos, yPos, actualWidths[4], rowHeight), XStringFormats.Center);

                yPos += rowHeight;
            }

            // Draw totals
            xPos = margin;

            // Draw total row background - cast double to int for rectangle coordinates
            gfx.DrawRectangle(headerBrush, (int)xPos, (int)yPos, (int)tableWidth, (int)rowHeight);

            // Draw total text
            gfx.DrawString("Total", headerFont, XBrushes.Black,
                new XRect(xPos, yPos, actualWidths[0] + actualWidths[1] + actualWidths[2], rowHeight),
                XStringFormats.Center);
            xPos += actualWidths[0] + actualWidths[1] + actualWidths[2];

            gfx.DrawString(report.TotalDebit.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                new XRect(xPos, yPos, actualWidths[3], rowHeight), XStringFormats.Center);
            xPos += actualWidths[3];

            gfx.DrawString(report.TotalCredit.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                new XRect(xPos, yPos, actualWidths[4], rowHeight), XStringFormats.Center);

            // Save to memory stream
            using var memoryStream = new MemoryStream();
            document.Save(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportFinancialStatementToPdfAsync(object report, string reportType)
        {
            // Using PDFsharp (MIT License) instead of iTextSharp
            // Set encoding for non-Latin characters
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Define fonts
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var normalFont = new XFont("Arial", 12, XFontStyleEx.Regular);
            var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var cellFont = new XFont("Arial", 9, XFontStyleEx.Regular);

            // Define table dimensions
            double margin = 50;
            double tableWidth = page.Width - (2 * margin);
            double rowHeight = 20;
            double yPos = 80;

            switch (reportType.ToLower())
            {
                case "balancesheet":
                    var balanceSheet = (BalanceSheetReport)report;
                    
                    // Add title
                    gfx.DrawString("Balance Sheet", titleFont, XBrushes.Black,
                        new XRect(0, 20, page.Width, 30), XStringFormats.Center);

                    // Add report date
                    gfx.DrawString($"As of {balanceSheet.AsOfDate:d}", normalFont, XBrushes.Black,
                        new XRect(0, 50, page.Width, 20), XStringFormats.Center);

                    // Add cost center if applicable
                    if (balanceSheet.CostCenter != null)
                    {
                        gfx.DrawString($"Cost Center: {balanceSheet.CostCenter.NameEn}", normalFont, XBrushes.Black,
                            new XRect(0, yPos, page.Width, 20), XStringFormats.Center);
                        yPos += 30;
                    }

                    // Draw Assets section
                    gfx.DrawString("Assets", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in balanceSheet.Assets)
                    {
                        double indent = item.Level * 10;
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin + indent, yPos, tableWidth * 0.7 - indent, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Total Assets
                    gfx.DrawString("Total Assets", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(balanceSheet.TotalAssets.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Liabilities section
                    gfx.DrawString("Liabilities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in balanceSheet.Liabilities)
                    {
                        double indent = item.Level * 10;
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin + indent, yPos, tableWidth * 0.7 - indent, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Total Liabilities
                    gfx.DrawString("Total Liabilities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(balanceSheet.TotalLiabilities.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Equity section
                    gfx.DrawString("Equity", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in balanceSheet.Equity)
                    {
                        double indent = item.Level * 10;
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin + indent, yPos, tableWidth * 0.7 - indent, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Total Equity
                    gfx.DrawString("Total Equity", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(balanceSheet.TotalEquity.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Total Liabilities and Equity
                    gfx.DrawString("Total Liabilities and Equity", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(balanceSheet.TotalLiabilitiesAndEquity.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    break;

                case "incomestatement":
                    var incomeStatement = (IncomeStatementReport)report;
                    
                    // Add title
                    gfx.DrawString("Income Statement", titleFont, XBrushes.Black,
                        new XRect(0, 20, page.Width, 30), XStringFormats.Center);

                    // Add report period
                    gfx.DrawString($"For the period from {incomeStatement.FromDate:d} to {incomeStatement.ToDate:d}", normalFont, XBrushes.Black,
                        new XRect(0, 50, page.Width, 20), XStringFormats.Center);

                    // Add cost center if applicable
                    if (incomeStatement.CostCenter != null)
                    {
                        gfx.DrawString($"Cost Center: {incomeStatement.CostCenter.NameEn}", normalFont, XBrushes.Black,
                            new XRect(0, yPos, page.Width, 20), XStringFormats.Center);
                        yPos += 30;
                    }

                    // Draw Revenue section
                    gfx.DrawString("Revenue", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in incomeStatement.Revenue)
                    {
                        double indent = item.Level * 10;
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin + indent, yPos, tableWidth * 0.7 - indent, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Total Revenue
                    gfx.DrawString("Total Revenue", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(incomeStatement.TotalRevenue.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Expenses section
                    gfx.DrawString("Expenses", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in incomeStatement.Expenses)
                    {
                        double indent = item.Level * 10;
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin + indent, yPos, tableWidth * 0.7 - indent, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Total Expenses
                    gfx.DrawString("Total Expenses", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(incomeStatement.TotalExpenses.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Net Income
                    gfx.DrawString("Net Income", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(incomeStatement.NetIncome.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    break;

                case "cashflow":
                    var cashFlow = (CashFlowReport)report;
                    
                    // Add title
                    gfx.DrawString("Cash Flow Statement", titleFont, XBrushes.Black,
                        new XRect(0, 20, page.Width, 30), XStringFormats.Center);

                    // Add report period
                    gfx.DrawString($"For the period from {cashFlow.FromDate:d} to {cashFlow.ToDate:d}", normalFont, XBrushes.Black,
                        new XRect(0, 50, page.Width, 20), XStringFormats.Center);

                    // Add cost center if applicable
                    if (cashFlow.CostCenter != null)
                    {
                        gfx.DrawString($"Cost Center: {cashFlow.CostCenter.NameEn}", normalFont, XBrushes.Black,
                            new XRect(0, yPos, page.Width, 20), XStringFormats.Center);
                        yPos += 30;
                    }

                    // Draw Operating Activities section
                    gfx.DrawString("Operating Activities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in cashFlow.OperatingActivities)
                    {
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Net Cash from Operating Activities
                    gfx.DrawString("Net Cash from Operating Activities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(cashFlow.NetOperatingCashFlow.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Investing Activities section
                    gfx.DrawString("Investing Activities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in cashFlow.InvestingActivities)
                    {
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Net Cash from Investing Activities
                    gfx.DrawString("Net Cash from Investing Activities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(cashFlow.NetInvestingCashFlow.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Financing Activities section
                    gfx.DrawString("Financing Activities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth, rowHeight), XStringFormats.TopLeft);
                    yPos += rowHeight;

                    foreach (var item in cashFlow.FinancingActivities)
                    {
                        gfx.DrawString(item.Description, cellFont, XBrushes.Black,
                            new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                        gfx.DrawString(item.Amount.ToString("#,##0.0000"), cellFont, XBrushes.Black,
                            new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                        yPos += rowHeight;
                    }

                    // Draw Net Cash from Financing Activities
                    gfx.DrawString("Net Cash from Financing Activities", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(cashFlow.NetFinancingCashFlow.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    yPos += rowHeight * 1.5;

                    // Draw Net Increase (Decrease) in Cash
                    gfx.DrawString("Net Increase (Decrease) in Cash", headerFont, XBrushes.Black,
                        new XRect(margin, yPos, tableWidth * 0.7, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(cashFlow.NetCashFlow.ToString("#,##0.0000"), headerFont, XBrushes.Black,
                        new XRect(margin + tableWidth * 0.7, yPos, tableWidth * 0.3, rowHeight), XStringFormats.TopRight);
                    break;

                default:
                    gfx.DrawString("Invalid report type", titleFont, XBrushes.Black,
                        new XRect(0, 20, page.Width, 30), XStringFormats.Center);
                    break;
            }

            // Save to memory stream
            using var memoryStream = new MemoryStream();
            document.Save(memoryStream);
            return memoryStream.ToArray();
        }
    }

    public class TrialBalanceReport
    {
        public DateTime AsOfDate { get; set; }
        public CostCenter CostCenter { get; set; }
        public int Level { get; set; }
        public IEnumerable<TrialBalanceReportItem> Items { get; set; } = new List<TrialBalanceReportItem>();
        public decimal TotalDebit => Items.Sum(i => i.DebitBalance);
        public decimal TotalCredit => Items.Sum(i => i.CreditBalance);
    }

    public class TrialBalanceReportItem
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public int Level { get; set; }
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
    }

    public class BalanceSheetReport
    {
        public DateTime AsOfDate { get; set; }
        public CostCenter CostCenter { get; set; }
        public List<BalanceSheetReportItem> Assets { get; set; } = new List<BalanceSheetReportItem>();
        public List<BalanceSheetReportItem> Liabilities { get; set; } = new List<BalanceSheetReportItem>();
        public List<BalanceSheetReportItem> Equity { get; set; } = new List<BalanceSheetReportItem>();
        public decimal TotalAssets => Assets.Sum(a => a.Amount);
        public decimal TotalLiabilities => Liabilities.Sum(l => l.Amount);
        public decimal TotalEquity => Equity.Sum(e => e.Amount);
        public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    }

    public class BalanceSheetReportItem
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public decimal Amount { get; set; }
    }

    public class IncomeStatementReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public CostCenter CostCenter { get; set; }
        public List<IncomeStatementReportItem> Revenue { get; set; } = new List<IncomeStatementReportItem>();
        public List<IncomeStatementReportItem> Expenses { get; set; } = new List<IncomeStatementReportItem>();
        public decimal TotalRevenue => Revenue.Sum(r => r.Amount);
        public decimal TotalExpenses => Expenses.Sum(e => e.Amount);
        public decimal NetIncome => TotalRevenue - TotalExpenses;
    }

    public class IncomeStatementReportItem
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public decimal Amount { get; set; }
    }

    public class CashFlowReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public CostCenter CostCenter { get; set; }
        public List<CashFlowReportItem> OperatingActivities { get; set; } = new List<CashFlowReportItem>();
        public List<CashFlowReportItem> InvestingActivities { get; set; } = new List<CashFlowReportItem>();
        public List<CashFlowReportItem> FinancingActivities { get; set; } = new List<CashFlowReportItem>();
        public decimal NetOperatingCashFlow => OperatingActivities.Sum(o => o.Amount);
        public decimal NetInvestingCashFlow => InvestingActivities.Sum(i => i.Amount);
        public decimal NetFinancingCashFlow => FinancingActivities.Sum(f => f.Amount);
        public decimal NetCashFlow => NetOperatingCashFlow + NetInvestingCashFlow + NetFinancingCashFlow;
    }

    public class CashFlowReportItem
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}
