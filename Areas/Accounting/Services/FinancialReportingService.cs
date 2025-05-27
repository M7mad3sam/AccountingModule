using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models; // Add this for model references
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels; // Add this for view model references
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
                je => je.Status == JournalEntryStatus.Approved && je.Date <= asOfDate);

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
            // For minimal implementation, we'll return a placeholder
            return new BalanceSheetReport
            {
                AsOfDate = asOfDate,
                Assets = new List<BalanceSheetReportItem>(),
                Liabilities = new List<BalanceSheetReportItem>(),
                Equity = new List<BalanceSheetReportItem>()
            };
        }

        public async Task<IncomeStatementReport> GenerateIncomeStatementAsync(DateTime fromDate, DateTime toDate, Guid? costCenterId = null)
        {
            // For minimal implementation, we'll return a placeholder
            return new IncomeStatementReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                Revenue = new List<IncomeStatementReportItem>(),
                Expenses = new List<IncomeStatementReportItem>()
            };
        }

        public async Task<CashFlowReport> GenerateCashFlowStatementAsync(DateTime fromDate, DateTime toDate, Guid? costCenterId = null)
        {
            // For minimal implementation, we'll return a placeholder
            return new CashFlowReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                OperatingActivities = new List<CashFlowReportItem>(),
                InvestingActivities = new List<CashFlowReportItem>(),
                FinancingActivities = new List<CashFlowReportItem>()
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
            gfx.DrawRectangle(headerBrush, xPos, yPos, tableWidth, rowHeight);

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

            // Define fonts - corrected XFontStyle usage
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var normalFont = new XFont("Arial", 12, XFontStyleEx.Regular);

            // Add title
            gfx.DrawString(reportType, titleFont, XBrushes.Black,
                new XRect(0, 20, page.Width, 30), XStringFormats.Center);

            // Add placeholder text
            gfx.DrawString("This is a placeholder for the " + reportType + " report.", normalFont, XBrushes.Black,
                new XRect(0, 50, page.Width, 20), XStringFormats.Center);

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
        public List<TrialBalanceReportItem> Items { get; set; } = new List<TrialBalanceReportItem>();
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
        public List<BalanceSheetReportItem> Assets { get; set; } = new List<BalanceSheetReportItem>();
        public List<BalanceSheetReportItem> Liabilities { get; set; } = new List<BalanceSheetReportItem>();
        public List<BalanceSheetReportItem> Equity { get; set; } = new List<BalanceSheetReportItem>();
        public decimal TotalAssets => Assets.Sum(a => a.Amount);
        public decimal TotalLiabilities => Liabilities.Sum(l => l.Amount);
        public decimal TotalEquity => Equity.Sum(e => e.Amount);
    }

    public class BalanceSheetReportItem
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class IncomeStatementReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<IncomeStatementReportItem> Revenue { get; set; } = new List<IncomeStatementReportItem>();
        public List<IncomeStatementReportItem> Expenses { get; set; } = new List<IncomeStatementReportItem>();
        public decimal TotalRevenue => Revenue.Sum(r => r.Amount);
        public decimal TotalExpenses => Expenses.Sum(e => e.Amount);
        public decimal NetIncome => TotalRevenue - TotalExpenses;
    }

    public class IncomeStatementReportItem
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class CashFlowReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
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
