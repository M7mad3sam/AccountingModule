using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IPeriodValidator
    {
        Task<bool> IsPeriodClosedAsync(DateTime date);
        Task<bool> IsPeriodClosedAsync(Guid periodId);
    }

    public interface IPeriodManagementService : IPeriodValidator
    {
        Task<FiscalYear> GetFiscalYearByIdAsync(Guid id);
        Task<IEnumerable<FiscalYear>> GetAllFiscalYearsAsync();
        Task<IEnumerable<FiscalYear>> GetActiveFiscalYearsAsync();
        Task<IEnumerable<FiscalYear>> GetFiscalYearsAsync(bool? isActive = null);
        Task<FiscalYear> AddFiscalYearAsync(FiscalYear fiscalYear);
        Task UpdateFiscalYearAsync(FiscalYear fiscalYear);
        Task DeleteFiscalYearAsync(Guid id);
        Task<bool> CanDeleteFiscalYearAsync(Guid id);
        Task<FiscalPeriod> GetFiscalPeriodByIdAsync(Guid id);
        Task<IEnumerable<FiscalPeriod>> GetAllFiscalPeriodsAsync();
        Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsAsync(bool? isClosed = null, Guid? fiscalYearId = null);
        Task<IEnumerable<FiscalPeriod>> GetOpenFiscalPeriodsAsync();
        Task<IEnumerable<SelectListItem>> GetOpenFiscalPeriodSelectListAsync();
        Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsByYearAsync(Guid fiscalYearId);
        Task<FiscalPeriod> AddFiscalPeriodAsync(FiscalPeriod fiscalPeriod);
        Task UpdateFiscalPeriodAsync(FiscalPeriod fiscalPeriod);
        Task DeleteFiscalPeriodAsync(Guid id);
        Task<bool> CanDeleteFiscalPeriodAsync(Guid id);
        Task ClosePeriodAsync(Guid id);
        Task OpenPeriodAsync(Guid id);
        Task<YearEndClosingValidationResult> ValidateYearEndClosingAsync(Guid fiscalYearId);
        Task PerformYearEndClosingAsync(Guid fiscalYearId);
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
            return await _fiscalYearRepository.GetByIdAsync(id, fy => fy.FiscalPeriods);
        }

        public async Task<IEnumerable<FiscalYear>> GetAllFiscalYearsAsync()
        {
            return await _fiscalYearRepository.GetAllAsync();
        }

        public async Task<IEnumerable<FiscalYear>> GetActiveFiscalYearsAsync()
        {
            return await _fiscalYearRepository.FindAllAsync(fy => fy.IsActive);
        }

        public async Task<IEnumerable<FiscalYear>> GetFiscalYearsAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _fiscalYearRepository.FindAllAsync(fy => fy.IsActive == isActive.Value);
            }
            return await _fiscalYearRepository.GetAllAsync();
        }

        public async Task<FiscalYear> AddFiscalYearAsync(FiscalYear fiscalYear)
        {
            await _fiscalYearRepository.AddAsync(fiscalYear);
            await _fiscalYearRepository.SaveAsync();
            return fiscalYear;
        }

        public async Task UpdateFiscalYearAsync(FiscalYear fiscalYear)
        {
            _fiscalYearRepository.Update(fiscalYear);
            await _fiscalYearRepository.SaveAsync();
        }

        public async Task DeleteFiscalYearAsync(Guid id)
        {
            await _fiscalYearRepository.DeleteAsync(id);
        }

        public async Task<bool> CanDeleteFiscalYearAsync(Guid id)
        {
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(id, fy => fy.FiscalPeriods);
            if (fiscalYear == null)
                return false;

            return fiscalYear.FiscalPeriods == null || !fiscalYear.FiscalPeriods.Any();
        }

        public async Task<FiscalPeriod> GetFiscalPeriodByIdAsync(Guid id)
        {
            return await _fiscalPeriodRepository.GetByIdAsync(id, fp => fp.FiscalYear);
        }

        public async Task<IEnumerable<FiscalPeriod>> GetAllFiscalPeriodsAsync()
        {
            return await _fiscalPeriodRepository.GetAllAsync();
        }

        public async Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsAsync(bool? isClosed = null, Guid? fiscalYearId = null)
        {
            if (isClosed.HasValue && fiscalYearId.HasValue)
            {
                return await _fiscalPeriodRepository.FindAllAsync(fp => fp.IsClosed == isClosed.Value && fp.FiscalYearId == fiscalYearId.Value);
            }
            else if (isClosed.HasValue)
            {
                return await _fiscalPeriodRepository.FindAllAsync(fp => fp.IsClosed == isClosed.Value);
            }
            else if (fiscalYearId.HasValue)
            {
                return await _fiscalPeriodRepository.FindAllAsync(fp => fp.FiscalYearId == fiscalYearId.Value);
            }
            return await _fiscalPeriodRepository.GetAllAsync();
        }

        public async Task<IEnumerable<FiscalPeriod>> GetOpenFiscalPeriodsAsync()
        {
            return await _fiscalPeriodRepository.FindAllAsync(fp => !fp.IsClosed);
        }

        public async Task<IEnumerable<SelectListItem>> GetOpenFiscalPeriodSelectListAsync()
        {
            var periods = await GetOpenFiscalPeriodsAsync();
            return periods.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} ({p.StartDate:d} - {p.EndDate:d})"
            }).ToList();
        }

        public async Task<IEnumerable<FiscalPeriod>> GetFiscalPeriodsByYearAsync(Guid fiscalYearId)
        {
            var spec = new FiscalPeriodsByYearSpecification(fiscalYearId);
            return await _fiscalPeriodRepository.FindAllAsync(spec.Criteria);
        }

        public async Task<FiscalPeriod> AddFiscalPeriodAsync(FiscalPeriod fiscalPeriod)
        {
            await _fiscalPeriodRepository.AddAsync(fiscalPeriod);
            await _fiscalPeriodRepository.SaveAsync();
            return fiscalPeriod;
        }

        public async Task UpdateFiscalPeriodAsync(FiscalPeriod fiscalPeriod)
        {
            _fiscalPeriodRepository.Update(fiscalPeriod);
            await _fiscalPeriodRepository.SaveAsync();
        }

        public async Task DeleteFiscalPeriodAsync(Guid id)
        {
            await _fiscalPeriodRepository.DeleteAsync(id);
        }

        public async Task<bool> CanDeleteFiscalPeriodAsync(Guid id)
        {
            return !await _journalEntryRepository.ExistsAsync(je => je.FiscalPeriodId == id);
        }

        public async Task<bool> IsPeriodClosedAsync(DateTime date)
        {
            var spec = new CurrentFiscalPeriodSpecification(date);
            var period = await _fiscalPeriodRepository.FindAsync(spec.Criteria, fp => fp.FiscalYear);
            
            if (period == null)
                return true; // No period defined, consider closed
            
            return period.IsClosed || period.FiscalYear.IsClosed;
        }

        public async Task<bool> IsPeriodClosedAsync(Guid periodId)
        {
            var period = await _fiscalPeriodRepository.GetByIdAsync(periodId, fp => fp.FiscalYear);
            if (period == null)
                return true; // Period not found, consider closed
            
            return period.IsClosed || period.FiscalYear.IsClosed;
        }

        public async Task ClosePeriodAsync(Guid id)
        {
            var period = await _fiscalPeriodRepository.GetByIdAsync(id);
            if (period != null)
            {
                period.IsClosed = true;
                period.ClosedDate = DateTime.UtcNow;
                // TODO: Set ClosedById from current user
                _fiscalPeriodRepository.Update(period);
                await _fiscalPeriodRepository.SaveAsync();
            }
        }

        public async Task OpenPeriodAsync(Guid id)
        {
            var period = await _fiscalPeriodRepository.GetByIdAsync(id);
            if (period != null)
            {
                period.IsClosed = false;
                period.ClosedDate = null;
                period.ClosedById = null;
                _fiscalPeriodRepository.Update(period);
                await _fiscalPeriodRepository.SaveAsync();
            }
        }

        public async Task<YearEndClosingValidationResult> ValidateYearEndClosingAsync(Guid fiscalYearId)
        {
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalYearId, fy => fy.FiscalPeriods);
            if (fiscalYear == null)
            {
                return new YearEndClosingValidationResult
                {
                    IsValid = false,
                    ValidationMessages = new List<string> { "Fiscal year not found." }
                };
            }

            var result = new YearEndClosingValidationResult { IsValid = true, ValidationMessages = new List<string>() };

            // Check if all periods in the fiscal year are closed
            if (fiscalYear.FiscalPeriods.Any(fp => !fp.IsClosed))
            {
                result.IsValid = false;
                result.ValidationMessages.Add("Not all fiscal periods are closed. All periods must be closed before year-end closing.");
            }

            // Check if there are any unposted journal entries in the fiscal year
            var hasUnpostedEntries = await _journalEntryRepository.ExistsAsync(je => 
                je.FiscalPeriod.FiscalYearId == fiscalYearId && je.Status != JournalEntryStatus.Posted);
            if (hasUnpostedEntries)
            {
                result.IsValid = false;
                result.ValidationMessages.Add("There are unposted journal entries in this fiscal year. All entries must be posted before year-end closing.");
            }

            return result;
        }

        public async Task PerformYearEndClosingAsync(Guid fiscalYearId)
        {
            var fiscalYear = await _fiscalYearRepository.GetByIdAsync(fiscalYearId, fy => fy.FiscalPeriods);
            if (fiscalYear == null)
            {
                throw new ArgumentException("Fiscal year not found.", nameof(fiscalYearId));
            }

            // Validate before closing
            var validationResult = await ValidateYearEndClosingAsync(fiscalYearId);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException("Year-end closing validation failed: " + string.Join("; ", validationResult.ValidationMessages));
            }

            // Mark fiscal year as closed
            fiscalYear.IsClosed = true;
            fiscalYear.ClosedDate = DateTime.UtcNow;
            // TODO: Set ClosedById from current user

            _fiscalYearRepository.Update(fiscalYear);
            await _fiscalYearRepository.SaveAsync();
        }
    }
}
