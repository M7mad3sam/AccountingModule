using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface ITaxService
    {
        // Tax Rate methods
        Task<TaxRate> GetTaxRateByIdAsync(Guid id);
        Task<IEnumerable<TaxRate>> GetTaxRatesAsync(bool? isActive = null);
        Task<IEnumerable<TaxRate>> GetAllTaxRatesAsync();
        Task<IEnumerable<TaxRate>> GetActiveTaxRatesAsync();
        Task<TaxRate> CreateTaxRateAsync(TaxRate taxRate);
        Task<TaxRate> AddTaxRateAsync(TaxRate taxRate);
        Task UpdateTaxRateAsync(TaxRate taxRate);
        Task DeleteTaxRateAsync(Guid id);
        Task<bool> CanDeleteTaxRateAsync(Guid id);
        Task<IEnumerable<SelectListItem>> GetTaxRateSelectListAsync(bool? isActive = null);
        Task<TaxCalculationResult> CalculateTaxAsync(decimal amount, Guid taxRateId);

        // Withholding Tax methods
        Task<WithholdingTax> GetWithholdingTaxByIdAsync(Guid id);
        Task<IEnumerable<WithholdingTax>> GetWithholdingTaxesAsync(bool? isActive = null);
        Task<IEnumerable<WithholdingTax>> GetAllWithholdingTaxesAsync();
        Task<WithholdingTax> CreateWithholdingTaxAsync(WithholdingTax withholdingTax);
        Task<WithholdingTax> AddWithholdingTaxAsync(WithholdingTax withholdingTax);
        Task UpdateWithholdingTaxAsync(WithholdingTax withholdingTax);
        Task DeleteWithholdingTaxAsync(Guid id);
        Task<bool> CanDeleteWithholdingTaxAsync(Guid id);
        Task<IEnumerable<SelectListItem>> GetWithholdingTaxSelectListAsync(bool? isActive = null);
    }

    public class TaxService : ITaxService
    {
        private readonly IRepository<TaxRate> _taxRateRepository;
        private readonly IRepository<WithholdingTax> _withholdingTaxRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;

        public TaxService(
            IRepository<TaxRate> taxRateRepository,
            IRepository<WithholdingTax> withholdingTaxRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository)
        {
            _taxRateRepository = taxRateRepository;
            _withholdingTaxRepository = withholdingTaxRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
        }

        // Tax Rate methods
        public async Task<TaxRate> GetTaxRateByIdAsync(Guid id)
        {
            return await _taxRateRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TaxRate>> GetTaxRatesAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _taxRateRepository.FindAllAsync(tr => tr.IsActive == isActive.Value);
            }
            return await _taxRateRepository.GetAllAsync();
        }

        public async Task<TaxRate> CreateTaxRateAsync(TaxRate taxRate)
        {
            await _taxRateRepository.AddAsync(taxRate);
            await _taxRateRepository.SaveAsync();
            return taxRate;
        }

        public async Task UpdateTaxRateAsync(TaxRate taxRate)
        {
            _taxRateRepository.Update(taxRate);
            await _taxRateRepository.SaveAsync();
        }

        public async Task DeleteTaxRateAsync(Guid id)
        {
            var taxRate = await _taxRateRepository.GetByIdAsync(id);
            if (taxRate != null)
            {
                _taxRateRepository.Delete(taxRate);
                await _taxRateRepository.SaveAsync();
            }
        }

        public async Task<bool> CanDeleteTaxRateAsync(Guid id)
        {
            return !await _journalEntryLineRepository.ExistsAsync(jel => jel.TaxRateId == id);
        }

        public async Task<IEnumerable<TaxRate>> GetAllTaxRatesAsync()
        {
            return await _taxRateRepository.GetAllAsync();
        }

        public async Task<IEnumerable<TaxRate>> GetActiveTaxRatesAsync()
        {
            return await _taxRateRepository.FindAllAsync(tr => tr.IsActive);
        }

        public async Task<TaxRate> AddTaxRateAsync(TaxRate taxRate)
        {
            await _taxRateRepository.AddAsync(taxRate);
            await _taxRateRepository.SaveAsync();
            return taxRate;
        }

        public async Task<TaxCalculationResult> CalculateTaxAsync(decimal amount, Guid taxRateId)
        {
            var taxRate = await GetTaxRateByIdAsync(taxRateId);
            if (taxRate == null)
                return new TaxCalculationResult { TaxAmount = 0, AmountAfterTax = amount };
            
            decimal taxAmount = amount * (taxRate.Rate / 100);
            return new TaxCalculationResult
            {
                TaxAmount = taxAmount,
                AmountAfterTax = amount + taxAmount
            };
        }

        public async Task<IEnumerable<SelectListItem>> GetTaxRateSelectListAsync(bool? isActive = null)
        {
            var taxRates = await GetTaxRatesAsync(isActive);
            return taxRates.Select(tr => new SelectListItem
            {
                Value = tr.Id.ToString(),
                Text = $"{tr.Code} - {tr.NameEn} ({tr.Rate}%)"
            }).ToList();
        }

        // Withholding Tax methods
        public async Task<WithholdingTax> GetWithholdingTaxByIdAsync(Guid id)
        {
            return await _withholdingTaxRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<WithholdingTax>> GetWithholdingTaxesAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _withholdingTaxRepository.FindAllAsync(wt => wt.IsActive == isActive.Value);
            }
            return await _withholdingTaxRepository.GetAllAsync();
        }

        public async Task<WithholdingTax> CreateWithholdingTaxAsync(WithholdingTax withholdingTax)
        {
            await _withholdingTaxRepository.AddAsync(withholdingTax);
            await _withholdingTaxRepository.SaveAsync();
            return withholdingTax;
        }

        public async Task UpdateWithholdingTaxAsync(WithholdingTax withholdingTax)
        {
            _withholdingTaxRepository.Update(withholdingTax);
            await _withholdingTaxRepository.SaveAsync();
        }

        public async Task DeleteWithholdingTaxAsync(Guid id)
        {
            var withholdingTax = await _withholdingTaxRepository.GetByIdAsync(id);
            if (withholdingTax != null)
            {
                _withholdingTaxRepository.Delete(withholdingTax);
                await _withholdingTaxRepository.SaveAsync();
            }
        }

        public async Task<bool> CanDeleteWithholdingTaxAsync(Guid id)
        {
            return !await _journalEntryLineRepository.ExistsAsync(jel => jel.WithholdingTaxId == id);
        }

        public async Task<IEnumerable<WithholdingTax>> GetAllWithholdingTaxesAsync()
        {
            return await _withholdingTaxRepository.GetAllAsync();
        }

        public async Task<WithholdingTax> AddWithholdingTaxAsync(WithholdingTax withholdingTax)
        {
            await _withholdingTaxRepository.AddAsync(withholdingTax);
            await _withholdingTaxRepository.SaveAsync();
            return withholdingTax;
        }

        public async Task<IEnumerable<SelectListItem>> GetWithholdingTaxSelectListAsync(bool? isActive = null)
        {
            var withholdingTaxes = await GetWithholdingTaxesAsync(isActive);
            return withholdingTaxes.Select(wt => new SelectListItem
            {
                Value = wt.Id.ToString(),
                Text = $"{wt.Code} - {wt.NameEn} ({wt.Rate}%)"
            }).ToList();
        }
    }
}
