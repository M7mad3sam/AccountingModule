using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using AspNetCoreMvcTemplate.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IGeneralLedgerService
    {
        // Journal Entry methods
        Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(
            string searchTerm = null, 
            JournalEntryStatus? status = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            Guid? clientId = null, 
            Guid? vendorId = null, 
            bool? isRecurring = null, 
            bool? isSystemGenerated = null);
        Task<JournalEntry> GetJournalEntryByIdAsync(Guid id);
        Task<JournalEntry> GetJournalEntryByReferenceAsync(string reference);
        Task AddJournalEntryAsync(JournalEntry journalEntry);
        Task UpdateJournalEntryAsync(JournalEntry journalEntry);
        Task DeleteJournalEntryAsync(Guid id);
        Task SubmitForApprovalAsync(Guid id);
        Task ApproveJournalEntryAsync(Guid id, string approvalNotes = null);
        Task RejectJournalEntryAsync(Guid id, string rejectionNotes = null);
        Task PostJournalEntryAsync(Guid id);
        Task<IEnumerable<JournalEntry>> GetRecurringJournalEntriesAsync();
        Task GenerateRecurringJournalEntriesAsync();
        
        // General Ledger methods
        Task<IEnumerable<GeneralLedgerEntry>> GetGeneralLedgerEntriesAsync(
            Guid? accountId = null, 
            Guid? costCenterId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null);
        Task<decimal> GetAccountBalanceAsync(Guid accountId, DateTime asOfDate, Guid? costCenterId = null);
        Task<AccountBalances> GetAccountBalancesAsync(Guid accountId, DateTime fromDate, DateTime toDate, Guid? costCenterId = null);
        
        // Period validation methods
        Task<bool> IsPostingAllowedAsync(DateTime postingDate);
    }

    public class GeneralLedgerService : IGeneralLedgerService
    {
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<Account> _accountRepository;
        private readonly IRepository<CostCenter> _costCenterRepository;
        private readonly IRepository<FiscalPeriod> _fiscalPeriodRepository;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPeriodValidator _periodValidator;

        public GeneralLedgerService(
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<Account> accountRepository,
            IRepository<CostCenter> costCenterRepository,
            IRepository<FiscalPeriod> fiscalPeriodRepository,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IPeriodValidator periodValidator)
        {
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _accountRepository = accountRepository;
            _costCenterRepository = costCenterRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
            _auditService = auditService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _periodValidator = periodValidator;
        }

        #region Journal Entry Methods

        public async Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(
            string searchTerm = null, 
            JournalEntryStatus? status = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            Guid? clientId = null, 
            Guid? vendorId = null, 
            bool? isRecurring = null, 
            bool? isSystemGenerated = null)
        {
            var specification = new Specification<JournalEntry>(je => true);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                specification = specification.And(je => 
                    je.Reference.ToLower().Contains(searchTerm) || 
                    je.Description.ToLower().Contains(searchTerm));
            }

            if (status.HasValue)
            {
                specification = specification.And(je => je.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                specification = specification.And(je => je.PostingDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                specification = specification.And(je => je.PostingDate <= toDate.Value);
            }

            if (clientId.HasValue)
            {
                specification = specification.And(je => je.ClientId == clientId.Value);
            }

            if (vendorId.HasValue)
            {
                specification = specification.And(je => je.VendorId == vendorId.Value);
            }

            if (isRecurring.HasValue)
            {
                specification = specification.And(je => je.IsRecurring == isRecurring.Value);
            }

            if (isSystemGenerated.HasValue)
            {
                specification = specification.And(je => je.IsSystemGenerated == isSystemGenerated.Value);
            }

            return await _journalEntryRepository.FindAllAsync(specification);
        }

        public async Task<JournalEntry> GetJournalEntryByIdAsync(Guid id)
        {
            return await _journalEntryRepository.GetByIdAsync(id, entry => entry.Lines);
        }

        public async Task<JournalEntry> GetJournalEntryByReferenceAsync(string reference)
        {
            var journalEntries = await _journalEntryRepository.FindAllAsync(je => je.Reference == reference);
            return journalEntries.FirstOrDefault();
        }

        public async Task AddJournalEntryAsync(JournalEntry journalEntry)
        {
            // Validate journal entry
            ValidateJournalEntry(journalEntry);

            // Check if reference is unique
            var existingJournalEntry = await GetJournalEntryByReferenceAsync(journalEntry.Reference);
            if (existingJournalEntry != null)
            {
                throw new InvalidOperationException($"A journal entry with reference '{journalEntry.Reference}' already exists");
            }

            // Check if posting is allowed for the posting date
            if (!await IsPostingAllowedAsync(journalEntry.PostingDate))
            {
                throw new InvalidOperationException($"Posting is not allowed for date {journalEntry.PostingDate:yyyy-MM-dd} because the fiscal period is closed");
            }

            // Set created by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            journalEntry.CreatedById = userId;
            journalEntry.CreatedDate = DateTime.Now;
            journalEntry.ApprovedById = userId;
            journalEntry.ModifiedDate = DateTime.Now;

            await _journalEntryRepository.AddAsync(journalEntry);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "Create", $"Created journal entry: {journalEntry.Reference}");
        }

        public async Task UpdateJournalEntryAsync(JournalEntry journalEntry)
        {
            // Validate journal entry
            ValidateJournalEntry(journalEntry);

            // Check if reference is unique (except for this journal entry)
            var existingJournalEntry = await GetJournalEntryByReferenceAsync(journalEntry.Reference);
            if (existingJournalEntry != null && existingJournalEntry.Id != journalEntry.Id)
            {
                throw new InvalidOperationException($"A journal entry with reference '{journalEntry.Reference}' already exists");
            }

            // Check if posting is allowed for the posting date
            if (!await IsPostingAllowedAsync(journalEntry.PostingDate))
            {
                throw new InvalidOperationException($"Posting is not allowed for date {journalEntry.PostingDate:yyyy-MM-dd} because the fiscal period is closed");
            }

            // Set modified by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            journalEntry.ModifiedById = userId;
            journalEntry.ModifiedDate = DateTime.Now;

            await _journalEntryRepository.UpdateAsync(journalEntry);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "Update", $"Updated journal entry: {journalEntry.Reference}");
        }

        public async Task DeleteJournalEntryAsync(Guid id)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new ArgumentException("Journal entry not found");
            }

            // Check if journal entry can be deleted
            if (journalEntry.Status == JournalEntryStatus.Posted)
            {
                throw new InvalidOperationException("Posted journal entries cannot be deleted");
            }

            await _journalEntryRepository.DeleteAsync(id);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "Delete", $"Deleted journal entry: {journalEntry.Reference}");
        }

        public async Task SubmitForApprovalAsync(Guid id)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new ArgumentException("Journal entry not found");
            }

            // Check if journal entry can be submitted for approval
            if (journalEntry.Status != JournalEntryStatus.Draft)
            {
                throw new InvalidOperationException("Only draft journal entries can be submitted for approval");
            }

            // Validate journal entry
            ValidateJournalEntry(journalEntry);

            // Check if posting is allowed for the posting date
            if (!await IsPostingAllowedAsync(journalEntry.PostingDate))
            {
                throw new InvalidOperationException($"Posting is not allowed for date {journalEntry.PostingDate:yyyy-MM-dd} because the fiscal period is closed");
            }

            // Update status
            journalEntry.Status = JournalEntryStatus.PendingApproval;

            // Set modified by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            journalEntry.ModifiedById = userId;
            journalEntry.ModifiedDate = DateTime.Now;

            await _journalEntryRepository.UpdateAsync(journalEntry);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "SubmitForApproval", $"Submitted journal entry for approval: {journalEntry.Reference}");
        }

        public async Task ApproveJournalEntryAsync(Guid id, string approvalNotes = null)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new ArgumentException("Journal entry not found");
            }

            // Check if journal entry can be approved
            if (journalEntry.Status != JournalEntryStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only journal entries with Pending Approval status can be approved");
            }

            // Check if posting is allowed for the posting date
            if (!await IsPostingAllowedAsync(journalEntry.PostingDate))
            {
                throw new InvalidOperationException($"Posting is not allowed for date {journalEntry.PostingDate:yyyy-MM-dd} because the fiscal period is closed");
            }

            // Update status
            journalEntry.Status = JournalEntryStatus.Approved;
            journalEntry.ApprovalNotes = approvalNotes;

            // Set approved by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            journalEntry.ApprovedById = userId;
            journalEntry.ApprovedDate = DateTime.Now;

            await _journalEntryRepository.UpdateAsync(journalEntry);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "Approve", $"Approved journal entry: {journalEntry.Reference}");
        }

        public async Task RejectJournalEntryAsync(Guid id, string rejectionNotes = null)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new ArgumentException("Journal entry not found");
            }

            // Check if journal entry can be rejected
            if (journalEntry.Status != JournalEntryStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only journal entries with Pending Approval status can be rejected");
            }

            // Update status
            journalEntry.Status = JournalEntryStatus.Rejected;
            journalEntry.ApprovalNotes = rejectionNotes;

            // Set rejected by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            journalEntry.ApprovedById = userId; // Using the same field for rejection
            journalEntry.ApprovedDate = DateTime.Now;

            await _journalEntryRepository.UpdateAsync(journalEntry);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "Reject", $"Rejected journal entry: {journalEntry.Reference}");
        }

        public async Task PostJournalEntryAsync(Guid id)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new ArgumentException("Journal entry not found");
            }

            // Check if journal entry can be posted
            if (journalEntry.Status != JournalEntryStatus.Approved)
            {
                throw new InvalidOperationException("Only approved journal entries can be posted");
            }

            // Check if posting is allowed for the posting date
            if (!await IsPostingAllowedAsync(journalEntry.PostingDate))
            {
                throw new InvalidOperationException($"Posting is not allowed for date {journalEntry.PostingDate:yyyy-MM-dd} because the fiscal period is closed");
            }

            // Update status
            journalEntry.Status = JournalEntryStatus.Posted;

            // Set posted by
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            journalEntry.PostedById = userId;
            journalEntry.PostedDate = DateTime.Now;

            await _journalEntryRepository.UpdateAsync(journalEntry);
            await _journalEntryRepository.SaveAsync();
            await _auditService.LogActivityAsync("JournalEntry", "Post", $"Posted journal entry: {journalEntry.Reference}");
        }

        public async Task<IEnumerable<JournalEntry>> GetRecurringJournalEntriesAsync()
        {
            return await _journalEntryRepository.FindAllAsync(je => je.IsRecurring);
        }

        public async Task GenerateRecurringJournalEntriesAsync()
        {
            var today = DateTime.Today;
            var recurringEntries = await _journalEntryRepository.FindAllAsync(
                je => je.IsRecurring && 
                      je.NextRecurrenceDate.HasValue && 
                      je.NextRecurrenceDate.Value <= today &&
                      (!je.EndRecurrenceDate.HasValue || je.EndRecurrenceDate.Value >= today));

            foreach (var recurringEntry in recurringEntries)
            {
                // Check if posting is allowed for today
                if (!await IsPostingAllowedAsync(today))
                {
                    // Skip this recurring entry if posting is not allowed for today
                    continue;
                }

                // Create new journal entry based on recurring entry
                var newJournalEntry = new JournalEntry
                {
                    EntryDate = today,
                    PostingDate = today,
                    Reference = $"{recurringEntry.Reference}-{today:yyyyMMdd}",
                    Description = $"{recurringEntry.Description} (Recurring: {today:yyyy-MM-dd})",
                    Status = JournalEntryStatus.Draft,
                    ClientId = recurringEntry.ClientId,
                    VendorId = recurringEntry.VendorId,
                    Currency = recurringEntry.Currency,
                    ExchangeRate = recurringEntry.ExchangeRate,
                    IsRecurring = false,
                    IsSystemGenerated = true,
                    SourceDocument = recurringEntry.SourceDocument,
                    Notes = recurringEntry.Notes,
                    Lines = new List<JournalEntryLine>()
                };

                // Copy journal entry lines
                foreach (var line in recurringEntry.Lines)
                {
                    newJournalEntry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = line.AccountId,
                        CostCenterId = line.CostCenterId,
                        Description = line.Description,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        TaxRateId = line.TaxRateId,
                        TaxAmount = line.TaxAmount,
                        WithholdingTaxId = line.WithholdingTaxId,
                        WithholdingTaxAmount = line.WithholdingTaxAmount
                    });
                }

                // Add new journal entry
                await AddJournalEntryAsync(newJournalEntry);

                // Update next recurrence date
                recurringEntry.NextRecurrenceDate = CalculateNextRecurrenceDate(recurringEntry.RecurrencePattern, today);
                await _journalEntryRepository.UpdateAsync(recurringEntry);
                await _journalEntryRepository.SaveAsync();
            }
        }

        #endregion

        #region General Ledger Methods

        public async Task<IEnumerable<GeneralLedgerEntry>> GetGeneralLedgerEntriesAsync(
            Guid? accountId = null, 
            Guid? costCenterId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            // Get journal entries
            var journalEntrySpecification = new Specification<JournalEntry>(je => je.Status == JournalEntryStatus.Posted);

            if (fromDate.HasValue)
            {
                journalEntrySpecification = journalEntrySpecification.And(je => je.PostingDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                journalEntrySpecification = journalEntrySpecification.And(je => je.PostingDate <= toDate.Value);
            }

            var journalEntries = await _journalEntryRepository.FindAllAsync(journalEntrySpecification);

            // Get journal entry lines
            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();
            var journalEntryLineSpecification = new Specification<JournalEntryLine>(jel => journalEntryIds.Contains(jel.JournalEntryId));

            if (accountId.HasValue)
            {
                journalEntryLineSpecification = journalEntryLineSpecification.And(jel => jel.AccountId == accountId.Value);
            }

            if (costCenterId.HasValue)
            {
                journalEntryLineSpecification = journalEntryLineSpecification.And(jel => jel.CostCenterId == costCenterId.Value);
            }

            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(journalEntryLineSpecification);

            // Create general ledger entries
            var generalLedgerEntries = new List<GeneralLedgerEntry>();
            foreach (var line in journalEntryLines)
            {
                var journalEntry = journalEntries.First(je => je.Id == line.JournalEntryId);
                var account = await _accountRepository.GetByIdAsync(line.AccountId);
                var costCenter = line.CostCenterId.HasValue ? await _costCenterRepository.GetByIdAsync(line.CostCenterId.Value) : null;

                generalLedgerEntries.Add(new GeneralLedgerEntry
                {
                    JournalEntryId = journalEntry.Id,
                    JournalEntryReference = journalEntry.Reference,
                    PostingDate = journalEntry.PostingDate,
                    AccountId = line.AccountId,
                    AccountCode = account.Code,
                    AccountName = account.NameEn,
                    CostCenterId = line.CostCenterId,
                    CostCenterName = costCenter?.NameEn,
                    Description = line.Description ?? journalEntry.Description,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Balance = line.Debit - line.Credit
                });
            }

            return generalLedgerEntries.OrderBy(gle => gle.PostingDate).ThenBy(gle => gle.JournalEntryReference);
        }

        public async Task<decimal> GetAccountBalanceAsync(Guid accountId, DateTime asOfDate, Guid? costCenterId = null)
        {
            var entries = await GetGeneralLedgerEntriesAsync(accountId, costCenterId, null, asOfDate);
            return entries.Sum(e => e.Debit - e.Credit);
        }

        public async Task<AccountBalances> GetAccountBalancesAsync(Guid accountId, DateTime fromDate, DateTime toDate, Guid? costCenterId = null)
        {
            // Get opening balance
            var openingBalance = await GetAccountBalanceAsync(accountId, fromDate.AddDays(-1), costCenterId);

            // Get period entries
            var periodEntries = await GetGeneralLedgerEntriesAsync(accountId, costCenterId, fromDate, toDate);
            var periodDebits = periodEntries.Sum(e => e.Debit);
            var periodCredits = periodEntries.Sum(e => e.Credit);

            // Calculate closing balance
            var closingBalance = openingBalance + periodDebits - periodCredits;

            return new AccountBalances
            {
                AccountId = accountId,
                FromDate = fromDate,
                ToDate = toDate,
                OpeningBalance = openingBalance,
                PeriodDebits = periodDebits,
                PeriodCredits = periodCredits,
                ClosingBalance = closingBalance
            };
        }

        #endregion

        #region Period Validation Methods

        public async Task<bool> IsPostingAllowedAsync(DateTime postingDate)
        {
            // Check if the fiscal period for the posting date is closed
            var isClosed = await _periodValidator.IsPeriodClosedAsync(postingDate);
            
            // Posting is allowed if the period is not closed
            return !isClosed;
        }

        #endregion

        #region Helper Methods

        private void ValidateJournalEntry(JournalEntry journalEntry)
        {
            // Check if journal entry is balanced
            var totalDebit = journalEntry.Lines.Sum(l => l.Debit);
            var totalCredit = journalEntry.Lines.Sum(l => l.Credit);
            if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            {
                throw new InvalidOperationException("Journal entry must be balanced (total debits must equal total credits)");
            }

            // Check if journal entry has at least two lines
            if (journalEntry.Lines.Count < 2)
            {
                throw new InvalidOperationException("Journal entry must have at least two lines");
            }

            // Check if all lines have an account
            if (journalEntry.Lines.Any(l => l.AccountId == Guid.Empty))
            {
                throw new InvalidOperationException("All journal entry lines must have an account");
            }

            // Check if all lines have either debit or credit
            if (journalEntry.Lines.Any(l => l.Debit == 0 && l.Credit == 0))
            {
                throw new InvalidOperationException("All journal entry lines must have either debit or credit");
            }

            // Check if all lines have either debit or credit, but not both
            if (journalEntry.Lines.Any(l => l.Debit > 0 && l.Credit > 0))
            {
                throw new InvalidOperationException("Journal entry lines cannot have both debit and credit");
            }
        }

        private DateTime? CalculateNextRecurrenceDate(string recurrencePattern, DateTime currentDate)
        {
            if (string.IsNullOrEmpty(recurrencePattern))
            {
                return null;
            }

            var parts = recurrencePattern.Split(':');
            if (parts.Length != 2)
            {
                return null;
            }

            var frequency = parts[0];
            var interval = int.Parse(parts[1]);

            switch (frequency.ToLower())
            {
                case "daily":
                    return currentDate.AddDays(interval);
                case "weekly":
                    return currentDate.AddDays(interval * 7);
                case "monthly":
                    return currentDate.AddMonths(interval);
                case "quarterly":
                    return currentDate.AddMonths(interval * 3);
                case "yearly":
                    return currentDate.AddYears(interval);
                default:
                    return null;
            }
        }

        #endregion
    }

    public class GeneralLedgerEntry
    {
        public Guid JournalEntryId { get; set; }
        public string JournalEntryReference { get; set; }
        public DateTime PostingDate { get; set; }
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public Guid? CostCenterId { get; set; }
        public string CostCenterName { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class AccountBalances
    {
        public Guid AccountId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal PeriodDebits { get; set; }
        public decimal PeriodCredits { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
