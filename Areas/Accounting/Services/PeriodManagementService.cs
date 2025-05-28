using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IPeriodManagementService
    {
        // Fiscal Year methods
        Task<IEnumerable<FiscalYear>> GetFiscalYearsAsync(bool? isActive = null);
        Task<FiscalYear> GetFiscalYearByIdAsync(Guid id);
        Task<FiscalYear> GetFiscalYearByCodeAsync(string code);
        Task<FiscalYear> GetFiscalYearByDateAsync(DateTime date);
        Task AddFiscalYearAsync(FiscalYear fiscalYear);
        Task UpdateFiscalYearAsync(FiscalYear fiscalYear);
        Task DeleteFiscalYearAsync(Guid id);
        
        // Fiscal Period methods
        Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsAsync(Guid? fiscalYearId = null, bool? isActive = null, bool? isClosed = null);
        Task<IEnumerable<FiscalPeriod>> GetOpenFiscalPeriodsAsync(Guid? fiscalYearId = null);
        Task<FiscalPeriod> GetFiscalPeriodByIdAsync(Guid id);
        Task<FiscalPeriod> GetFiscalPeriodByDateAsync(DateTime date);
        Task AddFiscalPeriodAsync(FiscalPeriod fiscalPeriod);
        Task UpdateFiscalPeriodAsync(FiscalPeriod fiscalPeriod);
        Task DeleteFiscalPeriodAsync(Guid id);
        
        // Period management methods
        Task OpenPeriodAsync(Guid periodId);
        Task ClosePeriodAsync(Guid periodId);
        Task<bool> IsPeriodClosedAsync(DateTime date);
        Task<bool> CanClosePeriodAsync(Guid periodId);
        Task PerformYearEndClosingAsync(Guid fiscalYearId);
        Task GenerateOpeningBalancesAsync(Guid fiscalYearId);
        
        // Year-end closing validation
        Task<YearEndClosingValidationResult> ValidateYearEndClosingAsync(Guid fiscalYearId);
    }

    public class PeriodManagementService : IPeriodManagementService
    {
        private readonly IRepository<FiscalYear> _fiscalYearRepository;
        private readonly IRepository<FiscalPeriod> _fiscalPeriodRepository;
        private readonly IRepository<Account> _accountRepository;
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IGeneralLedgerService _generalLedgerService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PeriodManagementService(
            IRepository<FiscalYear> fiscalYearRepository,
            IRepository<FiscalPeriod> fiscalPeriodRepository,
            IRepository<Account> accountRepository,
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IGeneralLedgerService generalLedgerService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _fiscalYearRepository = fiscalYearRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
            _accountRepository = accountRepository;
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _generalLedgerService = generalLedgerService;
            _auditService = auditService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Fiscal Year Methods

        public async Task<IEnumerable<FiscalYear>> GetFiscalYearsAsync(bool? isActive = null)
        {
            var specification = new Specification<FiscalYear>(fy => true);

            if (isActive.HasValue)
            {
                specification = specification.And(fy => fy.IsActive == isActive.Value);
            }

            return await _fiscalYearRepository.FindAllAsync(specification);
        }

        public async Task<FiscalYear> GetFiscalYearByIdAsync(Guid id)
        {
            return await _fiscalYearRepository.GetByIdAsync(id);
        }

        public async Task<FiscalYear> GetFiscalYearByCodeAsync(string code)
        {
            var fiscalYears = await _fiscalYearRepository.FindAllAsync(fy => fy.Code == code);
            return fiscalYears.FirstOrDefault();
        }

        public async Task<FiscalYear> GetFiscalYearByDateAsync(DateTime date)
        {
            var fiscalYears = await _fiscalYearRepository.FindAllAsync(
                fy => fy.StartDate <= date && fy.EndDate >= date);
            return fiscalYears.FirstOrDefault();
        }

        public async Task AddFiscalYearAsync(FiscalYear fiscalYear)
        {
            // Validate fiscal year
            await ValidateFiscalYearAsync(fiscalYear);

            // Set created by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalYear.CreatedById = userId;
            fiscalYear.CreatedDate = DateTime.Now;

            await _fiscalYearRepository.AddAsync(fiscalYear);
            await _auditService.LogActivityAsync("FiscalYear", "Create", $"Created fiscal year: {fiscalYear.Name}");

            // Generate fiscal periods
            await GenerateFiscalPeriodsAsync(fiscalYear);
        }

        public async Task UpdateFiscalYearAsync(FiscalYear fiscalYear)
        {
            // Validate fiscal year
            await ValidateFiscalYearAsync(fiscalYear, true);

            // Set modified by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalYear.ModifiedById = userId;
            fiscalYear.ModifiedDate = DateTime.Now;

            await _fiscalYearRepository.UpdateAsync(fiscalYear);
            await _auditService.LogActivityAsync("FiscalYear", "Update", $"Updated fiscal year: {fiscalYear.Name}");
        }

        public async Task DeleteFiscalYearAsync(Guid id)
        {
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(id);
            if (fiscalYear == null)
            {
                throw new ArgumentException("Fiscal year not found");
            }

            // Check if fiscal year has periods
            var periods = await _fiscalPeriodRepository.FindAllAsync(fp => fp.FiscalYearId == id);
            if (periods.Any())
            {
                throw new InvalidOperationException("Cannot delete fiscal year with associated periods");
            }

            await _fiscalYearRepository.DeleteAsync(id);
            await _auditService.LogActivityAsync("FiscalYear", "Delete", $"Deleted fiscal year: {fiscalYear.Name}");
        }

        #endregion

        #region Fiscal Period Methods

        public async Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsAsync(Guid? fiscalYearId = null, bool? isActive = null, bool? isClosed = null)
        {
            var specification = new Specification<FiscalPeriod>(fp => true);

            if (fiscalYearId.HasValue)
            {
                specification = specification.And(fp => fp.FiscalYearId == fiscalYearId.Value);
            }

            if (isActive.HasValue)
            {
                specification = specification.And(fp => fp.IsActive == isActive.Value);
            }

            if (isClosed.HasValue)
            {
                specification = specification.And(fp => fp.IsClosed == isClosed.Value);
            }

            return await _fiscalPeriodRepository.FindAllAsync(specification);
        }

        public async Task<IEnumerable<FiscalPeriod>> GetOpenFiscalPeriodsAsync(Guid? fiscalYearId = null)
        {
            // Get open fiscal periods (not closed)
            return await GetFiscalPeriodsAsync(fiscalYearId, true, false);
        }

        public async Task<FiscalPeriod> GetFiscalPeriodByIdAsync(Guid id)
        {
            return await _fiscalPeriodRepository.GetByIdAsync(id);
        }

        public async Task<FiscalPeriod> GetFiscalPeriodByDateAsync(DateTime date)
        {
            var fiscalPeriods = await _fiscalPeriodRepository.FindAllAsync(
                fp => fp.StartDate <= date && fp.EndDate >= date);
            return fiscalPeriods.FirstOrDefault();
        }

        public async Task AddFiscalPeriodAsync(FiscalPeriod fiscalPeriod)
        {
            // Validate fiscal period
            await ValidateFiscalPeriodAsync(fiscalPeriod);

            // Set created by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalPeriod.CreatedById = userId;
            fiscalPeriod.CreatedDate = DateTime.Now;

            await _fiscalPeriodRepository.AddAsync(fiscalPeriod);
            await _auditService.LogActivityAsync("FiscalPeriod", "Create", $"Created fiscal period: {fiscalPeriod.Name}");
        }

        public async Task UpdateFiscalPeriodAsync(FiscalPeriod fiscalPeriod)
        {
            // Validate fiscal period
            await ValidateFiscalPeriodAsync(fiscalPeriod, true);

            // Set modified by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalPeriod.ModifiedById = userId;
            fiscalPeriod.ModifiedDate = DateTime.Now;

            await _fiscalPeriodRepository.UpdateAsync(fiscalPeriod);
            await _auditService.LogActivityAsync("FiscalPeriod", "Update", $"Updated fiscal period: {fiscalPeriod.Name}");
        }

        public async Task DeleteFiscalPeriodAsync(Guid id)
        {
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(id);
            if (fiscalPeriod == null)
            {
                throw new ArgumentException("Fiscal period not found");
            }

            // Check if fiscal period has journal entries
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.PostingDate >= fiscalPeriod.StartDate && je.PostingDate <= fiscalPeriod.EndDate);
            if (journalEntries.Any())
            {
                throw new InvalidOperationException("Cannot delete fiscal period with associated journal entries");
            }

            await _fiscalPeriodRepository.DeleteAsync(id);
            await _auditService.LogActivityAsync("FiscalPeriod", "Delete", $"Deleted fiscal period: {fiscalPeriod.Name}");
        }

        #endregion

        #region Period Management Methods

        public async Task OpenPeriodAsync(Guid periodId)
        {
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(periodId);
            if (fiscalPeriod == null)
            {
                throw new ArgumentException("Fiscal period not found");
            }

            // Check if previous periods are closed
            var previousPeriods = await _fiscalPeriodRepository.FindAllAsync(
                fp => fp.FiscalYearId == fiscalPeriod.FiscalYearId && fp.StartDate < fiscalPeriod.StartDate);
            
            if (previousPeriods.Any(fp => !fp.IsClosed))
            {
                throw new InvalidOperationException("Cannot open period when previous periods are still open");
            }

            // Open period
            fiscalPeriod.IsClosed = false;
            fiscalPeriod.ClosedById = null;
            fiscalPeriod.ClosedDate = null;

            // Set modified by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalPeriod.ModifiedById = userId;
            fiscalPeriod.ModifiedDate = DateTime.Now;

            await _fiscalPeriodRepository.UpdateAsync(fiscalPeriod);
            await _auditService.LogActivityAsync("FiscalPeriod", "Open", $"Opened fiscal period: {fiscalPeriod.Name}");
        }

        public async Task ClosePeriodAsync(Guid periodId)
        {
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(periodId);
            if (fiscalPeriod == null)
            {
                throw new ArgumentException("Fiscal period not found");
            }

            // Check if period can be closed
            var canClose = await CanClosePeriodAsync(periodId);
            if (!canClose)
            {
                throw new InvalidOperationException("Cannot close period with pending journal entries");
            }

            // Close period
            fiscalPeriod.IsClosed = true;
            
            // Set closed by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalPeriod.ClosedById = userId;
            fiscalPeriod.ClosedDate = DateTime.Now;
            fiscalPeriod.ModifiedById = userId;
            fiscalPeriod.ModifiedDate = DateTime.Now;

            await _fiscalPeriodRepository.UpdateAsync(fiscalPeriod);
            await _auditService.LogActivityAsync("FiscalPeriod", "Close", $"Closed fiscal period: {fiscalPeriod.Name}");
        }

        public async Task<bool> IsPeriodClosedAsync(DateTime date)
        {
            var fiscalPeriod = await GetFiscalPeriodByDateAsync(date);
            if (fiscalPeriod == null)
            {
                // If no period is defined for this date, consider it closed
                return true;
            }

            return fiscalPeriod.IsClosed;
        }

        public async Task<bool> CanClosePeriodAsync(Guid periodId)
        {
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(periodId);
            if (fiscalPeriod == null)
            {
                throw new ArgumentException("Fiscal period not found");
            }

            // Check if there are any pending journal entries in this period
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.PostingDate >= fiscalPeriod.StartDate && 
                      je.PostingDate <= fiscalPeriod.EndDate &&
                      (je.Status == JournalEntryStatus.Draft || je.Status == JournalEntryStatus.PendingApproval));

            return !journalEntries.Any();
        }

        public async Task<YearEndClosingValidationResult> ValidateYearEndClosingAsync(Guid fiscalYearId)
        {
            var result = new YearEndClosingValidationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };

            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalYearId);
            if (fiscalYear == null)
            {
                result.IsValid = false;
                result.Messages.Add("Fiscal year not found");
                return result;
            }

            // Check if fiscal year is already closed
            if (fiscalYear.IsClosed)
            {
                result.IsValid = false;
                result.Messages.Add("Fiscal year is already closed");
                return result;
            }

            // Check if all periods in the fiscal year are closed
            var periods = await _fiscalPeriodRepository.FindAllAsync(fp => fp.FiscalYearId == fiscalYearId);
            var openPeriods = periods.Where(p => !p.IsClosed).ToList();
            
            if (openPeriods.Any())
            {
                result.IsValid = false;
                result.Messages.Add($"There are {openPeriods.Count} open periods that must be closed first");
                foreach (var period in openPeriods)
                {
                    result.Messages.Add($"- {period.Name} ({period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd})");
                }
            }

            // Check if next fiscal year exists
            var nextFiscalYear = await _fiscalYearRepository.FindAllAsync(
                fy => fy.StartDate == fiscalYear.EndDate.AddDays(1));
            
            if (!nextFiscalYear.Any())
            {
                result.IsValid = false;
                result.Messages.Add("Next fiscal year not found. You must create the next fiscal year before closing the current one");
            }

            // Check if there are any unposted journal entries
            var unpostedEntries = await _journalEntryRepository.FindAllAsync(
                je => je.FiscalPeriod.FiscalYearId == fiscalYearId && 
                      je.Status != JournalEntryStatus.Posted);
            
            if (unpostedEntries.Any())
            {
                result.IsValid = false;
                result.Messages.Add($"There are {unpostedEntries.Count()} unposted journal entries that must be posted or deleted");
            }

            return result;
        }

        public async Task PerformYearEndClosingAsync(Guid fiscalYearId)
        {
            // Validate year-end closing
            var validationResult = await ValidateYearEndClosingAsync(fiscalYearId);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Cannot perform year-end closing: {string.Join(", ", validationResult.Messages)}");
            }

            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalYearId);
            
            // Find next fiscal year
            var nextFiscalYears = await _fiscalYearRepository.FindAllAsync(
                fy => fy.StartDate == fiscalYear.EndDate.AddDays(1));
            var nextFiscalYear = nextFiscalYears.FirstOrDefault();
            
            if (nextFiscalYear == null)
            {
                throw new InvalidOperationException("Next fiscal year not found");
            }

            // Generate opening balances for next fiscal year
            await GenerateOpeningBalancesAsync(nextFiscalYear.Id);

            // Close fiscal year
            fiscalYear.IsClosed = true;
            
            // Set closed by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            fiscalYear.ClosedById = userId;
            fiscalYear.ClosedDate = DateTime.Now;
            fiscalYear.ModifiedById = userId;
            fiscalYear.ModifiedDate = DateTime.Now;

            await _fiscalYearRepository.UpdateAsync(fiscalYear);
            await _auditService.LogActivityAsync("FiscalYear", "Close", $"Closed fiscal year: {fiscalYear.Name}");
        }

        public async Task GenerateOpeningBalancesAsync(Guid fiscalYearId)
        {
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalYearId);
            if (fiscalYear == null)
            {
                throw new ArgumentException("Fiscal year not found");
            }

            // Find previous fiscal year
            var previousFiscalYears = await _fiscalYearRepository.FindAllAsync(
                fy => fy.EndDate.AddDays(1) == fiscalYear.StartDate);
            var previousFiscalYear = previousFiscalYears.FirstOrDefault();
            
            if (previousFiscalYear == null)
            {
                throw new InvalidOperationException("Previous fiscal year not found");
            }

            // Get first period of new fiscal year
            var firstPeriod = await _fiscalPeriodRepository.FindAllAsync(
                fp => fp.FiscalYearId == fiscalYearId);
            var openingPeriod = firstPeriod.OrderBy(fp => fp.StartDate).FirstOrDefault();
            
            if (openingPeriod == null)
            {
                throw new InvalidOperationException("No periods found in the fiscal year");
            }

            // Get all balance sheet accounts (Assets, Liabilities, Equity)
            var balanceSheetAccounts = await _accountRepository.FindAllAsync(
                a => a.Type == AccountType.Asset || 
                     a.Type == AccountType.Liability || 
                     a.Type == AccountType.Equity);

            // Get retained earnings account
            var retainedEarningsAccount = await _accountRepository.FindAllAsync(
                a => a.IsRetainedEarnings);
            var retainedEarnings = retainedEarningsAccount.FirstOrDefault();
            
            if (retainedEarnings == null)
            {
                throw new InvalidOperationException("Retained earnings account not found");
            }

            // Calculate net income/loss for previous fiscal year
            var revenueAccounts = await _accountRepository.FindAllAsync(a => a.Type == AccountType.Revenue);
            var expenseAccounts = await _accountRepository.FindAllAsync(a => a.Type == AccountType.Expense);
            
            decimal totalRevenue = 0;
            decimal totalExpenses = 0;
            
            foreach (var account in revenueAccounts)
            {
                var balance = await _generalLedgerService.GetAccountBalanceAsync(
                    account.Id, previousFiscalYear.StartDate, previousFiscalYear.EndDate);
                totalRevenue += balance;
            }
            
            foreach (var account in expenseAccounts)
            {
                var balance = await _generalLedgerService.GetAccountBalanceAsync(
                    account.Id, previousFiscalYear.StartDate, previousFiscalYear.EndDate);
                totalExpenses += balance;
            }
            
            decimal netIncome = totalRevenue - totalExpenses;

            // Create opening balance journal entry
            var journalEntry = new JournalEntry
            {
                Number = $"OB-{fiscalYear.Code}",
                Date = fiscalYear.StartDate,
                EntryDate = DateTime.Now,
                PostingDate = fiscalYear.StartDate,
                Description = $"Opening balances for fiscal year {fiscalYear.Name}",
                Reference = $"Year-end closing {previousFiscalYear.Name}",
                Status = JournalEntryStatus.Posted,
                FiscalPeriodId = openingPeriod.Id,
                IsSystemGenerated = true,
                CreatedById = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.Now,
                PostedById = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                PostedDate = DateTime.Now,
                Lines = new List<JournalEntryLine>()
            };

            // Add balance sheet account balances
            foreach (var account in balanceSheetAccounts.Where(a => !a.IsRetainedEarnings))
            {
                var balance = await _generalLedgerService.GetAccountBalanceAsync(
                    account.Id, null, previousFiscalYear.EndDate);
                
                if (balance != 0)
                {
                    var line = new JournalEntryLine
                    {
                        AccountId = account.Id,
                        Description = $"Opening balance for {account.Code} - {account.NameEn}",
                        DebitAmount = balance > 0 && account.Type == AccountType.Asset ? balance : 
                                     balance < 0 && (account.Type == AccountType.Liability || account.Type == AccountType.Equity) ? Math.Abs(balance) : 0,
                        CreditAmount = balance < 0 && account.Type == AccountType.Asset ? Math.Abs(balance) : 
                                      balance > 0 && (account.Type == AccountType.Liability || account.Type == AccountType.Equity) ? balance : 0
                    };
                    
                    journalEntry.Lines.Add(line);
                }
            }

            // Add retained earnings + net income
            var retainedEarningsBalance = await _generalLedgerService.GetAccountBalanceAsync(
                retainedEarnings.Id, null, previousFiscalYear.EndDate);
            
            var totalRetainedEarnings = retainedEarningsBalance + netIncome;
            
            if (totalRetainedEarnings != 0)
            {
                var line = new JournalEntryLine
                {
                    AccountId = retainedEarnings.Id,
                    Description = $"Retained earnings including net income/loss of {netIncome:C}",
                    DebitAmount = totalRetainedEarnings < 0 ? Math.Abs(totalRetainedEarnings) : 0,
                    CreditAmount = totalRetainedEarnings > 0 ? totalRetainedEarnings : 0
                };
                
                journalEntry.Lines.Add(line);
            }

            // Calculate totals
            journalEntry.TotalDebit = journalEntry.Lines.Sum(l => l.DebitAmount);
            journalEntry.TotalCredit = journalEntry.Lines.Sum(l => l.CreditAmount);

            // Save journal entry
            await _journalEntryRepository.AddAsync(journalEntry);
            await _auditService.LogActivityAsync("JournalEntry", "Create", 
                $"Created opening balance journal entry for fiscal year {fiscalYear.Name}");
        }

        #endregion

        #region Helper Methods

        private async Task ValidateFiscalYearAsync(FiscalYear fiscalYear, bool isUpdate = false)
        {
            // Check if dates are valid
            if (fiscalYear.StartDate >= fiscalYear.EndDate)
            {
                throw new ArgumentException("Start date must be before end date");
            }

            // Check if code is unique
            var existingByCode = await _fiscalYearRepository.FindAllAsync(fy => fy.Code == fiscalYear.Code);
            if (existingByCode.Any(fy => !isUpdate || fy.Id != fiscalYear.Id))
            {
                throw new ArgumentException($"Fiscal year with code {fiscalYear.Code} already exists");
            }

            // Check if dates overlap with existing fiscal years
            var existingFiscalYears = await _fiscalYearRepository.FindAllAsync(fy => true);
            foreach (var existing in existingFiscalYears)
            {
                if (isUpdate && existing.Id == fiscalYear.Id)
                {
                    continue;
                }

                if ((fiscalYear.StartDate >= existing.StartDate && fiscalYear.StartDate <= existing.EndDate) ||
                    (fiscalYear.EndDate >= existing.StartDate && fiscalYear.EndDate <= existing.EndDate) ||
                    (fiscalYear.StartDate <= existing.StartDate && fiscalYear.EndDate >= existing.EndDate))
                {
                    throw new ArgumentException($"Fiscal year dates overlap with existing fiscal year {existing.Name}");
                }
            }
        }

        private async Task ValidateFiscalPeriodAsync(FiscalPeriod fiscalPeriod, bool isUpdate = false)
        {
            // Check if dates are valid
            if (fiscalPeriod.StartDate >= fiscalPeriod.EndDate)
            {
                throw new ArgumentException("Start date must be before end date");
            }

            // Check if fiscal year exists
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalPeriod.FiscalYearId);
            if (fiscalYear == null)
            {
                throw new ArgumentException("Fiscal year not found");
            }

            // Check if dates are within fiscal year
            if (fiscalPeriod.StartDate < fiscalYear.StartDate || fiscalPeriod.EndDate > fiscalYear.EndDate)
            {
                throw new ArgumentException("Fiscal period dates must be within fiscal year dates");
            }

            // Check if code is unique within fiscal year
            var existingByCode = await _fiscalPeriodRepository.FindAllAsync(
                fp => fp.Code == fiscalPeriod.Code && fp.FiscalYearId == fiscalPeriod.FiscalYearId);
            if (existingByCode.Any(fp => !isUpdate || fp.Id != fiscalPeriod.Id))
            {
                throw new ArgumentException($"Fiscal period with code {fiscalPeriod.Code} already exists in this fiscal year");
            }

            // Check if dates overlap with existing fiscal periods in the same fiscal year
            var existingFiscalPeriods = await _fiscalPeriodRepository.FindAllAsync(
                fp => fp.FiscalYearId == fiscalPeriod.FiscalYearId);
            foreach (var existing in existingFiscalPeriods)
            {
                if (isUpdate && existing.Id == fiscalPeriod.Id)
                {
                    continue;
                }

                if ((fiscalPeriod.StartDate >= existing.StartDate && fiscalPeriod.StartDate <= existing.EndDate) ||
                    (fiscalPeriod.EndDate >= existing.StartDate && fiscalPeriod.EndDate <= existing.EndDate) ||
                    (fiscalPeriod.StartDate <= existing.StartDate && fiscalPeriod.EndDate >= existing.EndDate))
                {
                    throw new ArgumentException($"Fiscal period dates overlap with existing fiscal period {existing.Name}");
                }
            }
        }

        private async Task GenerateFiscalPeriodsAsync(FiscalYear fiscalYear)
        {
            // Generate monthly periods
            var startDate = fiscalYear.StartDate;
            var endDate = fiscalYear.EndDate;
            var currentDate = startDate;
            var periodNumber = 1;

            while (currentDate <= endDate)
            {
                var periodEndDate = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                if (periodEndDate > endDate)
                {
                    periodEndDate = endDate;
                }

                var fiscalPeriod = new FiscalPeriod
                {
                    FiscalYearId = fiscalYear.Id,
                    Code = $"{fiscalYear.Code}-{periodNumber:D2}",
                    Name = $"{currentDate:MMM yyyy}",
                    StartDate = currentDate,
                    EndDate = periodEndDate,
                    IsActive = true,
                    IsClosed = false,
                    CreatedById = fiscalYear.CreatedById,
                    CreatedDate = DateTime.Now
                };

                await _fiscalPeriodRepository.AddAsync(fiscalPeriod);
                await _auditService.LogActivityAsync("FiscalPeriod", "Create", $"Created fiscal period: {fiscalPeriod.Name}");

                currentDate = periodEndDate.AddDays(1);
                periodNumber++;
            }
        }

        #endregion
    }

    public class YearEndClosingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }
}
