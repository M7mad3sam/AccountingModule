using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface ITaxService
    {
        // Tax Rate methods
        Task<IEnumerable<TaxRate>> GetAllTaxRatesAsync();
        Task<IEnumerable<TaxRate>> GetActiveTaxRatesAsync();
        Task<TaxRate> GetTaxRateByIdAsync(Guid id);
        Task<TaxRate> GetTaxRateByCodeAsync(string code);
        Task AddTaxRateAsync(TaxRate taxRate);
        Task UpdateTaxRateAsync(TaxRate taxRate);
        Task DeleteTaxRateAsync(Guid id);
        Task<IEnumerable<TaxRate>> GetTaxRatesAsync(bool? isActive = null);
        
        // Tax Calculation methods
        Task<TaxCalculationResult> CalculateTaxAsync(decimal amount, Guid taxRateId);
        Task<TaxCalculationResult> CalculateVatAsync(decimal amount, Guid taxRateId, bool inclusive = false);
        
        // Withholding Tax methods
        Task<IEnumerable<WithholdingTax>> GetAllWithholdingTaxesAsync();
        Task<IEnumerable<WithholdingTax>> GetActiveWithholdingTaxesAsync();
        Task<WithholdingTax> GetWithholdingTaxByIdAsync(Guid id);
        Task<WithholdingTax> GetWithholdingTaxByCodeAsync(string code);
        Task AddWithholdingTaxAsync(WithholdingTax withholdingTax);
        Task UpdateWithholdingTaxAsync(WithholdingTax withholdingTax);
        Task DeleteWithholdingTaxAsync(Guid id);
        Task<decimal> CalculateWithholdingTaxAsync(decimal amount, Guid withholdingTaxId);
        Task<IEnumerable<WithholdingTax>> GetApplicableWithholdingTaxesAsync(Guid vendorId, decimal amount);
        Task<IEnumerable<WithholdingTax>> GetWithholdingTaxesAsync(bool? isActive = null);
    }

    public class TaxService : ITaxService
    {
        private readonly IRepository<TaxRate> _taxRateRepository;
        private readonly IRepository<WithholdingTax> _withholdingTaxRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IAuditService _auditService;

        public TaxService(
            IRepository<TaxRate> taxRateRepository,
            IRepository<WithholdingTax> withholdingTaxRepository,
            IRepository<Vendor> vendorRepository,
            IAuditService auditService)
        {
            _taxRateRepository = taxRateRepository;
            _withholdingTaxRepository = withholdingTaxRepository;
            _vendorRepository = vendorRepository;
            _auditService = auditService;
        }

        #region Tax Rate Methods

        public async Task<IEnumerable<TaxRate>> GetAllTaxRatesAsync()
        {
            return await _taxRateRepository.GetAllAsync();
        }

        public async Task<IEnumerable<TaxRate>> GetActiveTaxRatesAsync()
        {
            var today = DateTime.Today;
            return await _taxRateRepository.FindAllAsync(
                t => t.IsActive && 
                     t.EffectiveFrom <= today && 
                     (!t.EffectiveTo.HasValue || t.EffectiveTo.Value >= today));
        }

        public async Task<TaxRate> GetTaxRateByIdAsync(Guid id)
        {
            return await _taxRateRepository.GetByIdAsync(id);
        }

        public async Task<TaxRate> GetTaxRateByCodeAsync(string code)
        {
            var taxRates = await _taxRateRepository.FindAllAsync(t => t.Code == code);
            return taxRates.FirstOrDefault();
        }

        public async Task AddTaxRateAsync(TaxRate taxRate)
        {
            await _taxRateRepository.AddAsync(taxRate);
            await _auditService.LogActivityAsync("Tax", "Create", $"Created tax rate: {taxRate.NameEn}");
        }

        public async Task UpdateTaxRateAsync(TaxRate taxRate)
        {
            await _taxRateRepository.UpdateAsync(taxRate);
            await _auditService.LogActivityAsync("Tax", "Update", $"Updated tax rate: {taxRate.NameEn}");
        }

        public async Task DeleteTaxRateAsync(Guid id)
        {
            var taxRate = await _taxRateRepository.GetByIdAsync(id);
            if (taxRate != null)
            {
                await _taxRateRepository.DeleteAsync(id);
                await _auditService.LogActivityAsync("Tax", "Delete", $"Deleted tax rate: {taxRate.NameEn}");
            }
        }

        public async Task<IEnumerable<TaxRate>> GetTaxRatesAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _taxRateRepository.FindAllAsync(t => t.IsActive == isActive.Value);
            }
            return await _taxRateRepository.GetAllAsync();
        }

        #endregion

        #region Tax Calculation Methods

        public async Task<TaxCalculationResult> CalculateTaxAsync(decimal amount, Guid taxRateId)
        {
            var taxRate = await _taxRateRepository.GetByIdAsync(taxRateId);
            if (taxRate == null)
            {
                throw new ArgumentException("Invalid tax rate ID", nameof(taxRateId));
            }

            var taxAmount = Math.Round(amount * (taxRate.Rate / 100), 2);
            var amountAfterTax = amount + taxAmount;

            return new TaxCalculationResult
            {
                AmountBeforeTax = amount,
                TaxRate = taxRate.Rate,
                TaxAmount = taxAmount,
                AmountAfterTax = amountAfterTax,
                TaxRateId = taxRate.Id,
                TaxRateName = taxRate.NameEn,
                TaxRateCode = taxRate.Code,
                TaxType = taxRate.Type
            };
        }

        public async Task<TaxCalculationResult> CalculateVatAsync(decimal amount, Guid taxRateId, bool inclusive = false)
        {
            var taxRate = await _taxRateRepository.GetByIdAsync(taxRateId);
            if (taxRate == null)
            {
                throw new ArgumentException("Invalid tax rate ID", nameof(taxRateId));
            }

            if (taxRate.Type != TaxType.VAT)
            {
                throw new ArgumentException("Tax rate is not of VAT type", nameof(taxRateId));
            }

            decimal amountBeforeTax, taxAmount, amountAfterTax;

            if (inclusive)
            {
                // If the amount is tax inclusive, we need to extract the tax
                amountAfterTax = amount;
                taxAmount = Math.Round(amount - (amount / (1 + (taxRate.Rate / 100))), 2);
                amountBeforeTax = amount - taxAmount;
            }
            else
            {
                // If the amount is tax exclusive, we add the tax
                amountBeforeTax = amount;
                taxAmount = Math.Round(amount * (taxRate.Rate / 100), 2);
                amountAfterTax = amount + taxAmount;
            }

            return new TaxCalculationResult
            {
                AmountBeforeTax = amountBeforeTax,
                TaxRate = taxRate.Rate,
                TaxAmount = taxAmount,
                AmountAfterTax = amountAfterTax,
                TaxRateId = taxRate.Id,
                TaxRateName = taxRate.NameEn,
                TaxRateCode = taxRate.Code,
                TaxType = taxRate.Type,
                IsInclusive = inclusive
            };
        }

        #endregion

        #region Withholding Tax Methods

        public async Task<IEnumerable<WithholdingTax>> GetAllWithholdingTaxesAsync()
        {
            return await _withholdingTaxRepository.GetAllAsync();
        }

        public async Task<IEnumerable<WithholdingTax>> GetActiveWithholdingTaxesAsync()
        {
            var today = DateTime.Today;
            return await _withholdingTaxRepository.FindAllAsync(
                t => t.IsActive && 
                     t.EffectiveFrom <= today && 
                     (!t.EffectiveTo.HasValue || t.EffectiveTo.Value >= today));
        }

        public async Task<WithholdingTax> GetWithholdingTaxByIdAsync(Guid id)
        {
            return await _withholdingTaxRepository.GetByIdAsync(id);
        }

        public async Task<WithholdingTax> GetWithholdingTaxByCodeAsync(string code)
        {
            var withholdingTaxes = await _withholdingTaxRepository.FindAllAsync(t => t.Code == code);
            return withholdingTaxes.FirstOrDefault();
        }

        public async Task AddWithholdingTaxAsync(WithholdingTax withholdingTax)
        {
            await _withholdingTaxRepository.AddAsync(withholdingTax);
            await _auditService.LogActivityAsync("Tax", "Create", $"Created withholding tax: {withholdingTax.NameEn}");
        }

        public async Task UpdateWithholdingTaxAsync(WithholdingTax withholdingTax)
        {
            await _withholdingTaxRepository.UpdateAsync(withholdingTax);
            await _auditService.LogActivityAsync("Tax", "Update", $"Updated withholding tax: {withholdingTax.NameEn}");
        }

        public async Task DeleteWithholdingTaxAsync(Guid id)
        {
            var withholdingTax = await _withholdingTaxRepository.GetByIdAsync(id);
            if (withholdingTax != null)
            {
                await _withholdingTaxRepository.DeleteAsync(id);
                await _auditService.LogActivityAsync("Tax", "Delete", $"Deleted withholding tax: {withholdingTax.NameEn}");
            }
        }

        public async Task<decimal> CalculateWithholdingTaxAsync(decimal amount, Guid withholdingTaxId)
        {
            var withholdingTax = await _withholdingTaxRepository.GetByIdAsync(withholdingTaxId);
            if (withholdingTax == null)
            {
                throw new ArgumentException("Invalid withholding tax ID", nameof(withholdingTaxId));
            }

            // Check if amount is below minimum threshold
            if (withholdingTax.MinimumThreshold.HasValue && amount < withholdingTax.MinimumThreshold.Value)
            {
                return 0;
            }

            return Math.Round(amount * (withholdingTax.Rate / 100), 2);
        }

        public async Task<IEnumerable<WithholdingTax>> GetApplicableWithholdingTaxesAsync(Guid vendorId, decimal amount)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new ArgumentException("Invalid vendor ID", nameof(vendorId));
            }

            var activeWithholdingTaxes = await GetActiveWithholdingTaxesAsync();
            
            return activeWithholdingTaxes.Where(wt => 
                // Check if the vendor type is applicable
                (string.IsNullOrEmpty(wt.ApplicableVendorTypes) || 
                 wt.ApplicableVendorTypes.Split(',').Any(vt => vt == vendor.Type.ToString())) &&
                // Check if amount is above minimum threshold
                (!wt.MinimumThreshold.HasValue || amount >= wt.MinimumThreshold.Value)
            );
        }

        public async Task<IEnumerable<WithholdingTax>> GetWithholdingTaxesAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _withholdingTaxRepository.FindAllAsync(t => t.IsActive == isActive.Value);
            }
            return await _withholdingTaxRepository.GetAllAsync();
        }

        #endregion
    }

    public class TaxCalculationResult
    {
        public decimal AmountBeforeTax { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal AmountAfterTax { get; set; }
        public Guid TaxRateId { get; set; }
        public string TaxRateName { get; set; }
        public string TaxRateCode { get; set; }
        public TaxType TaxType { get; set; }
        public bool IsInclusive { get; set; }
    }
}
