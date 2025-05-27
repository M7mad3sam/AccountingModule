using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IPeriodManagementService
    {
        Task<FiscalYear> GetFiscalYearByIdAsync(Guid id);
        Task<IEnumerable<FiscalYear>> GetAllFiscalYearsAsync();
        Task<FiscalYear> CreateFiscalYearAsync(FiscalYear fiscalYear);
        Task UpdateFiscalYearAsync(FiscalYear fiscalYear);
        Task<FiscalPeriod> GetFiscalPeriodByIdAsync(Guid id);
        Task<FiscalPeriod> GetCurrentFiscalPeriodAsync();
        Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsForYearAsync(Guid fiscalYearId);
        Task<FiscalPeriod> CreateFiscalPeriodAsync(FiscalPeriod fiscalPeriod);
        Task UpdateFiscalPeriodAsync(FiscalPeriod fiscalPeriod);
        Task CloseFiscalPeriodAsync(Guid id);
        Task ReopenFiscalPeriodAsync(Guid id);
        Task CloseFiscalYearAsync(Guid id);
    }

    public class PeriodManagementService : IPeriodManagementService
    {
        private readonly IRepository<FiscalYear> _fiscalYearRepository;
        private readonly IRepository<FiscalPeriod> _fiscalPeriodRepository;
        private readonly IRepository<JournalEntry> _journalEntryRepository;

        public PeriodManagementService(
            IRepository<FiscalYear> fiscalYearRepository,
            IRepository<FiscalPeriod> fiscalPeriodRepository,
            IRepository<JournalEntry> journalEntryRepository)
        {
            _fiscalYearRepository = fiscalYearRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
            _journalEntryRepository = journalEntryRepository;
        }

        public async Task<FiscalYear> GetFiscalYearByIdAsync(Guid id)
        {
            return await _fiscalYearRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<FiscalYear>> GetAllFiscalYearsAsync()
        {
            return await _fiscalYearRepository.GetAllAsync();
        }

        public async Task<FiscalYear> CreateFiscalYearAsync(FiscalYear fiscalYear)
        {
            // Validate fiscal year dates
            if (fiscalYear.StartDate >= fiscalYear.EndDate)
            {
                throw new InvalidOperationException("Start date must be before end date");
            }

            // Check for overlapping fiscal years
            var overlappingYears = await _fiscalYearRepository.FindAllAsync(fy =>
                (fiscalYear.StartDate <= fy.EndDate && fiscalYear.EndDate >= fy.StartDate));

            if (overlappingYears.Any())
            {
                throw new InvalidOperationException("Fiscal year overlaps with existing fiscal years");
            }

            await _fiscalYearRepository.AddAsync(fiscalYear);
            await _fiscalYearRepository.SaveAsync();
            return fiscalYear;
        }

        public async Task UpdateFiscalYearAsync(FiscalYear fiscalYear)
        {
            // Validate fiscal year dates
            if (fiscalYear.StartDate >= fiscalYear.EndDate)
            {
                throw new InvalidOperationException("Start date must be before end date");
            }

            // Check for overlapping fiscal years
            var overlappingYears = await _fiscalYearRepository.FindAllAsync(fy =>
                fy.Id != fiscalYear.Id &&
                (fiscalYear.StartDate <= fy.EndDate && fiscalYear.EndDate >= fy.StartDate));

            if (overlappingYears.Any())
            {
                throw new InvalidOperationException("Fiscal year overlaps with existing fiscal years");
            }

            _fiscalYearRepository.Update(fiscalYear);
            await _fiscalYearRepository.SaveAsync();
        }

        public async Task<FiscalPeriod> GetFiscalPeriodByIdAsync(Guid id)
        {
            return await _fiscalPeriodRepository.GetByIdAsync(id, fp => fp.FiscalYear);
        }

        public async Task<FiscalPeriod> GetCurrentFiscalPeriodAsync()
        {
            var today = DateTime.Today;
            var spec = new CurrentFiscalPeriodSpecification(today);
            return await _fiscalPeriodRepository.FindAsync(spec.Criteria, fp => fp.FiscalYear);
        }

        public async Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsForYearAsync(Guid fiscalYearId)
        {
            var spec = new FiscalPeriodsByYearSpecification(fiscalYearId);
            return await _fiscalPeriodRepository.FindAllAsync(spec.Criteria);
        }

        public async Task<FiscalPeriod> CreateFiscalPeriodAsync(FiscalPeriod fiscalPeriod)
        {
            // Validate fiscal period dates
            if (fiscalPeriod.StartDate >= fiscalPeriod.EndDate)
            {
                throw new InvalidOperationException("Start date must be before end date");
            }

            // Get fiscal year
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalPeriod.FiscalYearId);
            if (fiscalYear == null)
            {
                throw new InvalidOperationException("Fiscal year not found");
            }

            // Validate that period is within fiscal year
            if (fiscalPeriod.StartDate < fiscalYear.StartDate || fiscalPeriod.EndDate > fiscalYear.EndDate)
            {
                throw new InvalidOperationException("Fiscal period must be within fiscal year");
            }

            // Check for overlapping periods
            var overlappingPeriods = await _fiscalPeriodRepository.FindAllAsync(fp =>
                fp.FiscalYearId == fiscalPeriod.FiscalYearId &&
                (fiscalPeriod.StartDate <= fp.EndDate && fiscalPeriod.EndDate >= fp.StartDate));

            if (overlappingPeriods.Any())
            {
                throw new InvalidOperationException("Fiscal period overlaps with existing periods");
            }

            await _fiscalPeriodRepository.AddAsync(fiscalPeriod);
            await _fiscalPeriodRepository.SaveAsync();
            return fiscalPeriod;
        }

        public async Task UpdateFiscalPeriodAsync(FiscalPeriod fiscalPeriod)
        {
            // Validate fiscal period dates
            if (fiscalPeriod.StartDate >= fiscalPeriod.EndDate)
            {
                throw new InvalidOperationException("Start date must be before end date");
            }

            // Get fiscal year
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalPeriod.FiscalYearId);
            if (fiscalYear == null)
            {
                throw new InvalidOperationException("Fiscal year not found");
            }

            // Validate that period is within fiscal year
            if (fiscalPeriod.StartDate < fiscalYear.StartDate || fiscalPeriod.EndDate > fiscalYear.EndDate)
            {
                throw new InvalidOperationException("Fiscal period must be within fiscal year");
            }

            // Check for overlapping periods
            var overlappingPeriods = await _fiscalPeriodRepository.FindAllAsync(fp =>
                fp.Id != fiscalPeriod.Id &&
                fp.FiscalYearId == fiscalPeriod.FiscalYearId &&
                (fiscalPeriod.StartDate <= fp.EndDate && fiscalPeriod.EndDate >= fp.StartDate));

            if (overlappingPeriods.Any())
            {
                throw new InvalidOperationException("Fiscal period overlaps with existing periods");
            }

            _fiscalPeriodRepository.Update(fiscalPeriod);
            await _fiscalPeriodRepository.SaveAsync();
        }

        public async Task CloseFiscalPeriodAsync(Guid id)
        {
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(id);
            if (fiscalPeriod == null)
            {
                throw new InvalidOperationException("Fiscal period not found");
            }

            // Check if there are any pending journal entries
            var pendingEntries = await _journalEntryRepository.FindAllAsync(je =>
                je.FiscalPeriodId == id &&
                (je.Status == JournalEntryStatus.Draft || je.Status == JournalEntryStatus.Pending));

            if (pendingEntries.Any())
            {
                throw new InvalidOperationException("Cannot close fiscal period with pending journal entries");
            }

            fiscalPeriod.IsLocked = true;
            _fiscalPeriodRepository.Update(fiscalPeriod);
            await _fiscalPeriodRepository.SaveAsync();
        }

        public async Task ReopenFiscalPeriodAsync(Guid id)
        {
            var fiscalPeriod = await _fiscalPeriodRepository.GetByIdAsync(id);
            if (fiscalPeriod == null)
            {
                throw new InvalidOperationException("Fiscal period not found");
            }

            // Check if fiscal year is closed
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalPeriod.FiscalYearId);
            if (fiscalYear.IsClosed)
            {
                throw new InvalidOperationException("Cannot reopen period in a closed fiscal year");
            }

            // Check if next period has entries
            var nextPeriod = await _fiscalPeriodRepository.FindAsync(fp =>
                fp.FiscalYearId == fiscalPeriod.FiscalYearId &&
                fp.StartDate > fiscalPeriod.EndDate &&
                fp.StartDate == fiscalPeriod.EndDate.AddDays(1));

            if (nextPeriod != null)
            {
                var nextPeriodEntries = await _journalEntryRepository.ExistsAsync(je => je.FiscalPeriodId == nextPeriod.Id);
                if (nextPeriodEntries)
                {
                    throw new InvalidOperationException("Cannot reopen period when next period has entries");
                }
            }

            fiscalPeriod.IsLocked = false;
            _fiscalPeriodRepository.Update(fiscalPeriod);
            await _fiscalPeriodRepository.SaveAsync();
        }

        public async Task CloseFiscalYearAsync(Guid id)
        {
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(id);
            if (fiscalYear == null)
            {
                throw new InvalidOperationException("Fiscal year not found");
            }

            // Get all periods for this fiscal year
            var periods = await _fiscalPeriodRepository.FindAllAsync(fp => fp.FiscalYearId == id);

            // Check if all periods are locked
            if (periods.Any(p => !p.IsLocked))
            {
                throw new InvalidOperationException("All fiscal periods must be locked before closing fiscal year");
            }

            using var transaction = await _fiscalYearRepository.BeginTransactionAsync();
            try
            {
                // Mark all periods as closed
                foreach (var period in periods)
                {
                    period.IsClosed = true;
                    _fiscalPeriodRepository.Update(period);
                }

                // Mark fiscal year as closed
                fiscalYear.IsClosed = true;
                _fiscalYearRepository.Update(fiscalYear);

                await _fiscalYearRepository.SaveAsync();
                await _fiscalYearRepository.CommitTransactionAsync();
            }
            catch
            {
                _fiscalYearRepository.RollbackTransaction();
                throw;
            }
        }
    }
}
