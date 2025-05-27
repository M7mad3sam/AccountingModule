using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IGeneralLedgerService
    {
        Task<JournalEntry> GetJournalEntryByIdAsync(Guid id);
        Task<JournalEntry> GetJournalEntryWithLinesAsync(Guid id);
        Task<(IEnumerable<JournalEntry> Items, int TotalCount, int TotalPages)> GetJournalEntriesAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            JournalEntryStatus? status = null, 
            int pageIndex = 1, 
            int pageSize = 50);
        Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry);
        Task UpdateJournalEntryAsync(JournalEntry journalEntry);
        Task DeleteJournalEntryAsync(Guid id);
        Task<JournalEntry> ApproveJournalEntryAsync(Guid id, string userId);
        Task<JournalEntry> RejectJournalEntryAsync(Guid id, string userId, string reason);
        Task<bool> ValidateJournalEntryAsync(JournalEntry journalEntry);
        Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityName, string entityId);
    }

    public class GeneralLedgerService : IGeneralLedgerService
    {
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<FiscalPeriod> _fiscalPeriodRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;

        public GeneralLedgerService(
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<FiscalPeriod> fiscalPeriodRepository,
            IRepository<AuditLog> auditLogRepository)
        {
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<JournalEntry> GetJournalEntryByIdAsync(Guid id)
        {
            return await _journalEntryRepository.GetByIdAsync(id, 
                je => je.FiscalPeriod,
                je => je.CreatedBy,
                je => je.ApprovedBy);
        }

        public async Task<JournalEntry> GetJournalEntryWithLinesAsync(Guid id)
        {
            var spec = new JournalEntryWithLinesSpecification(id);
            return await _journalEntryRepository.FindAsync(spec.Criteria,
                je => je.Lines,
                je => je.FiscalPeriod,
                je => je.CreatedBy,
                je => je.ApprovedBy);
        }

        public async Task<(IEnumerable<JournalEntry> Items, int TotalCount, int TotalPages)> GetJournalEntriesAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            JournalEntryStatus? status = null, 
            int pageIndex = 1, 
            int pageSize = 50)
        {
            var filter = BuildJournalEntryFilter(fromDate, toDate, status);
            var orderBy = BuildJournalEntryOrderBy();
            
            return await _journalEntryRepository.GetPagedAsync(
                pageIndex,
                pageSize,
                filter,
                orderBy);
        }

        private System.Linq.Expressions.Expression<Func<JournalEntry, bool>> BuildJournalEntryFilter(
            DateTime? fromDate, 
            DateTime? toDate, 
            JournalEntryStatus? status)
        {
            return je => 
                (!fromDate.HasValue || je.Date >= fromDate) &&
                (!toDate.HasValue || je.Date <= toDate) &&
                (!status.HasValue || je.Status == status);
        }

        private Func<IQueryable<JournalEntry>, IOrderedQueryable<JournalEntry>> BuildJournalEntryOrderBy()
        {
            return query => query.OrderByDescending(je => je.Date).ThenByDescending(je => je.CreatedAt);
        }

        public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry)
        {
            // Validate the journal entry
            if (!await ValidateJournalEntryAsync(journalEntry))
            {
                throw new InvalidOperationException("Journal entry validation failed");
            }

            // Calculate totals
            journalEntry.TotalDebit = journalEntry.Lines.Sum(l => l.DebitAmount);
            journalEntry.TotalCredit = journalEntry.Lines.Sum(l => l.CreditAmount);

            // Set status to Draft
            journalEntry.Status = JournalEntryStatus.Draft;
            journalEntry.CreatedAt = DateTime.UtcNow;

            // Generate journal entry number
            journalEntry.Number = await GenerateJournalEntryNumberAsync();

            using var transaction = await _journalEntryRepository.BeginTransactionAsync();
            try
            {
                await _journalEntryRepository.AddAsync(journalEntry);
                await _journalEntryRepository.SaveAsync();
                
                await _journalEntryRepository.CommitTransactionAsync();
                return journalEntry;
            }
            catch
            {
                _journalEntryRepository.RollbackTransaction();
                throw;
            }
        }

        private async Task<string> GenerateJournalEntryNumberAsync()
        {
            // Get the current year and month
            var now = DateTime.UtcNow;
            var yearMonth = now.ToString("yyyyMM");
            
            // Get the last journal entry number for this year/month
            var lastJournalEntry = await _journalEntryRepository.FindAllAsync(
                je => je.Number.StartsWith(yearMonth),
                je => je.Number)
                .ContinueWith(t => t.Result.OrderByDescending(je => je.Number).FirstOrDefault());
            
            int sequence = 1;
            if (lastJournalEntry != null && int.TryParse(lastJournalEntry.Number.Substring(6), out int lastSequence))
            {
                sequence = lastSequence + 1;
            }
            
            return $"{yearMonth}{sequence:D4}";
        }

        public async Task UpdateJournalEntryAsync(JournalEntry journalEntry)
        {
            // Can only update journal entries in Draft or Rejected status
            var existingJournalEntry = await _journalEntryRepository.GetByIdAsync(journalEntry.Id);
            if (existingJournalEntry == null)
            {
                throw new InvalidOperationException("Journal entry not found");
            }
            
            if (existingJournalEntry.Status != JournalEntryStatus.Draft && existingJournalEntry.Status != JournalEntryStatus.Rejected)
            {
                throw new InvalidOperationException("Cannot update journal entry in current status");
            }

            // Validate the journal entry
            if (!await ValidateJournalEntryAsync(journalEntry))
            {
                throw new InvalidOperationException("Journal entry validation failed");
            }

            // Calculate totals
            journalEntry.TotalDebit = journalEntry.Lines.Sum(l => l.DebitAmount);
            journalEntry.TotalCredit = journalEntry.Lines.Sum(l => l.CreditAmount);

            // Set status to Draft
            journalEntry.Status = JournalEntryStatus.Draft;

            using var transaction = await _journalEntryRepository.BeginTransactionAsync();
            try
            {
                // Delete existing lines
                var existingLines = await _journalEntryLineRepository.FindAllAsync(l => l.JournalEntryId == journalEntry.Id);
                _journalEntryLineRepository.DeleteRange(existingLines);
                
                // Update journal entry
                _journalEntryRepository.Update(journalEntry);
                
                await _journalEntryRepository.SaveAsync();
                await _journalEntryRepository.CommitTransactionAsync();
            }
            catch
            {
                _journalEntryRepository.RollbackTransaction();
                throw;
            }
        }

        public async Task DeleteJournalEntryAsync(Guid id)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new InvalidOperationException("Journal entry not found");
            }
            
            // Can only delete journal entries in Draft or Rejected status
            if (journalEntry.Status != JournalEntryStatus.Draft && journalEntry.Status != JournalEntryStatus.Rejected)
            {
                throw new InvalidOperationException("Cannot delete journal entry in current status");
            }

            using var transaction = await _journalEntryRepository.BeginTransactionAsync();
            try
            {
                // Delete lines first
                var lines = await _journalEntryLineRepository.FindAllAsync(l => l.JournalEntryId == id);
                _journalEntryLineRepository.DeleteRange(lines);
                
                // Delete journal entry
                _journalEntryRepository.Delete(journalEntry);
                
                await _journalEntryRepository.SaveAsync();
                await _journalEntryRepository.CommitTransactionAsync();
            }
            catch
            {
                _journalEntryRepository.RollbackTransaction();
                throw;
            }
        }

        public async Task<JournalEntry> ApproveJournalEntryAsync(Guid id, string userId)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new InvalidOperationException("Journal entry not found");
            }
            
            // Can only approve journal entries in Draft or Pending status
            if (journalEntry.Status != JournalEntryStatus.Draft && journalEntry.Status != JournalEntryStatus.Pending)
            {
                throw new InvalidOperationException("Cannot approve journal entry in current status");
            }

            // Check if fiscal period is locked
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(journalEntry.FiscalPeriodId);
            if (fiscalPeriod.IsLocked || fiscalPeriod.IsClosed)
            {
                throw new InvalidOperationException("Cannot post to a locked or closed fiscal period");
            }

            journalEntry.Status = JournalEntryStatus.Approved;
            journalEntry.ApprovedById = userId;
            journalEntry.ApprovedAt = DateTime.UtcNow;

            _journalEntryRepository.Update(journalEntry);
            await _journalEntryRepository.SaveAsync();

            return journalEntry;
        }

        public async Task<JournalEntry> RejectJournalEntryAsync(Guid id, string userId, string reason)
        {
            var journalEntry = await _journalEntryRepository.GetByIdAsync(id);
            if (journalEntry == null)
            {
                throw new InvalidOperationException("Journal entry not found");
            }
            
            // Can only reject journal entries in Draft or Pending status
            if (journalEntry.Status != JournalEntryStatus.Draft && journalEntry.Status != JournalEntryStatus.Pending)
            {
                throw new InvalidOperationException("Cannot reject journal entry in current status");
            }

            journalEntry.Status = JournalEntryStatus.Rejected;
            journalEntry.ApprovedById = userId;
            journalEntry.ApprovedAt = DateTime.UtcNow;
            journalEntry.RejectionReason = reason;

            _journalEntryRepository.Update(journalEntry);
            await _journalEntryRepository.SaveAsync();

            return journalEntry;
        }

        public async Task<bool> ValidateJournalEntryAsync(JournalEntry journalEntry)
        {
            // Check if fiscal period is locked
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(journalEntry.FiscalPeriodId);
            if (fiscalPeriod.IsLocked || fiscalPeriod.IsClosed)
            {
                return false;
            }

            // Check if journal entry is balanced
            var totalDebit = journalEntry.Lines.Sum(l => l.DebitAmount);
            var totalCredit = journalEntry.Lines.Sum(l => l.CreditAmount);
            
            return Math.Abs(totalDebit - totalCredit) < 0.0001m;
        }

        public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityName, string entityId)
        {
            var spec = new AuditLogsByEntitySpecification(entityName, entityId, 1, 100);
            return await _auditLogRepository.FindAllAsync(spec.Criteria, al => al.User);
        }
    }
}
