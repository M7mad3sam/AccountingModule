using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface ITaxService
    {
        Task<TaxRate> GetTaxRateByIdAsync(Guid id);
        Task<IEnumerable<TaxRate>> GetAllTaxRatesAsync();
        Task<TaxRate> CreateTaxRateAsync(TaxRate taxRate);
        Task UpdateTaxRateAsync(TaxRate taxRate);
        Task DeleteTaxRateAsync(Guid id);
        Task<decimal> CalculateVatAsync(decimal amount, Guid taxRateId);
        Task<decimal> CalculateWithholdingTaxAsync(decimal amount, Guid taxRateId);
        Task<VatReport> GenerateVatReportAsync(DateTime fromDate, DateTime toDate);
        Task<WithholdingTaxReport> GenerateWithholdingTaxReportAsync(DateTime fromDate, DateTime toDate);
        Task<byte[]> ExportVatReportToXmlAsync(VatReport report);
        Task<byte[]> ExportVatReportToCsvAsync(VatReport report);
    }

    public class TaxService : ITaxService
    {
        private readonly IRepository<TaxRate> _taxRateRepository;
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;

        public TaxService(
            IRepository<TaxRate> taxRateRepository,
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository)
        {
            _taxRateRepository = taxRateRepository;
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
        }

        public async Task<TaxRate> GetTaxRateByIdAsync(Guid id)
        {
            return await _taxRateRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TaxRate>> GetAllTaxRatesAsync()
        {
            var spec = new ActiveTaxRatesSpecification();
            return await _taxRateRepository.FindAllAsync(spec.Criteria);
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
            // Check if tax rate is used in journal entry lines
            var isUsed = await _journalEntryLineRepository.ExistsAsync(jel => jel.TaxRateId == id);
            if (isUsed)
            {
                // Instead of deleting, mark as inactive
                var taxRate = await _taxRateRepository.GetByIdAsync(id);
                if (taxRate != null)
                {
                    taxRate.IsActive = false;
                    _taxRateRepository.Update(taxRate);
                    await _taxRateRepository.SaveAsync();
                }
            }
            else
            {
                var taxRate = await _taxRateRepository.GetByIdAsync(id);
                if (taxRate != null)
                {
                    _taxRateRepository.Delete(taxRate);
                    await _taxRateRepository.SaveAsync();
                }
            }
        }

        public async Task<decimal> CalculateVatAsync(decimal amount, Guid taxRateId)
        {
            var taxRate = await _taxRateRepository.GetByIdAsync(taxRateId);
            if (taxRate == null || taxRate.Type != TaxType.VAT)
            {
                return 0;
            }

            return Math.Round(amount * (taxRate.Rate / 100), 4);
        }

        public async Task<decimal> CalculateWithholdingTaxAsync(decimal amount, Guid taxRateId)
        {
            var taxRate = await _taxRateRepository.GetByIdAsync(taxRateId);
            if (taxRate == null || taxRate.Type != TaxType.Withholding)
            {
                return 0;
            }

            return Math.Round(amount * (taxRate.Rate / 100), 4);
        }

        public async Task<VatReport> GenerateVatReportAsync(DateTime fromDate, DateTime toDate)
        {
            // Get all approved journal entries in the date range
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.Status == JournalEntryStatus.Approved && 
                      je.Date >= fromDate && 
                      je.Date <= toDate);

            // Get all journal entry lines with VAT tax rates
            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();
            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(
                jel => journalEntryIds.Contains(jel.JournalEntryId) && 
                       jel.TaxRateId.HasValue,
                jel => jel.TaxRate,
                jel => jel.JournalEntry,
                jel => jel.Account);

            // Filter for VAT tax rates
            var vatLines = journalEntryLines.Where(jel => jel.TaxRate?.Type == TaxType.VAT).ToList();

            // Group by tax rate
            var vatReportItems = vatLines
                .GroupBy(jel => jel.TaxRate)
                .Select(g => new VatReportItem
                {
                    TaxRateId = g.Key.Id,
                    TaxRateCode = g.Key.Code,
                    TaxRateName = g.Key.NameEn,
                    TaxRatePercentage = g.Key.Rate,
                    TaxableAmount = g.Sum(jel => jel.DebitAmount > 0 ? jel.DebitAmount : jel.CreditAmount),
                    TaxAmount = g.Sum(jel => jel.TaxAmount ?? 0)
                })
                .ToList();

            return new VatReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                GeneratedAt = DateTime.UtcNow,
                Items = vatReportItems,
                TotalTaxableAmount = vatReportItems.Sum(item => item.TaxableAmount),
                TotalTaxAmount = vatReportItems.Sum(item => item.TaxAmount)
            };
        }

        public async Task<WithholdingTaxReport> GenerateWithholdingTaxReportAsync(DateTime fromDate, DateTime toDate)
        {
            // Get all approved journal entries in the date range
            var journalEntries = await _journalEntryRepository.FindAllAsync(
                je => je.Status == JournalEntryStatus.Approved && 
                      je.Date >= fromDate && 
                      je.Date <= toDate);

            // Get all journal entry lines with withholding tax rates
            var journalEntryIds = journalEntries.Select(je => je.Id).ToList();
            var journalEntryLines = await _journalEntryLineRepository.FindAllAsync(
                jel => journalEntryIds.Contains(jel.JournalEntryId) && 
                       jel.TaxRateId.HasValue,
                jel => jel.TaxRate,
                jel => jel.JournalEntry,
                jel => jel.Account);

            // Filter for withholding tax rates
            var withholdingLines = journalEntryLines.Where(jel => jel.TaxRate?.Type == TaxType.Withholding).ToList();

            // Group by tax rate
            var withholdingReportItems = withholdingLines
                .GroupBy(jel => jel.TaxRate)
                .Select(g => new WithholdingTaxReportItem
                {
                    TaxRateId = g.Key.Id,
                    TaxRateCode = g.Key.Code,
                    TaxRateName = g.Key.NameEn,
                    TaxRatePercentage = g.Key.Rate,
                    TaxableAmount = g.Sum(jel => jel.DebitAmount > 0 ? jel.DebitAmount : jel.CreditAmount),
                    TaxAmount = g.Sum(jel => jel.TaxAmount ?? 0)
                })
                .ToList();

            return new WithholdingTaxReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                GeneratedAt = DateTime.UtcNow,
                Items = withholdingReportItems,
                TotalTaxableAmount = withholdingReportItems.Sum(item => item.TaxableAmount),
                TotalTaxAmount = withholdingReportItems.Sum(item => item.TaxAmount)
            };
        }

        public async Task<byte[]> ExportVatReportToXmlAsync(VatReport report)
        {
            var serializer = new XmlSerializer(typeof(VatReport));
            using var memoryStream = new MemoryStream();
            serializer.Serialize(memoryStream, report);
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportVatReportToCsvAsync(VatReport report)
        {
            var csv = new StringBuilder();
            
            // Add header
            csv.AppendLine("Tax Rate Code,Tax Rate Name,Tax Rate Percentage,Taxable Amount,Tax Amount");
            
            // Add items
            foreach (var item in report.Items)
            {
                csv.AppendLine($"{item.TaxRateCode},{item.TaxRateName},{item.TaxRatePercentage},{item.TaxableAmount},{item.TaxAmount}");
            }
            
            // Add totals
            csv.AppendLine($"Total,,{report.TotalTaxableAmount},{report.TotalTaxAmount}");
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }

    public class VatReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<VatReportItem> Items { get; set; } = new List<VatReportItem>();
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
    }

    public class VatReportItem
    {
        public Guid TaxRateId { get; set; }
        public string TaxRateCode { get; set; }
        public string TaxRateName { get; set; }
        public decimal TaxRatePercentage { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
    }

    public class WithholdingTaxReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<WithholdingTaxReportItem> Items { get; set; } = new List<WithholdingTaxReportItem>();
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
    }

    public class WithholdingTaxReportItem
    {
        public Guid TaxRateId { get; set; }
        public string TaxRateCode { get; set; }
        public string TaxRateName { get; set; }
        public decimal TaxRatePercentage { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
